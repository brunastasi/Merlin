using System.Text.Json.Serialization;

namespace Merlin.Models.ApiData
{
    public class AlphaVantageApiResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }  // Exemple : "Real Gross Domestic Product"

        [JsonPropertyName("interval")]
        public string Interval { get; set; }  // Exemple : "annual"

        [JsonPropertyName("unit")]
        public string Unit { get; set; }  // Exemple : "billions of dollars"

        [JsonPropertyName("data")]
        public List<AlphaVantageEconomicData> Data { get; set; } = new();
    }
}
