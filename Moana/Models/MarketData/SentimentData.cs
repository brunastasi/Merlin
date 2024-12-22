namespace Moana.Models.MarketData
{
    // Sentiment du marché
    public class SentimentData
    {
        public decimal SentimentScore { get; set; } // Entre -1 (très négatif) et +1 (très positif)
        public string Sentiment { get; set; } // Positif, Neutre, Négatif
        public int FearGreedIndex { get; set; } // Entre 0 et 100
    }

}
