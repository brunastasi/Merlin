namespace Merlin.Services.Utils
{
    public class IndicatorCalculations
    {
        public static decimal CalculateATR(decimal[] highs, decimal[] lows, decimal[] closes, int period)
        {
            if (highs.Length < period || lows.Length < period || closes.Length < period) return 0;

            var trueRanges = new decimal[highs.Length];
            for (int i = 1; i < highs.Length; i++)
            {
                var tr1 = highs[i] - lows[i];
                var tr2 = Math.Abs(highs[i] - closes[i - 1]);
                var tr3 = Math.Abs(lows[i] - closes[i - 1]);

                trueRanges[i] = Math.Max(tr1, Math.Max(tr2, tr3));
            }

            return CalculateSMA(trueRanges, period);
        }

        /// <summary>
        /// Calcule la moyenne mobile simple (SMA).
        /// </summary>
        /// <param name="prices">Tableau de prix.</param>
        /// <param name="period">Période de calcul.</param>
        /// <returns>La SMA calculée.</returns>
        public static decimal CalculateSMA(decimal[] prices, int period)
        {
            if (prices.Length < period) return 0;
            return prices.TakeLast(period).Average();
        }

        /// <summary>
        /// Calcule la moyenne mobile exponentielle (EMA).
        /// </summary>
        /// <param name="prices">Tableau de prix.</param>
        /// <param name="period">Période de calcul.</param>
        /// <returns>La EMA calculée.</returns>
        public static decimal CalculateEMA(decimal[] prices, int period)
        {
            if (prices.Length < period) return 0;
            decimal multiplier = 2m / (period + 1);
            decimal ema = prices[0]; // Initialisation avec le premier prix

            for (int i = 1; i < prices.Length; i++)
            {
                ema = ((prices[i] - ema) * multiplier) + ema;
            }

            return ema;
        }


    }
}
