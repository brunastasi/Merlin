using Microsoft.Extensions.Options;
using Moana.Configurations;
using Moana.Models.ApiData;
using Moana.Models.MarketData;
using Moana.Services.Utils;
using System.Text.Json;

namespace Moana.Services
{
    public class AlphaVantageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AlphaVantageService(HttpClient httpClient, IOptions<AlphaVantageOptions> options)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;
        }

        /// <summary>
        /// Récupère les données du PIB réel.
        /// </summary>
        public async Task<List<AlphaVantageEconomicData>> GetRealGDPAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=REAL_GDP&interval=annual&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
                throw new Exception("Impossible de récupérer les données du PIB réel.");

            return response.Data;
        }

        /// <summary>
        /// Récupère les données du rendement des obligations (Treasury Yield).
        /// </summary>
        public async Task<List<AlphaVantageEconomicData>> GetTreasuryYieldAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=TREASURY_YIELD&interval=monthly&maturity=10year&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
                throw new Exception("Impossible de récupérer les données des rendements des obligations.");

            return response.Data;
        }

        /// <summary>
        /// Récupère les données de l'indice des prix à la consommation (CPI).
        /// </summary>
        public async Task<List<AlphaVantageEconomicData>> GetCPIAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=CPI&interval=monthly&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
                throw new Exception("Impossible de récupérer les données de l'indice des prix à la consommation.");

            return response.Data;
        }

        public async Task<List<AlphaVantageEconomicData>> GetUnemploymentRateAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=UNEMPLOYMENT&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
                throw new Exception("Impossible de récupérer les données du taux de chômage.");

            return response.Data;
        }

        public async Task<List<AlphaVantageEconomicData>> GetInflationAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=INFLATION&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
                throw new Exception("Impossible de récupérer les données de l'inflation.");

            return response.Data;
        }

    }
}
