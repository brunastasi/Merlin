using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Moana.Configurations;
using Moana.Models.ApiData;

namespace Moana.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> GetTradingAdviceAsync(string marketData)
        {
            var requestData = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "Tu es un assistant expert en trading crypto. Donne des recommandations de points d'entrée et de sortie pour le marché suivant." },
                    new { role = "user", content = marketData }
                },
                max_completion_tokens = 10
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completionsT", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                return "Erreur lors de l'appel à OpenAI.";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var reply = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return reply;
        }

        public async Task<string> AnalyzeMarketDataAsync(string marketDataJson)
        {
            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a trading assistant. Analyze the following market data and provide a trading strategy (BUY/SELL/HOLD), Stop Loss, and Take Profit recommendations." },
                    new { role = "user", content = marketDataJson }
                }
            };

            try
            {
                // Envoi de la requête à l'API OpenAI
                var response = await _httpClient.PostAsJsonAsync($"https://api.openai.com/v1/chat/completions", requestBody);

                // Vérifie si le statut HTTP est correct
                response.EnsureSuccessStatusCode();

                // Lecture et parsing de la réponse en tant qu'OpenAIResponse
                var responseData = await response.Content.ReadFromJsonAsync<OpenAIResponse>();

                // Vérification si `Choices` existe et contient des données
                if (responseData?.Choices != null && responseData.Choices.Any())
                {
                    return responseData.Choices.First().Message.Content;
                }
                else
                {
                    return "La réponse de l'API OpenAI ne contient pas de choix valides.";
                }
            }
            catch (Exception ex)
            {
                // Gestion des erreurs (par exemple, des exceptions HTTP ou JSON)
                return $"Erreur lors de l'analyse des données de marché : {ex.Message}";
            }
        }
    }
}
