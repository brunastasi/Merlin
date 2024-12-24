using Binance.Net.Enums;
using Moana.Models.MarketData;
using Newtonsoft.Json;

namespace Moana.Services.MarketData
{
    public class DataAggregatorService
    {
        private readonly VolumeService _volumeService;
        private readonly TrendService _trendService;
        private readonly IndicatorsService _indicatorsService;
        private readonly SentimentService _sentimentService;
        private readonly DerivativesService _derivativesService;
        private readonly LiquidityService _liquidityService;
        private readonly EconomicEventService _economicEventService;
        private readonly CorrelationService _correlationService;

        public DataAggregatorService(
            VolumeService volumeService,
            TrendService trendService,
            IndicatorsService indicatorsService,
            SentimentService sentimentService,
            DerivativesService derivativesService,
            LiquidityService liquidityService,
            EconomicEventService economicEventService,
            CorrelationService correlationService)
        {
            _volumeService = volumeService;
            _trendService = trendService;
            _indicatorsService = indicatorsService;
            _sentimentService = sentimentService;
            _derivativesService = derivativesService;
            _liquidityService = liquidityService;
            _economicEventService = economicEventService;
            _correlationService = correlationService;
        }

        public async Task<AggregatedMarketData> AggregateMarketDataAsync(string symbol, List<(string Asset, string Type)> assets)
        {
            // Récupérer les données à partir des différents services
            var volumeData = await _volumeService.GetVolumeDataAsync(symbol);
            var trendData = await _trendService.GetTrendDataAsync(symbol, KlineInterval.OneHour);
            var sentimentData = await _sentimentService.GetMarketSentimentAsync();
            var derivativesData = await _derivativesService.GetDerivativesDataAsync(symbol);
            var liquidityData = await _liquidityService.GetLiquidityDataAsync(symbol, 1000);
            var economicEvents = await _economicEventService.GetEconomicEventsAsync();
            var correlations = await _correlationService.GetCorrelationDataAsync(assets);

            // Récupérer les indicateurs techniques
            var indicatorData = await _indicatorsService.GetIndicatorsAsync(symbol, KlineInterval.FifteenMinutes);

            // Optimisations des données
            if (trendData?.HistoricalPrices != null && trendData.HistoricalPrices.Length > 10)
                trendData.HistoricalPrices = trendData.HistoricalPrices.TakeLast(10).ToArray();

            if (indicatorData?.ParabolicSAR != null && indicatorData.ParabolicSAR.Length > 10)
                indicatorData.ParabolicSAR = indicatorData.ParabolicSAR.TakeLast(10).ToArray();

            // Créer et retourner l'objet agrégé
            return new AggregatedMarketData
            {
                Symbol = symbol,
                VolumeData = volumeData,
                TrendData = trendData,
                SentimentData = sentimentData,
                DerivativesData = derivativesData,
                IndicatorData = indicatorData,
                LiquidityData = liquidityData,
                EconomicEvents = economicEvents,
                Correlations = correlations
            };
        }

        public string ConvertMarketDataToJson(AggregatedMarketData aggregatedMarketData)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // Ignore uniquement les propriétés nulles
                    Formatting = Formatting.Indented,
                };

                return JsonConvert.SerializeObject(aggregatedMarketData, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion en JSON : {ex.Message}");
                throw;
            }
        }
    }
}
