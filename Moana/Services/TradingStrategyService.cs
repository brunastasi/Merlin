using Moana.Models;
using Moana.Models.MarketData;
using Moana.Services.MarketData;
using System.Text.Json;

namespace Moana.Services
{
    public class TradingStrategyService
    {
        private readonly OpenAIService _openAIService;
        private readonly DataAggregatorService _dataAggregatorService;
        private readonly ILogger<TradingStrategyService> _logger;

        public TradingStrategyService(OpenAIService openAIService, DataAggregatorService dataAggregatorService, ILogger<TradingStrategyService> logger)
        {
            _openAIService = openAIService;
            _dataAggregatorService = dataAggregatorService;
            _logger = logger;
        }

        public async Task<string> AnalyzeAndExecuteStrategyAsync(string symbol, List<(string Asset, string Type)> assets, UserPreferences userPreferences, bool useAIAnalysis = false)
        {
            // Vérification des entrées
            if (userPreferences == null)
                throw new ArgumentNullException(nameof(userPreferences), "Les préférences utilisateur ne peuvent pas être nulles.");

            // Agréger les données du marché
            var marketData = await _dataAggregatorService.AggregateMarketDataAsync(symbol, assets);

            // Convertir en JSON
            var marketDataJson = _dataAggregatorService.ConvertMarketDataToJson(marketData);

            if (useAIAnalysis)
            {
                // Envoyer à OpenAI pour analyse
                var aiResponse = await _openAIService.AnalyzeMarketDataAsync(marketDataJson);
                return aiResponse; // Retourne la stratégie suggérée par OpenAI
            }
            else
            {
                // Implémentation d'une stratégie locale
                var localStrategy = ExecuteLocalStrategy(marketData, userPreferences);

                // Formatage de la réponse locale
                var response = new
                {
                    Symbol = symbol,
                    Action = localStrategy.Action,
                    StopLoss = localStrategy.StopLoss,
                    TakeProfit = localStrategy.TakeProfit,
                    Confidence = localStrategy.Confidence
                };

                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            }
        }


