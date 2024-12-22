namespace Moana.Models.MarketData
{
    // Corrélations avec d'autres actifs
    public class CorrelationData
    {
        public string CorrelatedSymbol { get; set; }  // Actif corrélé (ex. BTC)
        public decimal CorrelationCoefficient { get; set; } // Coefficient de corrélation (-1 à 1)
    }
}
