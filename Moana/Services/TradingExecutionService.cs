using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Moana.Models;

namespace Moana.Services
{
    public class TradingExecutionService
    {
        private readonly BinanceService _binanceService;
        private readonly LoggerService _logger;

        public TradingExecutionService(BinanceService binanceService, LoggerService logger)
        {
            _binanceService = binanceService;
            _logger = logger;
        }

        public async Task ExecuteTradingDecisionAsync(TradingDecision decision, string symbol, decimal budget, int leverageValue)
        {
            try
            {
                _logger.LogInformation($"Exécution de la décision de trading : {decision.Action} pour {symbol}", "APPLICATION");
                _logger.LogInformation($"Exécution de la décision de trading : {decision.Action} pour {symbol}", "TRADING");

                switch (decision.Action.ToUpper())
                {
                    case "BUY":
                        await PlaceOrderWithStopLossAndTakeProfit(symbol, budget, decision.SL, decision.TP, OrderSide.Buy, leverageValue);
                        break;

                    case "SELL":
                        await PlaceOrderWithStopLossAndTakeProfit(symbol, budget, decision.SL, decision.TP, OrderSide.Sell, leverageValue);
                        break;

                    case "HOLD":
                        _logger.LogInformation($"Aucune action à prendre pour {symbol}. Décision HOLD.", "TRADING");
                        break;

                    default:
                        _logger.LogWarning($"Action inconnue : {decision.Action}", "TRADING");
                        break;
                }

                _logger.LogInformation("Exécution de la décision de trading terminée.", "APPLICATION");
                _logger.LogInformation("Exécution de la décision de trading terminée.", "TRADING");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de l'exécution de la décision pour {symbol}.", "TRADING");
                throw;
            }
        }

