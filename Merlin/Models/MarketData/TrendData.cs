namespace Merlin.Models.MarketData
{
    // Tendances des prix et volumes historiques
    public class TrendData
    {
        public decimal[] HistoricalPrices { get; set; }  // Exemple : prix de clôture
        public decimal SMA { get; set; }  // Moyenne mobile simple
        public decimal EMA { get; set; }  // Moyenne mobile exponentielle
        public decimal SupportLevel { get; set; }
        public decimal ResistanceLevel { get; set; }
    }
}
