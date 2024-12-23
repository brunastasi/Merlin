namespace Moana.Models.MarketData
{
    // Analyses fondamentales ou économiques
    public class FundamentalData
    {
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Impact { get; set; } // Positif, Neutre, Négatif
        public string Summary { get; set; } // Résumé ou description de l'information
        public string Source { get; set; }  // Source sous forme de chaîne
    }
}
