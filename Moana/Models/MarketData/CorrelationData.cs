namespace Moana.Models.MarketData
{
    // Corrélations avec d'autres actifs
    public class CorrelationData
    {
        public string Asset1 { get; set; } // Premier actif (exemple : BTC/USD)
        public string Asset2 { get; set; } // Deuxième actif (exemple : ETH/USD)
        public decimal CorrelationCoefficient { get; set; } // Corrélation (-1 à 1)
        public string CorrelationType => CorrelationCoefficient switch
        {
            >= 0.8m => "Strong Positive",
            <= -0.8m => "Strong Negative",
            _ => "Weak or None"
        };
        public DateTime CalculatedDate { get; set; }
    }
}
