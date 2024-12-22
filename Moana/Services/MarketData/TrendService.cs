using Binance.Net.Enums;
using Moana.Models.MarketData;
using Moana.Services.Utils;

namespace Moana.Services.MarketData
{
    public class TrendService
    {
        private readonly BinanceService _binanceService;

        public TrendService(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        /// <summary>
        /// Récupère les tendances des prix pour une paire donnée.
        /// </summary>
        /// <param name="symbol">Le symbole de la paire (ex: BTCUSDT)</param>
        /// <param name="interval">Intervalle de temps (ex: 1m, 1h)</param>
        /// <returns>Un objet TrendData contenant les indicateurs calculés.</returns>
        public async Task<TrendData> GetTrendDataAsync(string symbol, KlineInterval interval)
        {
            var historicalPrices = await _binanceService.GetHistoricalPricesAsync(symbol, interval);

            var sma = IndicatorCalculations.CalculateSMA(historicalPrices, 14);
            var ema = IndicatorCalculations.CalculateEMA(historicalPrices, 14);
            var support = historicalPrices.Min();
            var resistance = historicalPrices.Max();

            return new TrendData
            {
                HistoricalPrices = historicalPrices,
                SMA = sma,
                EMA = ema,
                SupportLevel = support,
                ResistanceLevel = resistance
            };
        }
    }
}
