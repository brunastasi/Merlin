namespace Merlin.Models.MarketData
{
    // Conditions de liquidité
    public class LiquidityData
    {
        public decimal OrderBookDepth { get; set; }  // Profondeur du carnet d'ordres
        public decimal Slippage { get; set; }       // Slippage estimé pour de grosses transactions
        public decimal Spread { get; set; }         // Écart entre le meilleur bid et le meilleur ask
        public decimal TopOrderVolume { get; set; } // Volume cumulé des 10 premiers niveaux
        public decimal LiquidityScore { get; set; } // Score agrégé de la liquidité
    }

}
