namespace Merlin.Services.Utils
{
    public static class ImpactKeywords
    {
        /// <summary>
        /// Liste pondérée des mots-clés positifs.
        /// Chaque mot-clé est associé à un poids reflétant son importance dans l'analyse.
        /// </summary>
        public static readonly Dictionary<string, int> WeightedPositiveKeywords = new Dictionary<string, int>
        {
            { "growth", 5 },
            { "profit", 4 },
            { "upgrade", 3 },
            { "increase", 4 },
            { "rise", 3 },
            { "record", 5 },
            { "success", 4 },
            { "surge", 4 },
            { "gain", 3 },
            { "positive", 2 },
            { "improvement", 4 },
            { "strong", 3 },
            { "high", 2 }
        };

        /// <summary>
        /// Liste pondérée des mots-clés négatifs.
        /// Chaque mot-clé est associé à un poids reflétant son impact négatif.
        /// </summary>
        public static readonly Dictionary<string, int> WeightedNegativeKeywords = new Dictionary<string, int>
        {
            { "loss", 5 },
            { "decrease", 4 },
            { "downgrade", 3 },
            { "failure", 5 },
            { "drop", 4 },
            { "risk", 3 },
            { "decline", 4 },
            { "fall", 4 },
            { "negative", 2 },
            { "weak", 3 },
            { "low", 2 },
            { "bearish", 3 },
            { "crash", 5 },
            { "collapse", 5 },
            { "uncertain", 3 }
        };
    }
}
