using Binance.Net.Enums;
using Moana.Models.MarketData;
using Moana.Services.Utils;

namespace Moana.Services.MarketData
{
    public class IndicatorsService
    {
        private readonly BinanceService _binanceService;


        public IndicatorsService(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        /// <summary>
        /// Récupère les indicateurs techniques pour une paire donnée.
        /// </summary>
        /// <param name="symbol">Le symbole de la paire (ex: BTCUSDT)</param>
        /// <param name="interval">Intervalle de temps sous forme de KlineInterval</param>
        /// <returns>Un objet IndicatorData contenant les indicateurs calculés.</returns>
        public async Task<IndicatorData> GetIndicatorsAsync(string symbol, KlineInterval interval)
        {
            // Récupérer les données historiques depuis Binance
            var historicalPrices = await _binanceService.GetHistoricalPricesAsync(symbol, interval);
            var highs = await _binanceService.GetHighPricesAsync(symbol, interval);
            var lows = await _binanceService.GetLowPricesAsync(symbol, interval);
            var opens = await _binanceService.GetOpenPricesAsync(symbol, interval); // Ajout de l'ouverture
            var closes = await _binanceService.GetClosePricesAsync(symbol, interval);
            var volumes = await _binanceService.GetVolumesAsync(symbol, interval); // Récupération des volumes

            // Calculer les indicateurs
            var rsi = CalculateRSI(historicalPrices, 14);
            var macd = CalculateMACD(historicalPrices, 12, 26, 9);
            var bollingerBands = CalculateBollingerBands(historicalPrices, 20, 2);
            var atr = IndicatorCalculations.CalculateATR(highs, lows, closes, 14); // Période de 14 par défaut

            var ichimoku = CalculateIchimokuCloud(highs, lows);
            var stochastic = CalculateStochasticOscillator(highs, lows, closes, 14);

            var parabolicSAR = CalculateParabolicSAR(highs, lows);
            var vwap = CalculateVWAP(highs, lows, closes, volumes); // Calcul du VWAP
            var adx = CalculateADX(highs, lows, closes, 14);

            var cmf = CalculateCMF(highs, lows, closes, volumes, 14);
            var rvi = CalculateRVI(opens, closes, highs, lows, 14);
            var williamsR = CalculateWilliamsR(highs, lows, closes, 14);
            var adl = CalculateADL(highs, lows, closes, volumes);
            var cmo = CalculateCMO(closes, 14);
            var obv = CalculateOBV(closes, volumes);

            return new IndicatorData
            {
                RSI = rsi,
                MACD = macd,
                BollingerBands = bollingerBands,
                ATR = atr,
                Ichimoku = ichimoku,
                Stochastic = stochastic,
                ParabolicSAR = parabolicSAR,
                VWAP = vwap,
                ADX = adx,
                CMF = cmf,
                RVI = rvi,
                WilliamsR = williamsR,
                ADL = adl,
                CMO = cmo,
                OBV = obv,
            };
        }

        private decimal CalculateRSI(decimal[] prices, int period)
        {
            if (prices.Length < period + 1) return 0;

            decimal gain = 0, loss = 0;

            for (int i = 1; i <= period; i++)
            {
                var change = prices[i] - prices[i - 1];
                if (change > 0) gain += change;
                else loss -= change;
            }

            decimal averageGain = gain / period;
            decimal averageLoss = loss / period;

            if (averageLoss == 0) return 100;

            decimal rs = averageGain / averageLoss;
            return 100 - (100 / (1 + rs));
        }

        private (decimal Line, decimal Signal) CalculateMACD(decimal[] prices, int fastPeriod, int slowPeriod, int signalPeriod)
        {
            if (prices.Length < slowPeriod) return (0, 0);

            var emaFast = IndicatorCalculations.CalculateEMA(prices, fastPeriod);
            var emaSlow = IndicatorCalculations.CalculateEMA(prices, slowPeriod);
            var macdLine = emaFast - emaSlow;

            // Calcul de la ligne Signal (EMA du MACD)
            var macdArray = prices.Select((_, idx) => idx >= slowPeriod
                ? IndicatorCalculations.CalculateEMA(prices.Take(idx + 1).ToArray(), fastPeriod)
                  - IndicatorCalculations.CalculateEMA(prices.Take(idx + 1).ToArray(), slowPeriod)
                : 0).ToArray();

            var signalLine = IndicatorCalculations.CalculateEMA(macdArray, signalPeriod);

            return (macdLine, signalLine);
        }

        private (decimal Upper, decimal Middle, decimal Lower) CalculateBollingerBands(decimal[] prices, int period, decimal deviation)
        {
            if (prices.Length < period) return (0, 0, 0);

            var sma = IndicatorCalculations.CalculateSMA(prices, period);
            var variance = prices.TakeLast(period).Select(p => (p - sma) * (p - sma)).Average();
            var standardDeviation = (decimal)Math.Sqrt((double)variance);

            var upperBand = sma + (deviation * standardDeviation);
            var lowerBand = sma - (deviation * standardDeviation);

            return (upperBand, sma, lowerBand);
        }

        private (decimal TenkanSen, decimal KijunSen, decimal SenkouSpanA, decimal SenkouSpanB) CalculateIchimokuCloud(decimal[] highs, decimal[] lows, int shortPeriod = 9, int mediumPeriod = 26, int longPeriod = 52)
        {
            if (highs.Length < longPeriod || lows.Length < longPeriod) return (0, 0, 0, 0);

            decimal tenkanSen = (highs.TakeLast(shortPeriod).Max() + lows.TakeLast(shortPeriod).Min()) / 2;
            decimal kijunSen = (highs.TakeLast(mediumPeriod).Max() + lows.TakeLast(mediumPeriod).Min()) / 2;
            decimal senkouSpanA = (tenkanSen + kijunSen) / 2;
            decimal senkouSpanB = (highs.TakeLast(longPeriod).Max() + lows.TakeLast(longPeriod).Min()) / 2;

            return (tenkanSen, kijunSen, senkouSpanA, senkouSpanB);
        }

        private (decimal PercentK, decimal PercentD) CalculateStochasticOscillator(decimal[] highs, decimal[] lows, decimal[] closes, int period)
        {
            if (highs.Length < period || lows.Length < period || closes.Length < period)
                return (0, 0);

            var highestHigh = highs.TakeLast(period).Max();
            var lowestLow = lows.TakeLast(period).Min();

            if (highestHigh == lowestLow)
            {
                // Si le plus haut et le plus bas sont identiques, on retourne 0 pour éviter une division par zéro.
                return (0, 0);
            }

            var percentK = ((closes.Last() - lowestLow) / (highestHigh - lowestLow)) * 100;
            var percentD = IndicatorCalculations.CalculateSMA(closes.TakeLast(3).ToArray(), 3);

            return (percentK, percentD);
        }


        private decimal[] CalculateParabolicSAR(decimal[] highs, decimal[] lows, decimal initialAf = 0.02m, decimal maxAf = 0.2m)
        {
            if (highs.Length < 2 || lows.Length < 2) return new decimal[highs.Length];

            decimal af = initialAf;
            decimal extremePoint = highs[0];
            decimal sar = lows[0];
            var sarValues = new decimal[highs.Length];

            for (int i = 1; i < highs.Length; i++)
            {
                sar = sar + af * (extremePoint - sar);
                if (highs[i] > extremePoint)
                {
                    extremePoint = highs[i];
                    af = Math.Min(af + initialAf, maxAf);
                }
                else if (lows[i] < sar)
                {
                    extremePoint = lows[i];
                    af = initialAf;
                    sar = highs[i - 1]; // Reset SAR
                }

                sarValues[i] = sar;
            }

            return sarValues;
        }

        private decimal CalculateVWAP(decimal[] highs, decimal[] lows, decimal[] closes, decimal[] volumes)
        {
            if (highs.Length != volumes.Length || lows.Length != volumes.Length || closes.Length != volumes.Length)
                return 0;

            decimal totalTypicalPriceVolume = 0;
            decimal totalVolume = 0;

            for (int i = 0; i < volumes.Length; i++)
            {
                var typicalPrice = (highs[i] + lows[i] + closes[i]) / 3; // Prix typique
                totalTypicalPriceVolume += typicalPrice * volumes[i];   // Accumuler Volume * Prix Typique
                totalVolume += volumes[i];                              // Accumuler Volume
            }

            return totalVolume == 0 ? 0 : totalTypicalPriceVolume / totalVolume; // VWAP
        }

        private decimal CalculateADX(decimal[] highs, decimal[] lows, decimal[] closes, int period)
        {
            if (highs.Length < period || lows.Length < period || closes.Length < period) return 0;

            var trueRanges = new decimal[highs.Length];
            var plusDM = new decimal[highs.Length];
            var minusDM = new decimal[highs.Length];

            for (int i = 1; i < highs.Length; i++)
            {
                var tr = Math.Max(highs[i] - lows[i], Math.Max(Math.Abs(highs[i] - closes[i - 1]), Math.Abs(lows[i] - closes[i - 1])));
                var pdm = highs[i] > highs[i - 1] ? highs[i] - highs[i - 1] : 0;
                var mdm = lows[i - 1] > lows[i] ? lows[i - 1] - lows[i] : 0;

                trueRanges[i] = tr;
                plusDM[i] = pdm > mdm ? pdm : 0;
                minusDM[i] = mdm > pdm ? mdm : 0;
            }

            decimal smoothedTR = IndicatorCalculations.CalculateSMA(trueRanges, period);
            decimal smoothedPlusDM = IndicatorCalculations.CalculateSMA(plusDM, period);
            decimal smoothedMinusDM = IndicatorCalculations.CalculateSMA(minusDM, period);

            if (smoothedTR == 0) return 0;

            decimal plusDI = (smoothedPlusDM / smoothedTR) * 100;
            decimal minusDI = (smoothedMinusDM / smoothedTR) * 100;

            decimal[] dxArray = new decimal[highs.Length - period];
            for (int i = 0; i < dxArray.Length; i++)
            {
                dxArray[i] = Math.Abs(plusDI - minusDI) / (plusDI + minusDI) * 100;
            }

            // Calcul de l'ADX avec SMA
            return IndicatorCalculations.CalculateSMA(dxArray, period);
        }

        private decimal CalculateCMF(decimal[] highs, decimal[] lows, decimal[] closes, decimal[] volumes, int period)
        {
            if (highs.Length < period || lows.Length < period || closes.Length < period || volumes.Length < period)
                return 0;

            decimal moneyFlowVolume = 0;
            decimal totalVolume = 0;

            for (int i = highs.Length - period; i < highs.Length; i++)
            {
                // Vérification pour éviter la division par zéro
                if (highs[i] == lows[i])
                {
                    continue; // Ignorer ce point, car le dénominateur serait 0
                }

                var moneyFlowMultiplier = ((closes[i] - lows[i]) - (highs[i] - closes[i])) / (highs[i] - lows[i]);
                moneyFlowVolume += moneyFlowMultiplier * volumes[i];
                totalVolume += volumes[i];
            }

            if (totalVolume == 0) return 0;

            return moneyFlowVolume / totalVolume;
        }


        private decimal CalculateRVI(decimal[] opens, decimal[] closes, decimal[] highs, decimal[] lows, int period)
        {
            if (opens.Length < period || closes.Length < period || highs.Length < period || lows.Length < period)
                return 0;

            decimal numerator = 0;
            decimal denominator = 0;

            for (int i = highs.Length - period; i < highs.Length; i++)
            {
                numerator += closes[i] - opens[i];
                denominator += highs[i] - lows[i];
            }

            if (denominator == 0) return 0;

            return numerator / denominator;
        }

        private decimal CalculateWilliamsR(decimal[] highs, decimal[] lows, decimal[] closes, int period)
        {
            // Vérifier que les tableaux ont au moins 'period' éléments
            if (highs.Length < period || lows.Length < period || closes.Length < period)
                return 0;

            // Calculer les valeurs nécessaires
            var highestHigh = highs.TakeLast(period).Max();
            var lowestLow = lows.TakeLast(period).Min();
            var lastClose = closes.Last();

            // Vérifier pour éviter une division par zéro
            if (highestHigh == lowestLow)
            {
                Console.WriteLine("Williams %R: highestHigh et lowestLow sont égaux, division par zéro évitée.");
                return 0; // Retourner une valeur neutre
            }

            // Calculer et retourner Williams %R
            return ((highestHigh - lastClose) / (highestHigh - lowestLow)) * -100;
        }


        private decimal CalculateADL(decimal[] highs, decimal[] lows, decimal[] closes, decimal[] volumes)
        {
            // Vérification des longueurs des tableaux
            if (highs.Length != volumes.Length || lows.Length != volumes.Length || closes.Length != volumes.Length)
            {
                Console.WriteLine("Les tableaux d'entrées pour ADL ont des longueurs différentes.");
                return 0;
            }

            decimal adl = 0;

            for (int i = 0; i < highs.Length; i++)
            {
                var range = highs[i] - lows[i];

                // Vérification pour éviter une division par zéro
                if (range == 0)
                {
                    Console.WriteLine($"Division par zéro évitée à l'index {i} : highs[{i}] = {highs[i]}, lows[{i}] = {lows[i]}");
                    continue; // Passer à l'itération suivante
                }

                var moneyFlowMultiplier = ((closes[i] - lows[i]) - (highs[i] - closes[i])) / range;
                var moneyFlowVolume = moneyFlowMultiplier * volumes[i];
                adl += moneyFlowVolume;
            }

            return adl;
        }


        private decimal CalculateCMO(decimal[] closes, int period)
        {
            if (closes.Length < period + 1)
            {
                Console.WriteLine($"Taille insuffisante pour calculer le CMO. Longueur des fermetures : {closes.Length}, Période : {period}");
                return 0;
            }

            decimal gains = 0, losses = 0;

            for (int i = 1; i <= period; i++)
            {
                var change = closes[i] - closes[i - 1];
                if (change > 0)
                    gains += change;
                else
                    losses -= change;
            }

            if (gains + losses == 0)
            {
                Console.WriteLine("Division par zéro évitée lors du calcul du CMO : (gains + losses) == 0");
                return 0;
            }

            return (gains - losses) / (gains + losses) * 100;
        }


        private decimal CalculateOBV(decimal[] closes, decimal[] volumes)
        {
            if (closes.Length != volumes.Length)
            {
                Console.WriteLine($"Les longueurs des tableaux 'closes' ({closes.Length}) et 'volumes' ({volumes.Length}) ne correspondent pas.");
                return 0;
            }

            decimal obv = 0;

            for (int i = 1; i < closes.Length; i++)
            {
                if (closes[i] > closes[i - 1])
                    obv += Math.Max(volumes[i], 0); // Utilisation de Math.Max pour éviter les volumes négatifs
                else if (closes[i] < closes[i - 1])
                    obv -= Math.Max(volumes[i], 0);
            }

            return obv;
        }

    }
}
