using Microsoft.Extensions.Options;
using Moana.Configurations;
using Moana.Models.ApiData;

namespace Moana.Services
{
    public class NewsAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NewsAPIService(HttpClient httpClient, IOptions<NewsAPIOptions> options)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;
        }

        public async Task<NewsApiResponse> GetNewsAsync(string query)
        {
            // Construire l'URL de manière dynamique
            string url = $"https://newsapi.org/v2/everything?q={query}&apiKey={_apiKey}";

            // Envoyer la requête et gérer les erreurs
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur API : {response.StatusCode} - {errorDetails}");
            }

            // Désérialiser la réponse
            return await response.Content.ReadFromJsonAsync<NewsApiResponse>();
        }
    }
}
