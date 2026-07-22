using System;
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

        public AIQueryService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            var apiKey = "key";
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
    }
}