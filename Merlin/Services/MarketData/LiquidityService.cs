using Binance.Net.Objects.Models.Spot;
using Merlin.Models.MarketData;

namespace Merlin.Services.MarketData
{
    public class LiquidityService
    {
        private readonly BinanceService _binanceService;

        public LiquidityService(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        /// <summary>
        /// Récupère les données de liquidité pour un actif donné.
        /// </summary>
        /// <param name="symbol">Symbole de l’actif (ex. : BTCUSDT)</param>
        /// <returns>Instance de LiquidityData.</returns>
        public async Task<LiquidityData> GetLiquidityDataAsync(string symbol, decimal amount)
        {
            // Récupérer le carnet d'ordres via BinanceService
            var orderBook = await _binanceService.GetOrderBookAsync(symbol, 100);

            // Calcul du Spread
            var bestBid = orderBook.Bids.First().Price;
            var bestAsk = orderBook.Asks.First().Price;
            var spread = bestAsk - bestBid;

            // Profondeur cumulée des 10 premiers ordres
            var topOrderVolume = orderBook.Bids.Take(10).Sum(o => o.Quantity) + orderBook.Asks.Take(10).Sum(o => o.Quantity);

            // Calcul d'un slippage dynamique
            var slippageAmount = Math.Min(orderBook.Asks.Sum(o => o.Quantity) * bestAsk, amount * 0.2m); // 20% du montant testé
            var slippage = CalculateSlippage(orderBook, slippageAmount);


            // Score de liquidité (ajout du slippage dans l'évaluation)
            var liquidityScore = topOrderVolume / spread - Math.Max(0, slippage * 100); // Pénalité basée sur le slippage


            return new LiquidityData
            {
                OrderBookDepth = topOrderVolume,
                Spread = spread,
                TopOrderVolume = topOrderVolume,
                Slippage = slippage,
                LiquidityScore = liquidityScore
            };
        }


        /// <summary>
        /// Calcule le slippage estimé pour un montant donné.
        /// </summary>
        private decimal CalculateSlippage(BinanceOrderBook orderBook, decimal amount)
        {
            decimal totalCost = 0;
            decimal remainingAmount = amount;

            foreach (var ask in orderBook.Asks)
            {
                if (remainingAmount <= ask.Quantity)
                {
                    totalCost += remainingAmount * ask.Price;
                    remainingAmount = 0; // Montant couvert
                    break;
                }

                totalCost += ask.Quantity * ask.Price;
                remainingAmount -= ask.Quantity;
            }

            // Si le montant restant est supérieur à 0, utiliser le volume disponible
            var availableAmount = amount - remainingAmount;

            if (availableAmount == 0)
            {
                Console.WriteLine($"Aucun volume disponible pour un ordre de {amount}");
                return -1; // Signal que le carnet ne peut pas couvrir l’ordre
            }

            var averagePrice = totalCost / availableAmount; // Prix moyen pondéré sur le volume disponible
            var slippage = (averagePrice - orderBook.Asks.First().Price) / orderBook.Asks.First().Price;

            Console.WriteLine($"Volume disponible : {availableAmount}, Prix moyen : {averagePrice}, Slippage : {slippage}");
            return slippage;
        }
    }
}
