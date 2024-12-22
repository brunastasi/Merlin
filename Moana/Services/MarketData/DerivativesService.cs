using Binance.Net.Enums;
using Moana.Models.MarketData;

namespace Moana.Services.MarketData
{
    public class DerivativesService
    {
        private readonly BinanceService _binanceService;

        public DerivativesService(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        public async Task<DerivativesData> GetDerivativesDataAsync(string symbol)
        {
            // Récupération des données sur les dérivés depuis Binance
            var openInterest = await _binanceService.GetOpenInterestAsync(symbol);
            var fundingRate = await _binanceService.GetFundingRateAsync(symbol);
            var longShortRatio = await _binanceService.GetLongShortRatioAsync(symbol); // (VERIFIER API FUTURES)
            var futuresVolume = await _binanceService.GetFuturesVolumeAsync(symbol, KlineInterval.FifteenMinutes); // Exemple : 15 minutes


            // Retourner les données consolidées
            return new DerivativesData
            {
                OpenInterest = openInterest,
                FundingRate = fundingRate,
                //LongShortRatio = longShortRatio,
                FuturesVolume = futuresVolume,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
