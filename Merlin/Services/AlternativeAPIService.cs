using Merlin.Models.ApiData;
using System.Net.Http.Json;

namespace Merlin.Services
{
    public class AlternativeAPIService
    {
        private readonly HttpClient _httpClient;

        public AlternativeAPIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Récupère le Fear & Greed Index depuis l'API Alternative.me.
        /// </summary>
        /// <returns>Les données du Fear & Greed Index.</returns>
        public async Task<FearGreedApiResponse> GetFearGreedIndexAsync()
        {
            string url = "https://api.alternative.me/fng/?limit=1";

            var response = await _httpClient.GetFromJsonAsync<FearGreedApiResponse>(url);

            if (response == null || response.Data == null)
                throw new Exception("Erreur lors de la récupération du Fear & Greed Index.");

            return response;
        }
    }
}
