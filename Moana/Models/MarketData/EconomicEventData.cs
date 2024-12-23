namespace Moana.Models.MarketData
{
    // Evènements économiques majeurs
    public class EconomicEventData
    {
        public DateTime EventDate { get; set; }    // Date de l'événement
        public string Title { get; set; }         // Titre de l'événement
        public string Country { get; set; }       // Pays concerné
        public string ImpactLevel { get; set; }   // Niveau d'impact (High, Medium, Low)
        public string Description { get; set; }   // Description ou résumé de l'événement

        public decimal? TreasuryYield { get; set; }
        public decimal? CPI { get; set; }
        public decimal? InflationRate { get; set; }
        public decimal? UnemploymentRate { get; set; }
    }
}
