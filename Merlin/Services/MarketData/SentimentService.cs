using Merlin.Models.MarketData;

namespace Merlin.Services.MarketData
{
    public class SentimentService
    {
        private readonly AlternativeAPIService _alternativeAPIService;
        private readonly BinanceService _binanceService;

        public SentimentService(AlternativeAPIService alternativeAPIService, BinanceService binanceService)
        {
            _alternativeAPIService = alternativeAPIService;
            _binanceService = binanceService;
        }

        /// <summary>
        /// Récupère les données de sentiment du marché.
        /// </summary>
        /// <returns>Les données de sentiment.</returns>
        public async Task<SentimentData> GetMarketSentimentAsync()
        {
            // 1. Récupération du Fear & Greed Index depuis l'AlternativeAPIService
            var fearGreedResponse = await _alternativeAPIService.GetFearGreedIndexAsync();

            // Vérifie si des données sont disponibles
            if (fearGreedResponse?.Data == null || !fearGreedResponse.Data.Any())
                throw new Exception("Impossible de récupérer le Fear & Greed Index.");

            int fearGreedIndex = fearGreedResponse.Data.First().Value;

            // 2. Déterminer la classification du sentiment
            string sentimentClassification = fearGreedIndex switch
            {
                >= 80 => "Extreme Greed",
                >= 60 => "Greedy",
                >= 50 => "Neutral",
                >= 30 => "Fearful",
                _ => "Extreme Fear"
            };

            // 3. Retourne les données de sentiment
            return new SentimentData
            {
                FearGreedIndex = fearGreedIndex,
                SentimentClassification = sentimentClassification
            };
        }
    }
}
