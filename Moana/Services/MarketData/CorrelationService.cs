using Moana.Models.MarketData;
using Moana.Services.Utils;

namespace Moana.Services.MarketData
{
    public class CorrelationService
    {
        private readonly AlphaVantageService _alphaVantageService;

        public CorrelationService(AlphaVantageService alphaVantageService)
        {
            _alphaVantageService = alphaVantageService;
        }

        public async Task<List<CorrelationData>> GetCorrelationDataAsync(List<(string Asset, string Type)> assets, string interval = "daily")
        {
            var correlations = new List<CorrelationData>();

            for (int i = 0; i < assets.Count; i++)
            {
                for (int j = i + 1; j < assets.Count; j++)
                {
                    try
                    {
                        // Récupération des prix pour les deux actifs
                        var prices1 = await _alphaVantageService.GetHistoricalPricesAsync(assets[i].Asset, assets[i].Type, interval);
                        var prices2 = await _alphaVantageService.GetHistoricalPricesAsync(assets[j].Asset, assets[j].Type, interval);

                        // Calcul de la corrélation
                        var correlation = CorrelationCalculations.CalculatePearsonCorrelation(prices1, prices2);

                        // Ajout des données de corrélation
                        correlations.Add(new CorrelationData
                        {
                            Asset1 = assets[i].Asset,
                            Asset2 = assets[j].Asset,
                            CorrelationCoefficient = Math.Round(correlation, 2),
                            CalculatedDate = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        // Gestion des erreurs pour un actif individuel
                        Console.WriteLine($"Erreur lors de la récupération ou du calcul pour {assets[i].Asset} et {assets[j].Asset}: {ex.Message}");
                    }
                }
            }

            return correlations;
        }
    }
}
