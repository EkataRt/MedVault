using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MedVaultAPI.Services
{
    public class AIQueryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        private static readonly string[] AllowedSpecialties =
        {
            "General Physician", "Neurologist", "Dermatologist", "Cardiologist",
            "Orthopedics", "Dentist", "Gastroenterologist", "Ophthalmologist",
            "Psychiatrist", "Pediatrician", "Gynecology", "Pulmonologist"
        };

        // Default match score used when the AI call fails or a specialty is
        // missing from the model's response. Kept low-ish so a failed AI call
        // doesn't accidentally dominate the weighted score.
        private const double DefaultSpecialtyMatchScore = 40.0;

        public AIQueryService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var apiKey = configuration["GoogleAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Google AI API key not configured.");
            }
            _apiKey = apiKey;
        }

        public async Task<string> GetIntentAsync(string userMessage)
        {
            Console.WriteLine("I got it:" + userMessage);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = @"You are an intent classifier for a Personal Health Assistant. 
                        Your ONLY task is to output exactly ONE of these words based on the user's message:

                        upload_report     → user wants to upload or add a health report or document
                        view_reports      → user wants to see, show, find or retrieve their reports
                        set_reminder      → user wants to set a reminder, alarm or notification for medicine or health task
                        set_appointment   → user wants to schedule, book or set an appointment or hospital visit
                        none              → anything else

                        Rules:
                        - Output ONLY the intent word. No punctuation, no explanation, no labels.
                        - If unsure, output: none" } }
                },
                contents = new[] {
                    new { parts = new[] { new { text = userMessage } } }
                },
                generationConfig = new
                {
                    temperature = 0.0
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_url}?key={_apiKey}", content);

            if (!response.IsSuccessStatusCode)
            {
                return "none";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("I got the intent:" + jsonResponse);

            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    string result = text?.Trim().ToLower() ?? "";

                    if (result.Contains("upload_report")) return "upload_report";
                    if (result.Contains("view_reports")) return "view_reports";
                    if (result.Contains("set_reminder")) return "set_reminder";
                    if (result.Contains("set_appointment")) return "set_appointment";
                }
            }
            catch
            {
                // fall through to default below
            }

            return "none";
        }

        public async Task<string> ClassifySpecialtyAsync(string healthProblem)
        {
            if (string.IsNullOrWhiteSpace(healthProblem))
            {
                return "General Physician";
            }

            var specialtyList = string.Join(", ", AllowedSpecialties);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = $@"You are a symptom-to-specialty classifier for a healthcare app.
                                Given a brief description of a symptom, respond with ONLY the most
                                appropriate medical specialty from this fixed list: [{specialtyList}].
                                If uncertain, or the symptom seems mild/general, respond ""General Physician"".
                                Respond with only the specialty name, nothing else."
                        }
                    }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = healthProblem } } }
                },
                generationConfig = new
                {
                    temperature = 0.0
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_url}?key={_apiKey}", content);

            if (!response.IsSuccessStatusCode)
            {
                return "General Physician";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString()
                        ?.Trim();

                    var matched = Array.Find(AllowedSpecialties, s =>
                        string.Equals(s, text, StringComparison.OrdinalIgnoreCase));

                    return matched ?? "General Physician";
                }
            }
            catch
            {
                // fall through to safe default
            }

            return "General Physician";
        }

        /// <summary>
        /// Asks Gemini to score how well EVERY allowed specialty matches the given
        /// health problem description, on a 0-100 scale. This backs the
        /// "Specialty Match" (40%) factor of the weighted recommendation score,
        /// so every candidate doctor's specialty can be scored with a single AI
        /// call instead of one call per doctor.
        /// </summary>
        /// <param name="healthProblem">Free-text symptom / health problem from the user.</param>
        /// <returns>
        /// Dictionary keyed by specialty name (matching <see cref="AllowedSpecialties"/>)
        /// with a 0-100 match score as the value. Never null - falls back to a
        /// uniform default score for every specialty if the AI call fails.
        /// </returns>
        public async Task<Dictionary<string, double>> GetSpecialtyMatchScoresAsync(string healthProblem)
        {
            // No health problem supplied -> no preference, treat every specialty equally.
            if (string.IsNullOrWhiteSpace(healthProblem))
            {
                return AllowedSpecialties.ToDictionary(s => s, s => 100.0);
            }

            var specialtyList = string.Join(", ", AllowedSpecialties);

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = $@"You are a symptom-to-specialty relevance scorer for a healthcare app.
                                Given a brief description of a health problem, score how relevant EACH of the
                                following medical specialties is for treating that problem, on a scale of 0 to 100
                                (100 = perfect match, 0 = completely irrelevant): [{specialtyList}].

                                Respond with ONLY a raw JSON object (no markdown, no code fences, no explanation)
                                mapping every specialty name exactly as given to an integer score, e.g.:
                                {{""General Physician"": 40, ""Cardiologist"": 95, ...}}

                                Every specialty in the list MUST appear as a key exactly once."
                        }
                    }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = healthProblem } } }
                },
                generationConfig = new
                {
                    temperature = 0.0,
                    responseMimeType = "application/json"
                }
            };

            var fallback = AllowedSpecialties.ToDictionary(s => s, s => DefaultSpecialtyMatchScore);

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync($"{_url}?key={_apiKey}", content);
            }
            catch
            {
                return fallback;
            }

            if (!response.IsSuccessStatusCode)
            {
                return fallback;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return fallback;
                }

                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return fallback;
                }

                // Gemini occasionally wraps JSON in ```json fences even when asked not to; strip them defensively.
                text = text.Trim().Trim('`');
                if (text.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(4).Trim();
                }

                using var scoresDoc = JsonDocument.Parse(text);
                var result = new Dictionary<string, double>();

                foreach (var specialty in AllowedSpecialties)
                {
                    if (scoresDoc.RootElement.TryGetProperty(specialty, out var scoreEl) &&
                        scoreEl.TryGetDouble(out var score))
                    {
                        result[specialty] = Math.Clamp(score, 0.0, 100.0);
                    }
                    else
                    {
                        result[specialty] = DefaultSpecialtyMatchScore;
                    }
                }

                return result;
            }
            catch
            {
                return fallback;
            }
        }
    }
}