        private async Task PlaceOrderWithStopLossAndTakeProfit(
            string symbol,
            decimal budget,
            decimal? initialStopLoss,
            decimal? initialTakeProfit,
            OrderSide side,
            int leverageValue)
        {
            try
            {
                // Configurez le levier à 1x
                _logger.LogInformation($"Configuration de l'effet de levier x{leverageValue}", "TRADING");
                await _binanceService.SetLeverageAsync(symbol, leverageValue);

                // Configurez le mode de marge (par défaut, isolé)
                //await _binanceService.SetMarginModeAsync(symbol, FuturesMarginType.Isolated);

                decimal currentPrice = await GetCurrentMarketPriceAsync(symbol);
                _logger.LogInformation($"Récupération du prix actuel du {symbol} : {currentPrice} $", "TRADING");

                // Obtenir le TickSize et la StepSize pour ajuster les prix et quantités
                decimal tickSize = await _binanceService.GetTickSizeAsync(symbol);
                int quantityPrecision = await _binanceService.GetAssetPrecisionAsync(symbol);

                // Calculer la quantité
                decimal quantity = budget / currentPrice;
                quantity = AdjustToPrecision(quantity, quantityPrecision);

                if (quantity <= 0)
                {
                    throw new Exception($"Quantité trop petite après ajustement : {quantity}");
                }

                // Ajuster les prix initialement
                decimal slPercentage = initialStopLoss.HasValue ? Math.Abs((currentPrice - initialStopLoss.Value) / currentPrice) : 0.05m; // Par défaut 5%
                decimal tpPercentage = initialTakeProfit.HasValue ? Math.Abs((initialTakeProfit.Value - currentPrice) / currentPrice) : 0.20m; // Par défaut 20%


                decimal? stopLoss = initialStopLoss.HasValue ? AdjustPriceToTickSize(initialStopLoss.Value, tickSize) : null;
                decimal? takeProfit = initialTakeProfit.HasValue ? AdjustPriceToTickSize(initialTakeProfit.Value, tickSize) : null;

                // Validation initiale et ajustement
                ValidateAndAdjustOrderLevels(ref stopLoss, ref takeProfit, currentPrice, slPercentage, tpPercentage, tickSize, side);


                // Vérifier si l'ordre respecte le montant minimal requis
                decimal minNotional = await _binanceService.GetMinNotionalAsync(symbol);
                if (quantity * currentPrice < minNotional)
                {
                    _logger.LogError($"La valeur totale de l'ordre ({quantity * currentPrice}) est inférieure au montant minimal requis ({minNotional}) pour {symbol}.", "ERROR");
                    throw new Exception($"La valeur totale de l'ordre ({quantity * currentPrice}) est inférieure au montant minimal requis ({minNotional}) pour {symbol}.");
                }

                // Vérification si le prix actuel change et ajustement dynamique des ordres
                decimal updatedPrice = await GetCurrentMarketPriceAsync(symbol);
                if (Math.Abs(updatedPrice - currentPrice) > tickSize * 5)
                {
                    _logger.LogWarning($"Prix du marché modifié : ANCIEN = {currentPrice}, NOUVEAU = {updatedPrice}", "TRADING");

                    currentPrice = updatedPrice;

                    // Validation après ajustement basé sur le pourcentage initial
                    ValidateAndAdjustOrderLevels(ref stopLoss, ref takeProfit, currentPrice, slPercentage, tpPercentage, tickSize, side);
                }

                // Placer l'ordre principal (Market Order)
                var mainOrder = await _binanceService.PlaceAdvancedOrderAsync(
                    symbol: symbol,
                    side: side,
                    type: FuturesOrderType.Market,
                    quantity: quantity
                );
                _logger.LogInformation($"Ordre principal {side} placé avec succès : {mainOrder.Id}", "TRADING");

                // Placer Stop-Loss
                BinanceFuturesOrder stopOrder = null;
                if (stopLoss.HasValue)
                {
                    stopOrder = await _binanceService.PlaceAdvancedOrderAsync(
                        symbol: symbol,
                        side: side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                        type: FuturesOrderType.StopMarket,
                        quantity: quantity, // Utilisation de closePosition pour réduire la position
                        stopPrice: stopLoss
                    );
                    _logger.LogInformation($"Ordre STOPLOSS placé à {stopLoss} - {stopOrder.Id}", "TRADING");
                }

                // Placer Take-Profit
                BinanceFuturesOrder takeProfitOrder = null;
                if (takeProfit.HasValue)
                {
                    // Récupérer les limites de prix
                    var (minPrice, maxPrice) = await _binanceService.GetPriceLimitsAsync(symbol);

                    // Valider le `takeProfit`
                    if (takeProfit < minPrice || takeProfit > maxPrice)
                    {
                        _logger.LogError($"Le TakeProfit ({takeProfit}) est hors des limites autorisées ({minPrice} - {maxPrice}).", "ERROR");
                        throw new Exception($"Le TakeProfit ({takeProfit}) est hors des limites autorisées ({minPrice} - {maxPrice}).");
                    }

                    takeProfitOrder = await _binanceService.PlaceAdvancedOrderAsync(
                        symbol: symbol,
                        side: side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                        type: FuturesOrderType.TakeProfitMarket,
                        quantity: quantity,
                        stopPrice: takeProfit,
                        reduceOnly: false, // Associer au TakeProfit
                        positionSide: PositionSide.Both // Assurez-vous que le PositionSide est cohérent avec votre position
                    );

                    _logger.LogInformation($"Ordre TAKEPROFIT placé à {takeProfit} - {takeProfitOrder.Id}", "TRADING");
                }

                // Surveiller et annuler les ordres opposés une seule fois
                if (stopOrder != null && takeProfitOrder != null)
                {
                    _logger.LogInformation("Vérification et suppression des ordres opposé déjà executé.", "TRADING");
                    await CheckAndCancelOppositeOrderAsync(symbol, stopOrder.Id, takeProfitOrder.Id);
                }

                // Logs finaux
                _logger.LogInformation("--------------------------------------------", "TRADING");
                _logger.LogInformation($"Prix actuel : {currentPrice}", "TRADING");
                _logger.LogInformation($"StopLoss final ajusté : {stopLoss}", "TRADING");
                _logger.LogInformation($"TakeProfit final ajusté : {takeProfit}", "TRADING");
                _logger.LogInformation($"Quantité pour l'ordre Take-Profit : {quantity}", "TRADING");
                _logger.LogInformation("--------------------------------------------", "TRADING");

            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du placement des ordres pour {symbol}.", "ERROR");
                throw;
            }
        }

