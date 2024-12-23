namespace Moana.Models.ApiData
{
    public class FearGreedData
    {
        public int Value { get; set; }              // Valeur de l'indice
        public string ValueClassification { get; set; } // Catégorie (Fear, Greed, etc.)
        public string Timestamp { get; set; }       // Date et heure
    }
}
