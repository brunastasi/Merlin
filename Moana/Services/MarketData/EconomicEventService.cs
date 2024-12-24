using Moana.Models.MarketData;

namespace Moana.Services.MarketData
{
    public class EconomicEventService
    {
        private readonly AlphaVantageService _alphaVantageService;

        public EconomicEventService(AlphaVantageService alphaVantageService)
        {
            _alphaVantageService = alphaVantageService;
        }

        /// <summary>
        /// Récupère les événements économiques en combinant les données de plusieurs indicateurs (GDP, CPI, Treasury Yield, Unemployment Rate, Inflation).
        /// </summary>
        /// <returns>Liste des événements économiques pertinents.</returns>
        public async Task<List<EconomicEventData>> GetEconomicEventsAsync()
        {
            var events = new List<EconomicEventData>();

            // Date minimale pour filtrer les données
            var minDate = new DateTime(2000, 1, 1);

            try
            {
                // 1. Récupération des données du PIB réel (Real GDP)
                var realGDPData = await _alphaVantageService.GetRealGDPAsync();
                events.AddRange(realGDPData
                    .Where(d => d.Date >= minDate)
                    .Select(d => new EconomicEventData
                    {
                        EventDate = d.Date,
                        Title = "Real Gross Domestic Product (GDP)",
                        Description = $"Valeur : {d.Value} billions of dollars",
                        ImpactLevel = d.Value > 20000 ? "High" : d.Value > 15000 ? "Medium" : "Low",
                        Country = "United States"
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la récupération des données du PIB réel.", ex);
            }

            try
            {
                // 2. Récupération des données du taux des bons du Trésor (Treasury Yield)
                var treasuryYieldData = await _alphaVantageService.GetTreasuryYieldAsync();
                events.AddRange(treasuryYieldData
                    .Where(d => d.Date >= minDate)
                    .Select(d => new EconomicEventData
                    {
                        EventDate = d.Date,
                        Title = "Treasury Yield",
                        Description = $"Valeur : {d.Value} %",
                        ImpactLevel = d.Value > 3 ? "High" : d.Value > 1.5m ? "Medium" : "Low",
                        Country = "United States"
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la récupération des données des taux des bons du Trésor.", ex);
            }

            try
            {
                // 3. Récupération des données de l'indice des prix à la consommation (CPI)
                var cpiData = await _alphaVantageService.GetCPIAsync();
                events.AddRange(cpiData
                    .Where(d => d.Date >= minDate)
                    .Select(d => new EconomicEventData
                    {
                        EventDate = d.Date,
                        Title = "Consumer Price Index (CPI)",
                        Description = $"Valeur : {d.Value}",
                        ImpactLevel = d.Value > 5 ? "High" : d.Value > 2 ? "Medium" : "Low",
                        Country = "United States"
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la récupération des données du CPI.", ex);
            }

            try
            {
                // 4. Récupération des données du taux de chômage (Unemployment Rate)
                var unemploymentRateData = await _alphaVantageService.GetUnemploymentRateAsync();
                events.AddRange(unemploymentRateData
                    .Where(d => d.Date >= minDate)
                    .Select(d => new EconomicEventData
                    {
                        EventDate = d.Date,
                        Title = "Unemployment Rate",
                        Description = $"Valeur : {d.Value} %",
                        ImpactLevel = d.Value > 7 ? "High" : d.Value > 4 ? "Medium" : "Low",
                        Country = "United States"
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la récupération des données du taux de chômage.", ex);
            }

            try
            {
                // 5. Récupération des données de l'inflation annuelle (Inflation)
                var inflationData = await _alphaVantageService.GetInflationAsync();
                events.AddRange(inflationData
                    .Where(d => d.Date >= minDate)
                    .Select(d => new EconomicEventData
                    {
                        EventDate = d.Date,
                        Title = "Annual Inflation Rate",
                        Description = $"Valeur : {d.Value} %",
                        ImpactLevel = d.Value > 5 ? "High" : d.Value > 2 ? "Medium" : "Low",
                        Country = "United States"
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la récupération des données de l'inflation annuelle.", ex);
            }

            // Tri par date décroissante et limitation à 50 événements récents
            return events
                .OrderByDescending(e => e.EventDate)
                .Take(5)
                .ToList();
        }

    }
}
