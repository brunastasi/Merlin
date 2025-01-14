using Merlin.Models.MarketData;
using Merlin.Services.Utils;

namespace Merlin.Services.MarketData
{
    public class FundamentalService
    {
        private readonly NewsAPIService _newsAPIService;

        public FundamentalService(NewsAPIService newsAPIService)
        {
            _newsAPIService = newsAPIService;
        }

        public async Task<List<FundamentalData>> GetMarketNewsAsync(string symbol)
        {
            var newsApiResponse = await _newsAPIService.GetNewsAsync(symbol);

            // Mapper les données de NewsAPI en FundamentalData
            return newsApiResponse.Articles.Select(article => new FundamentalData
            {
                Date = article.PublishedAt,
                Title = article.Title,
                Description = article.Description,
                Impact = AnalyzeImpact(article.Title, article.Description),
                Summary = article.Description,
                Source = article.Source?.Name
            }).ToList();
        }

        /// <summary>
        /// Analyse l'impact d'un titre et d'une description en fonction de mots-clés pondérés.
        /// Renvoie "Positive", "Negative", ou "Neutral".
        /// </summary>
        /// <param name="title">Le titre de l'article à analyser.</param>
        /// <param name="description">La description de l'article à analyser.</param>
        /// <returns>
        /// "Positive" si le score des mots-clés positifs est supérieur,
        /// "Negative" si le score des mots-clés négatifs est supérieur,
        /// "Neutral" en cas d'égalité ou d'absence de mots-clés significatifs.
        /// </returns>
        private string AnalyzeImpact(string title, string description)
        {
            // Combine le titre et la description en un seul texte, et le convertit en minuscule
            var text = $"{title} {description}".ToLower();

            // Calcule le score total des mots-clés positifs présents dans le texte
            int positiveScore = ImpactKeywords.WeightedPositiveKeywords.Sum(keyword =>
                text.Contains(keyword.Key) ? keyword.Value : 0);

            // Calcule le score total des mots-clés négatifs présents dans le texte
            int negativeScore = ImpactKeywords.WeightedNegativeKeywords.Sum(keyword =>
                text.Contains(keyword.Key) ? keyword.Value : 0);

            // Compare les scores et renvoie le résultat approprié
            if (positiveScore > negativeScore)
                return "Positive";
            else if (negativeScore > positiveScore)
                return "Negative";
            else
                return "Neutral";
        }
    }
}
