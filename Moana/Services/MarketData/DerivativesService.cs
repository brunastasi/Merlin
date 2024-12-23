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

            // Récupérer les données de Long/Short Ratio
            var longShortData = await _binanceService.GetLongShortRatioAsync(symbol);
            var latestLongShort = longShortData.First();

            // Récupération du volume des contrats à terme
            var futuresVolume = await _binanceService.GetFuturesVolumeAsync(symbol, KlineInterval.FifteenMinutes);

            // Calcul du ratio long/short et des volumes longs/courts
            decimal longPositions = latestLongShort.LongAccount;
            decimal shortPositions = latestLongShort.ShortAccount;
            decimal longShortRatio = shortPositions > 0 ? Math.Round(longPositions / shortPositions, 2) : 0;

            // Retourner les données consolidées
            return new DerivativesData
            {
                OpenInterest = openInterest,
                FundingRate = fundingRate,
                LongShortRatio = longShortRatio,
                LongPositions = Math.Round(longPositions, 2),
                ShortPositions = Math.Round(shortPositions, 2),
                FuturesVolume = Math.Round(futuresVolume, 2),
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
