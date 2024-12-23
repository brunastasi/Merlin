namespace Moana.Models.MarketData
{
    // Données des marchés de dérivés.
    public class DerivativesData
    {
        public decimal OpenInterest { get; set; }  // Intérêt ouvert sur les contrats
        public decimal FundingRate { get; set; }  // Taux de financement des contrats perpétuels
        public decimal LongShortRatio { get; set; } // Ratio des positions longues/courtes
        public decimal LongPositions { get; set; }  // Volume des positions longues
        public decimal ShortPositions { get; set; } // Volume des positions courtes
        public decimal FuturesVolume { get; set; } // Volume sur les futures
        public DateTime LastUpdated { get; set; } // Dernière mise à jour
    }
}