        private async Task CheckAndCancelOppositeOrderAsync(string symbol, long stopOrderId, long takeProfitOrderId)
        {
            // Récupérer les ordres actifs
            var activeOrders = await _binanceService.GetActiveOrdersAsync(symbol);

            // Vérifier si le Stop-Loss est toujours actif
            var stopOrder = activeOrders.FirstOrDefault(o => o.Id == stopOrderId);

            // Vérifier si le Take-Profit est toujours actif
            var takeProfitOrder = activeOrders.FirstOrDefault(o => o.Id == takeProfitOrderId);

            // Si l'ordre Stop-Loss est exécuté, annuler le Take-Profit
            if (stopOrder == null && takeProfitOrder != null)
            {
                _logger.LogInformation($"Stop-Loss exécuté. Annulation du Take-Profit : {takeProfitOrderId}", "TRADING");
                await _binanceService.CancelOrderAsync(symbol, takeProfitOrderId);
            }

            // Si l'ordre Take-Profit est exécuté, annuler le Stop-Loss
            if (takeProfitOrder == null && stopOrder != null)
            {
                _logger.LogInformation($"Take-Profit exécuté. Annulation du Stop-Loss : {stopOrderId}", "TRADING");
                await _binanceService.CancelOrderAsync(symbol, stopOrderId);
            }

            // Si les deux ordres sont déjà fermés ou exécutés, aucun traitement n'est nécessaire
            if (stopOrder == null && takeProfitOrder == null)
            {
                _logger.LogInformation("Les deux ordres sont déjà exécutés ou annulés.", "TRADING");
            }
        }

        private void ValidateAndAdjustOrderLevels(
            ref decimal? stopLoss,
            ref decimal? takeProfit,
            decimal currentPrice,
            decimal slPercentage,
            decimal tpPercentage,
            decimal tickSize,
            OrderSide side)
        {
            if (side == OrderSide.Buy)
            {
                // Ajustement StopLoss pour un ordre BUY
                if (stopLoss.HasValue)
                {
                    decimal expectedSL = AdjustPriceToTickSize(currentPrice * (1 - slPercentage), tickSize);

                    // Vérifier si le SL actuel respecte déjà les contraintes
                    if (stopLoss < expectedSL && stopLoss >= currentPrice - tickSize * 2)
                    {
                        _logger.LogInformation($"STOPLOSS ({stopLoss}) déjà valide pour un ordre ACHAT.", "TRADING");
                    }
                    else
                    {
                        decimal oldStopLoss = stopLoss.Value;
                        stopLoss = expectedSL < currentPrice - tickSize * 2 ? expectedSL : currentPrice - tickSize * 2;
                        _logger.LogInformation($"STOPLOSS {oldStopLoss} ajusté à {stopLoss} pour un ordre ACHAT.", "TRADING");
                    }
                }

                // Ajustement TakeProfit pour un ordre BUY
                if (takeProfit.HasValue)
                {
                    decimal expectedTP = AdjustPriceToTickSize(currentPrice * (1 + tpPercentage), tickSize);

                    // Vérifier si le TP actuel respecte déjà les contraintes
                    if (takeProfit > expectedTP && takeProfit <= currentPrice + tickSize * 2)
                    {
                        _logger.LogInformation($"TAKEPROFIT ({takeProfit}) déjà valide pour un ordre ACHAT.", "TRADING");
                    }
                    else
                    {
                        decimal oldTakeProfit = takeProfit.Value;
                        takeProfit = expectedTP > currentPrice + tickSize * 2 ? expectedTP : currentPrice + tickSize * 2;
                        _logger.LogInformation($"TAKEPROFIT {oldTakeProfit} ajusté à {takeProfit} pour un ordre ACHAT.", "TRADING");
                    }
                }
            }
            else if (side == OrderSide.Sell)
            {
                // Ajustement StopLoss pour un ordre SELL
                if (stopLoss.HasValue)
                {
                    decimal expectedSL = AdjustPriceToTickSize(currentPrice * (1 + slPercentage), tickSize);

                    // Vérifier si le SL actuel respecte déjà les contraintes
                    if (stopLoss > expectedSL && stopLoss <= currentPrice + tickSize * 2)
                    {
                        _logger.LogInformation($"STOPLOSS ({stopLoss}) déjà valide pour un ordre VENTE.", "TRADING");
                    }
                    else
                    {
                        decimal oldStopLoss = stopLoss.Value;
                        stopLoss = expectedSL > currentPrice + tickSize * 2 ? expectedSL : currentPrice + tickSize * 2;
                        _logger.LogInformation($"STOPLOSS {oldStopLoss} ajusté à {stopLoss} pour un ordre VENTE.", "TRADING");
                    }
                }

                // Ajustement TakeProfit pour un ordre SELL
                if (takeProfit.HasValue)
                {
                    decimal expectedTP = AdjustPriceToTickSize(currentPrice * (1 - tpPercentage), tickSize);

                    // Vérifier si le TP actuel respecte déjà les contraintes
                    if (takeProfit < expectedTP && takeProfit >= currentPrice - tickSize * 2)
                    {
                        _logger.LogInformation($"TAKEPROFIT ({takeProfit}) déjà valide pour un ordre VENTE.", "TRADING");
                    }
                    else
                    {
                        decimal oldTakeProfit = takeProfit.Value;
                        takeProfit = expectedTP < currentPrice - tickSize * 2 ? expectedTP : currentPrice - tickSize * 2;
                        _logger.LogInformation($"TAKEPROFIT {oldTakeProfit} ajusté à {takeProfit} pour un ordre VENTE.", "TRADING");
                    }
                }
            }
        }