        /// <summary>
        /// Exécute une stratégie de trading basée sur les données agrégées.
        /// </summary>
        /// <param name="aggregatedData">Les données agrégées pour un symbole de marché donné.</param>
        /// <returns>Une décision de trading incluant l'action, le stop-loss et le take-profit.</returns>
        /// <summary>
        /// Exécute une stratégie de trading locale en analysant les données de marché agrégées et les préférences utilisateur.
        /// Basée sur une combinaison d'indicateurs techniques, de sentiment, de dérivés, de liquidité, et d'événements économiques.
        /// </summary>
        /// <param name="aggregatedData">Les données de marché agrégées.</param>
        /// <param name="userPreferences">Les préférences de l'utilisateur, incluant le budget et la gestion des risques.</param>
        /// <returns>Une décision de trading avec action (BUY/SELL/HOLD), stop-loss, take-profit, et niveau de confiance.</returns>
        public TradingDecision ExecuteLocalStrategy(AggregatedMarketData aggregatedData, UserPreferences userPreferences)
        {
            try
            {
                // **Vérifications des entrées**
                if (aggregatedData == null)
                    throw new ArgumentNullException(nameof(aggregatedData), "Les données agrégées ne peuvent pas être nulles.");
                if (userPreferences == null)
                    throw new ArgumentNullException(nameof(userPreferences), "Les préférences utilisateur ne peuvent pas être nulles.");

                _logger.LogInformation("Exécution de la stratégie pour le symbole {Symbol}.", aggregatedData.Symbol);

                // **1. Analyse des indicateurs techniques**
                // RSI : Ajustement dynamique des seuils selon la volatilité
                var volatility = CalculateVolatility(aggregatedData.TrendData.HistoricalPrices);
                var (overboughtThreshold, oversoldThreshold) = AdjustRSIThresholds(volatility);

                var rsi = aggregatedData.IndicatorData.RSI;
                var isOverbought = rsi > overboughtThreshold;
                var isOversold = rsi < oversoldThreshold;

                // MACD : Ajustement dynamique du seuil
                var macdThreshold = AdjustMACDThreshold(volatility);
                var macdSignal = aggregatedData.IndicatorData.MACD.Signal;
                var macdBullish = macdSignal > macdThreshold;
                var macdBearish = macdSignal < -macdThreshold;

                // Bollinger Bands : Analyse des limites
                var bollingerBands = aggregatedData.IndicatorData.BollingerBands;

                // **2. Analyse de la tendance**
                var currentPrice = aggregatedData.TrendData.HistoricalPrices.Last();
                var (supportLevel, resistanceLevel) = CalculateSupportResistance(aggregatedData.TrendData.HistoricalPrices);

                var isNearSupport = Math.Abs(currentPrice - supportLevel) / currentPrice < 0.02m; // À 2% du support
                var isNearResistance = Math.Abs(currentPrice - resistanceLevel) / currentPrice < 0.02m; // À 2% de la résistance

                var trendDirection = DetermineTrend(aggregatedData.TrendData);

                // **3. Analyse du sentiment**
                var sentiment = aggregatedData.SentimentData.FearGreedIndex;

                // **4. Analyse des dérivés**
                var longShortRatioHigh = aggregatedData.DerivativesData.LongShortRatio > 2;
                var longShortRatioLow = aggregatedData.DerivativesData.LongShortRatio < 0.5m;

                // **5. Analyse fondamentale**
                var highImpactEvents = aggregatedData.EconomicEvents.Count(e => e.ImpactLevel == "High");

                // **6. Analyse de la liquidité**
                var liquidityScore = aggregatedData.LiquidityData.LiquidityScore;

                // **7. Analyse des corrélations**
                var correlatedAssets = aggregatedData.Correlations
                    .Where(c => Math.Abs(c.CorrelationCoefficient) > 0.8m)
                    .ToList();

                // **8. Application des préférences utilisateur**
                var userStopLossPercentage = userPreferences.RiskManagement.StopLoss;
                var userTakeProfitPercentage = userPreferences.RiskManagement.TakeProfit;
                var budget = userPreferences.Budget;

                var calculatedStopLoss = budget * (userStopLossPercentage / 100);
                var calculatedTakeProfit = budget * (userTakeProfitPercentage / 100);

                var stopLoss = Math.Max(calculatedStopLoss, liquidityScore * -0.01m);
                var takeProfit = Math.Min(calculatedTakeProfit, liquidityScore * 0.02m);

                // **9. Prise de décision basée sur des règles pondérées**
                if (isOversold && macdBullish && trendDirection == "Uptrend" && isNearSupport)
                {
                    _logger.LogInformation("Signal BUY détecté pour {Symbol}.", aggregatedData.Symbol);
                    return new TradingDecision
                    {
                        Action = "BUY",
                        StopLoss = stopLoss,
                        TakeProfit = takeProfit,
                        Confidence = "High"
                    };
                }

                if (isOverbought && macdBearish && trendDirection == "Downtrend" && isNearResistance)
                {
                    _logger.LogInformation("Signal SELL détecté pour {Symbol}.", aggregatedData.Symbol);
                    return new TradingDecision
                    {
                        Action = "SELL",
                        StopLoss = stopLoss,
                        TakeProfit = takeProfit,
                        Confidence = "High"
                    };
                }

                _logger.LogInformation("Aucune action significative détectée pour {Symbol}. Recommandation : HOLD.", aggregatedData.Symbol);
                return new TradingDecision
                {
                    Action = "HOLD",
                    StopLoss = 0,
                    TakeProfit = 0,
                    Confidence = "Medium"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution de la stratégie pour {Symbol}.", aggregatedData.Symbol);
                throw;
            }
        }

        public string DetermineTrend(TrendData trendData)
        {
            if (trendData == null)
                throw new ArgumentNullException(nameof(trendData));

            var currentPrice = trendData.HistoricalPrices.LastOrDefault(); // Dernier prix disponible

            if (currentPrice > trendData.EMA)
                return "Uptrend";
            if (currentPrice < trendData.EMA)
                return "Downtrend";
            return "Neutral";
        }

        private (decimal supportLevel, decimal resistanceLevel) CalculateSupportResistance(decimal[] historicalPrices)
        {
            if (historicalPrices == null || historicalPrices.Length < 2)
                throw new ArgumentException("Les prix historiques doivent contenir au moins deux valeurs.");

            var supportLevel = historicalPrices.Min();
            var resistanceLevel = historicalPrices.Max();

            return (supportLevel, resistanceLevel);
        }

        private decimal CalculateVolatility(decimal[] historicalPrices)
        {
            if (historicalPrices == null || historicalPrices.Length < 2)
                throw new ArgumentException("Les prix historiques doivent contenir au moins deux valeurs.");

            var logReturns = historicalPrices
                .Skip(1)
                .Zip(historicalPrices, (current, previous) => (decimal)Math.Log((double)(current / previous)))
                .ToArray();

            var averageReturn = logReturns.Average();
            var squaredDeviations = logReturns.Select(r => (r - averageReturn) * (r - averageReturn));
            var variance = squaredDeviations.Average();

            return (decimal)Math.Sqrt((double)variance);
        }

        private (decimal overboughtThreshold, decimal oversoldThreshold) AdjustRSIThresholds(decimal volatility)
        {
            // Exemple : si la volatilité est élevée, on élargit les seuils
            var adjustment = Math.Min(volatility * 10, 5); // Limite maximale d'ajustement
            var overboughtThreshold = 70 + adjustment;
            var oversoldThreshold = 30 - adjustment;

            return (overboughtThreshold, oversoldThreshold);
        }

        private decimal AdjustMACDThreshold(decimal volatility)
        {
            // Exemple : on ajuste le seuil MACD en fonction de la volatilité
            return Math.Min(volatility * 0.1m, 0.5m); // Ajustement limité à 0.5
        }


    }
}
