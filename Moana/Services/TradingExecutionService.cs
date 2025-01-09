using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.SharedApis;
using Moana.Models;
using System.Drawing;

namespace Moana.Services
{
    public class TradingExecutionService
    {
        private readonly BinanceService _binanceService;
        private readonly ILogger<TradingExecutionService> _logger;

        public TradingExecutionService(BinanceService binanceService, ILogger<TradingExecutionService> logger)
        {
            _binanceService = binanceService;
            _logger = logger;
        }

        public async Task ExecuteTradingDecisionAsync(TradingDecision decision, string symbol, decimal budget)
        {
            try
            {
                _logger.LogInformation($"Exécution de la décision : {decision.Action} pour {symbol}");

                // Configurez le levier à 1x
                await _binanceService.SetLeverageAsync(symbol, 1);

                // Configurez le mode de marge (par défaut, isolé)
                //await _binanceService.SetMarginModeAsync(symbol, FuturesMarginType.Isolated);

                decimal currentPrice = await GetCurrentMarketPriceAsync(symbol);

                switch (decision.Action.ToUpper())
                {
                    case "BUY":
                        await PlaceOrderWithStopLossAndTakeProfit(symbol, budget, decision.SL, decision.TP, currentPrice, OrderSide.Buy);
                        break;

                    case "SELL":
                        await PlaceOrderWithStopLossAndTakeProfit(symbol, budget, decision.SL, decision.TP, currentPrice, OrderSide.Sell);
                        break;

                    case "HOLD":
                        _logger.LogInformation($"Aucune action à prendre pour {symbol}. Décision HOLD.");
                        break;

                    default:
                        _logger.LogWarning($"Action inconnue : {decision.Action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'exécution de la décision pour {symbol}.");
                throw;
            }
        }

        private async Task PlaceOrderWithStopLossAndTakeProfit(
            string symbol,
            decimal budget,
            decimal? initialStopLoss,
            decimal? initialTakeProfit,
            decimal currentPrice,
            OrderSide side)
        {
            try
            {
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

                // Validation initiale des niveaux
                ValidateAndAdjustOrderLevels(ref stopLoss, ref takeProfit, currentPrice, side, tickSize);

                // Vérifier si l'ordre respecte le montant minimal requis
                decimal minNotional = await _binanceService.GetMinNotionalAsync(symbol);
                if (quantity * currentPrice < minNotional)
                {
                    throw new Exception($"La valeur totale de l'ordre ({quantity * currentPrice}) est inférieure au montant minimal requis ({minNotional}) pour {symbol}.");
                }

                // Vérifier si le prix actuel change et ajuster le SL/TP dynamiquement
                decimal updatedPrice = await GetCurrentMarketPriceAsync(symbol);
                if (Math.Abs(updatedPrice - currentPrice) > tickSize * 5)
                {
                    _logger.LogWarning($"Prix du marché modifié : ancien = {currentPrice}, nouveau = {updatedPrice}");

                    // Réajuster le SL et le TP
                    currentPrice = updatedPrice;
                    stopLoss = AdjustPriceToTickSize(currentPrice * (1 - slPercentage), tickSize);
                    takeProfit = AdjustPriceToTickSize(currentPrice * (1 + tpPercentage), tickSize);

                    // Validation après ajustement
                    ValidateAndAdjustOrderLevels(ref stopLoss, ref takeProfit, currentPrice, side, tickSize);
                }

                // Placer l'ordre principal (Market Order)
                var mainOrder = await _binanceService.PlaceAdvancedOrderAsync(
                    symbol: symbol,
                    side: side,
                    type: FuturesOrderType.Market,
                    quantity: quantity
                );
                _logger.LogInformation($"Ordre principal {side} placé avec succès : {mainOrder.Id}");

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
                    _logger.LogInformation($"Ordre Stop-Loss placé pour {symbol} à {stopLoss} : {stopOrder.Id}");
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

                    _logger.LogInformation($"Ordre Take-Profit placé pour {symbol} à {takeProfit} : {takeProfitOrder.Id}");
                }

                // Surveiller l'exécution des ordres et annuler l'autre si l'un est exécuté
                //if (stopOrder != null && takeProfitOrder != null)
                //{
                //    await MonitorAndCancelOppositeOrderAsync(symbol, stopOrder.Id, takeProfitOrder.Id);
                //}

                // Logs finaux
                _logger.LogInformation($"Prix actuel : {currentPrice}");
                _logger.LogInformation($"StopLoss final ajusté : {stopLoss}");
                _logger.LogInformation($"TakeProfit final ajusté : {takeProfit}");
                _logger.LogInformation($"Quantité pour l'ordre Take-Profit : {quantity}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du placement des ordres pour {symbol}.");
                throw;
            }
        }

