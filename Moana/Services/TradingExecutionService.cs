using Binance.Net.Enums;
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
                        await PlaceOrderWithStopLossAndTakeProfit(symbol, budget, decision.StopLoss, decision.TakeProfit, currentPrice, OrderSide.Buy);
                        break;

                    case "SELL":
                        await PlaceOrderWithStopLossAndTakeProfit(symbol, budget, decision.StopLoss, decision.TakeProfit, currentPrice, OrderSide.Sell);
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
            decimal? stopLoss,
            decimal? takeProfit,
            decimal currentPrice,
            OrderSide side)
        {
            try
            {
                // Obtenir le TickSize pour ajuster les prix
                decimal tickSize = await _binanceService.GetTickSizeAsync(symbol);

                // Ajustement des quantités
                int quantityPrecision = await _binanceService.GetAssetPrecisionAsync(symbol);
                var quantity = budget / currentPrice;
                quantity = AdjustToPrecision(quantity, quantityPrecision);

                if (quantity <= 0)
                {
                    throw new Exception($"Quantité trop petite après ajustement : {quantity}");
                }

                // Ajustement des niveaux pour le TickSize
                if (stopLoss.HasValue)
                {
                    stopLoss = AdjustPriceToTickSize(stopLoss.Value, tickSize);
                }
                if (takeProfit.HasValue)
                {
                    takeProfit = AdjustPriceToTickSize(takeProfit.Value, tickSize);
                }

                // Validation renforcée des niveaux
                ValidateOrderLevels(currentPrice, stopLoss, takeProfit, side, tickSize);

                // Vérification du montant minimal requis
                decimal minNotional = await _binanceService.GetMinNotionalAsync(symbol);
                if (quantity * currentPrice < minNotional)
                {
                    throw new Exception($"La valeur totale de l'ordre ({quantity * currentPrice}) est inférieure au montant minimal requis ({minNotional}) pour {symbol}.");
                }

                // Vérification du prix actuel et réajustement si nécessaire
                decimal updatedPrice = await GetCurrentMarketPriceAsync(symbol);
                if (Math.Abs(updatedPrice - currentPrice) > tickSize * 5)
                {
                    _logger.LogWarning($"Prix du marché modifié : ancien = {currentPrice}, nouveau = {updatedPrice}");
                    currentPrice = updatedPrice;
                    stopLoss = AdjustPriceToTickSize(currentPrice * 0.95m, tickSize);
                    takeProfit = AdjustPriceToTickSize(currentPrice * 1.20m, tickSize);
                }

                // Placer l'ordre principal
                var mainOrder = await _binanceService.PlaceAdvancedOrderAsync(
                    symbol: symbol,
                    side: side,
                    type: FuturesOrderType.Market,
                    quantity: quantity
                );
                _logger.LogInformation($"Ordre principal {side} placé avec succès : {mainOrder.Id}");

                // Placer Stop-Loss si défini
                if (stopLoss.HasValue)
                {
                    var stopOrder = await _binanceService.PlaceAdvancedOrderAsync(
                        symbol: symbol,
                        side: side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                        type: FuturesOrderType.StopMarket,
                        quantity: quantity,
                        stopPrice: stopLoss
                    );
                    _logger.LogInformation($"Ordre Stop-Loss placé pour {symbol} à {stopLoss} : {stopOrder.Id}");
                }

                // Placer Take-Profit si défini
                if (takeProfit.HasValue)
                {
                    var takeProfitOrder = await _binanceService.PlaceAdvancedOrderAsync(
                        symbol: symbol,
                        side: side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                        type: FuturesOrderType.TakeProfit,
                        quantity: quantity,
                        price: takeProfit, // Ajouter le paramètre 'price' ici
                        stopPrice: takeProfit
                    );
                    _logger.LogInformation($"Ordre Take-Profit placé pour {symbol} à {takeProfit} : {takeProfitOrder.Id}");
                }

                // Logs des ajustements
                _logger.LogInformation($"Prix actuel : {currentPrice}");
                _logger.LogInformation($"StopLoss ajusté : {stopLoss}");
                _logger.LogInformation($"TakeProfit ajusté : {takeProfit}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du placement des ordres pour {symbol}.");
                throw;
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
