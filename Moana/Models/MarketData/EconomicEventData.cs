namespace Moana.Models.MarketData
{
    // Evènements économiques majeurs
    public class EconomicEventData
    {
        public string EventName { get; set; }     // Nom de l’événement (ex. NFP, Taux d'intérêt)
        public DateTime EventDate { get; set; } // Date de l'événement
        public string ImpactLevel { get; set; }   // Impact attendu (High, Medium, Low)
    }
}