        private async Task MonitorAndCancelOppositeOrderAsync(string symbol, long? stopOrderId, long? takeProfitOrderId)
        {
            while (true)
            {
                var activeOrders = await _binanceService.GetActiveOrdersAsync(symbol);

                if (stopOrderId.HasValue && !activeOrders.Any(o => o.Id == stopOrderId.Value))
                {
                    _logger.LogInformation($"Ordre Stop-Loss {stopOrderId} exécuté. Annulation de l'ordre Take-Profit.");
                    if (takeProfitOrderId.HasValue)
                        await _binanceService.CancelOrderAsync(symbol, takeProfitOrderId.Value);
                    break;
                }

                if (takeProfitOrderId.HasValue && !activeOrders.Any(o => o.Id == takeProfitOrderId.Value))
                {
                    _logger.LogInformation($"Ordre Take-Profit {takeProfitOrderId} exécuté. Annulation de l'ordre Stop-Loss.");
                    if (stopOrderId.HasValue)
                        await _binanceService.CancelOrderAsync(symbol, stopOrderId.Value);
                    break;
                }

                await Task.Delay(1000); // Vérification toutes les secondes
            }
        }



        private void ValidateAndAdjustOrderLevels(
        ref decimal? stopLoss,
        ref decimal? takeProfit,
        decimal currentPrice,
        OrderSide side,
        decimal tickSize)
        {
            if (side == OrderSide.Buy)
            {
                // Ajustement automatique du StopLoss pour un ordre BUY
                if (stopLoss.HasValue && stopLoss >= currentPrice - tickSize * 2)
                {
                    stopLoss = stopLoss - tickSize * 2;
                    Console.WriteLine($"StopLoss ajusté automatiquement à {stopLoss} pour un ordre BUY.");
                }

                // Ajustement automatique du TakeProfit pour un ordre BUY
                if (takeProfit.HasValue && takeProfit <= currentPrice + tickSize * 2)
                {
                    takeProfit = takeProfit + tickSize * 2;
                    Console.WriteLine($"TakeProfit ajusté automatiquement à {takeProfit} pour un ordre BUY.");
                }
            }
            else if (side == OrderSide.Sell)
            {
                // Ajustement automatique du StopLoss pour un ordre SELL
                if (stopLoss.HasValue && stopLoss <= currentPrice + tickSize * 2)
                {
                    stopLoss = stopLoss + tickSize * 2;
                    Console.WriteLine($"StopLoss ajusté automatiquement à {stopLoss} pour un ordre SELL.");
                }

                // Ajustement automatique du TakeProfit pour un ordre SELL
                if (takeProfit.HasValue && takeProfit >= currentPrice - tickSize * 2)
                {
                    takeProfit = takeProfit - tickSize * 2;
                    Console.WriteLine($"TakeProfit ajusté automatiquement à {takeProfit} pour un ordre SELL.");
                }
            }
        }

        private void ValidateOrderLevels(
            decimal currentPrice,
            decimal? stopLoss,
            decimal? takeProfit,
            OrderSide side,
            decimal tickSize)
        {
            if (side == OrderSide.Buy)
            {
                if (stopLoss.HasValue && stopLoss >= currentPrice - tickSize * 2)
                {
                    throw new Exception($"Le StopLoss ({stopLoss}) est mal positionné pour un ordre BUY. Il doit être au moins à {currentPrice - tickSize * 2}.");
                }

                if (takeProfit.HasValue && takeProfit <= currentPrice + tickSize * 2)
                {
                    throw new Exception($"Le TakeProfit ({takeProfit}) est mal positionné pour un ordre BUY. Il doit être au moins à {currentPrice + tickSize * 2}.");
                }
            }
            else if (side == OrderSide.Sell)
            {
                if (stopLoss.HasValue && stopLoss <= currentPrice + tickSize * 2)
                {
                    throw new Exception($"Le StopLoss ({stopLoss}) est mal positionné pour un ordre SELL. Il doit être au moins à {currentPrice + tickSize * 2}.");
                }

                if (takeProfit.HasValue && takeProfit >= currentPrice - tickSize * 2)
                {
                    throw new Exception($"Le TakeProfit ({takeProfit}) est mal positionné pour un ordre SELL. Il doit être au moins à {currentPrice - tickSize * 2}.");
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
                _logger.LogError($"Erreur lors de la récupération du prix actuel pour {symbol} : {result.Error?.Message}");
                throw new Exception($"Erreur lors de la récupération du prix actuel pour {symbol}.");
            }

            _logger.LogInformation($"Prix actuel récupéré pour {symbol} : {result.Data.Price}");
            return result.Data.Price;
        }

        public async Task<decimal> GetAvailableBalance(string symbol)
        {
            // Récupérer les informations du compte Futures via BinanceService
            var accountInfo = await _binanceService.GetAccountInfoAsync();

            if (accountInfo == null)
            {
                _logger.LogError("Erreur lors de la récupération des informations de compte Futures.");
                throw new Exception("Erreur lors de la récupération des informations de compte Futures.");
            }

            // Extraction de l'actif depuis le symbole
            var asset = symbol.Replace("USDT", ""); // Par exemple : "BTC" pour "BTCUSDT"

            // Rechercher le solde pour l'actif
            var balance = accountInfo.Assets.FirstOrDefault(a => a.Asset == asset);

            if (balance == null)
            {
                _logger.LogWarning($"Aucun solde trouvé pour l'actif {asset}.");
                return 0;
            }

            // Retourner le solde disponible
            _logger.LogInformation($"Solde disponible pour {asset} : {balance.AvailableBalance}");
            return balance.AvailableBalance;
        }

    }

}
