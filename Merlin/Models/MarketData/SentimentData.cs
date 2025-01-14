namespace Merlin.Models.MarketData
{
    // Sentiment du marché
    public class SentimentData
    {
        public int FearGreedIndex { get; set; }      // Indice de peur et avidité (0-100)
        public string SentimentClassification { get; set; } // Classification du sentiment (Greedy, Fearful, etc.)
    }

}
