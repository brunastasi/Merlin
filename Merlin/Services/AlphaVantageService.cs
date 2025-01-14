using Microsoft.Extensions.Options;
using Merlin.Configurations;
using Merlin.Models.ApiData;
using Merlin.Models.MarketData;
using Merlin.Services.Utils;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Merlin.Services
{
    public class AlphaVantageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly LoggerService _logger;

        public AlphaVantageService(HttpClient httpClient, IOptions<AlphaVantageOptions> options, LoggerService logger)
        {
            _httpClient = httpClient;
            _apiKey = options.Value.ApiKey;
            _logger = logger;
        }

        /// <summary>
        /// Récupère les données du PIB réel.
        /// </summary>
        public async Task<List<AlphaVantageEconomicData>> GetRealGDPAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=REAL_GDP&interval=annual&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
            {
                _logger.LogError("AlphaVantage : Impossible de récupérer les données du PIB réel.", "ERROR");
                throw new Exception("Impossible de récupérer les données du PIB réel.");
            }

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
            {
                _logger.LogError("AlphaVantage : Impossible de récupérer les données des rendements des obligations.", "ERROR");
                throw new Exception("Impossible de récupérer les données des rendements des obligations.");
            }

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
            {
                _logger.LogError("AlphaVantage : Impossible de récupérer les données de l'indice des prix à la consommation.", "ERROR");
                throw new Exception("Impossible de récupérer les données de l'indice des prix à la consommation.");
            }

            return response.Data;
        }

        public async Task<List<AlphaVantageEconomicData>> GetUnemploymentRateAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=UNEMPLOYMENT&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
            {
                _logger.LogError("AlphaVantage : Impossible de récupérer les données du taux de chômage.", "ERROR");
                throw new Exception("Impossible de récupérer les données du taux de chômage.");
            }

            return response.Data;
        }

        public async Task<List<AlphaVantageEconomicData>> GetInflationAsync()
        {
            string url = $"https://www.alphavantage.co/query?function=INFLATION&apikey={_apiKey}";

            var response = await _httpClient.GetFromJsonAsync<AlphaVantageApiResponse>(url);

            if (response?.Data == null || !response.Data.Any())
            {
                _logger.LogError("AlphaVantage : Impossible de récupérer les données de l'inflation.", "ERROR");
                throw new Exception("Impossible de récupérer les données de l'inflation.");
            }

            return response.Data;
        }

        public async Task<List<decimal>> GetHistoricalPricesAsync(string symbol, string type = "crypto", string interval = "daily")
        {
            string url = type.ToLower() switch
            {
                "crypto" => $"https://www.alphavantage.co/query?function=DIGITAL_CURRENCY_DAILY&symbol={symbol}&market=USD&apikey={_apiKey}",
                "stock" => $"https://www.alphavantage.co/query?function=TIME_SERIES_{interval.ToUpper()}&symbol={symbol}&apikey={_apiKey}",
                _ => throw new ArgumentException("Type d'actif invalide. Utilisez 'crypto' ou 'stock'.")
            };

            var response = await _httpClient.GetStringAsync(url);

            var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(response);

            if (jsonResponse.ContainsKey("Note") || jsonResponse.ContainsKey("Error Message"))
            {
                string errorMessage = jsonResponse.ContainsKey("Note") ? jsonResponse["Note"].ToString() : jsonResponse["Error Message"].ToString();

                _logger.LogError($"AlphaVantage : Erreur de l'API Alpha Vantage : {errorMessage}", "ERROR");
                throw new Exception($"Erreur de l'API Alpha Vantage : {errorMessage}");
            }

            var timeSeriesKey = type.ToLower() == "crypto" ? "Time Series (Digital Currency Daily)" : $"Time Series ({interval})";
            if (!jsonResponse.ContainsKey(timeSeriesKey))
            {
                _logger.LogError("AlphaVantage : Les données historiques ne sont pas disponibles.", "ERROR");
                throw new Exception("Les données historiques ne sont pas disponibles.");
            }

            var timeSeries = jsonResponse[timeSeriesKey] as JsonElement?;
            if (timeSeries == null)
            {
                _logger.LogError("AlphaVantage : Les données historiques ne sont pas disponibles.", "ERROR");
                throw new Exception("Les données historiques ne sont pas disponibles.");
            }

            var prices = new List<decimal>();
            foreach (var element in timeSeries.Value.EnumerateObject())
            {
                if (element.Value.TryGetProperty("4. close", out var closePrice) || element.Value.TryGetProperty("4a. close (USD)", out closePrice))
                {
                    if (decimal.TryParse(closePrice.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var closePriceDecimal))
                    {
                        prices.Add(closePriceDecimal);
                    }
                    else
                    {
                        _logger.LogWarning($"AlphaVantage : Impossible de convertir {closePrice.GetString()} en decimal pour la date {element.Name}.", "ERROR");
                        Console.WriteLine($"Impossible de convertir {closePrice.GetString()} en decimal pour la date {element.Name}.");
                    }
                }
            }

            return prices;
        }

    }
}
