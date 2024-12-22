namespace Moana.Models.MarketData
{
    // Analyses fondamentales ou économiques
    public class FundamentalData
    {
        public string NewsHeadline { get; set; }
        public string Source { get; set; }
        public DateTime PublishedDate { get; set; }
        public string ImpactLevel { get; set; }  // Exemple : High, Medium, Low
    }
}
