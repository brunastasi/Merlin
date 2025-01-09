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
                    new { role = "system", content = "You are a trading assistant specializing in scalping strategies. Analyze the provided market data and generate a scalping strategy strictly in JSON format. The JSON output must follow this structure: { \"Action\": \"BUY/SELL/HOLD\", \"SL\": decimal, \"TP\": decimal, \"Confidence\": \"High/Medium/Low\" }. Do not include any explanatory text or additional comments, only return the JSON object." },
                    new { role = "user", content = marketDataJson }
                },
                max_tokens = 400 // Propriété correcte pour limiter le nombre de tokens dans la réponse
            };

            try
            {
                // Envoi de la requête à l'API OpenAI
                var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

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
            catch (HttpRequestException ex)
            {
                return $"Erreur HTTP lors de l'analyse des données de marché : {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Erreur lors de l'analyse des données de marché : {ex.Message}";
            }
        }
    }
}
