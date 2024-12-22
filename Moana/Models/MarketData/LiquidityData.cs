namespace Moana.Models.MarketData
{
    // Conditions de liquidité
    public class LiquidityData
    {
        public decimal OrderBookDepth { get; set; }  // Profondeur du carnet d'ordres
        public decimal Slippage { get; set; }       // Slippage estimé pour de grosses transactions
    }
}