        private async Task<decimal> GetCurrentMarketPriceAsync(string symbol)
        {
            var priceResult = await _binanceService.GetPriceAsync(symbol);
            if (!priceResult.Success)
                throw new Exception($"Erreur lors de la récupération du prix pour {symbol} : {priceResult.Error?.Message}");

            return priceResult.Data.Price;
        }

        private decimal AdjustToPrecision(decimal value, int precision)
        {
            decimal step = (decimal)Math.Pow(10, -precision); // Calcul du pas à partir de la précision
            return Math.Floor(value / step) * step; // Réduit à la précision demandée sans tomber à zéro
        }

        private decimal AdjustPriceToTickSize(decimal value, decimal tickSize)
        {
            return Math.Floor(value / tickSize) * tickSize;
        }

        public async Task<decimal> GetCurrentPrice(string symbol)
        {
            // Utilisation de l'API Binance pour récupérer le prix actuel
            var result = await _binanceService.GetPriceAsync(symbol);

            if (!result.Success)
            {
                _logger.LogError($"Erreur lors de la récupération du prix actuel pour {symbol} : {result.Error?.Message}", "ERROR");
                throw new Exception($"Erreur lors de la récupération du prix actuel pour {symbol}.");
            }

            _logger.LogInformation($"Prix actuel récupéré pour {symbol} : {result.Data.Price}", "TRADING");
            return result.Data.Price;
        }

        public async Task<decimal> GetAvailableBalance(string symbol)
        {
            // Récupérer les informations du compte Futures via BinanceService
            var accountInfo = await _binanceService.GetAccountInfoAsync();

            if (accountInfo == null)
            {
                _logger.LogError("Erreur lors de la récupération des informations de compte Futures.", "API");
                throw new Exception("Erreur lors de la récupération des informations de compte Futures.");
            }

            // Extraction de l'actif depuis le symbole
            var asset = symbol.Replace("USDT", ""); // Par exemple : "BTC" pour "BTCUSDT"

            // Rechercher le solde pour l'actif
            var balance = accountInfo.Assets.FirstOrDefault(a => a.Asset == asset);

            if (balance == null)
            {
                _logger.LogWarning($"Aucun solde trouvé pour l'actif {asset}.", "TRADING");
                return 0;
            }

            // Retourner le solde disponible
            _logger.LogInformation($"Solde disponible pour {asset} : {balance.AvailableBalance}", "TRADING");
            return balance.AvailableBalance;
        }
    }

}
