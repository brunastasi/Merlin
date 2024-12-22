namespace Moana.Models.MarketData
{
    // Données des marchés de dérivés.
    public class DerivativesData
    {
        public decimal OpenInterest { get; set; }  // Intérêt ouvert sur les contrats
        public decimal FundingRate { get; set; }  // Taux de financement des contrats perpétuels
        public decimal LongShortRatio { get; set; } // Ratio des positions longues/courtes
    }
}
