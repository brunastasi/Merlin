namespace Moana.Models.MarketData
{
    // Sentiment du marché
    public class SentimentData
    {
        public string SocialSentiment { get; set; }  // Exemple : bullish, bearish, neutral
        public int FearGreedIndex { get; set; }      // Indice de peur et avidité (0-100)
    }
}
