using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MedVaultAPI.Services
{
    public class RxNormService
    {
        private readonly HttpClient _httpClient;

        public RxNormService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://rxnav.nlm.nih.gov/REST/");
        }

        public async Task<List<string>> SearchDrugsAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<JsonObject>($"approximateTerm.json?term={Uri.EscapeDataString(query)}");

                var concepts = response?["approximateGroup"]?["candidate"] as JsonArray;
                if (concepts == null) return new List<string>();

                return concepts
                    .Select(c => c?["name"]?.ToString())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .Take(10)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}