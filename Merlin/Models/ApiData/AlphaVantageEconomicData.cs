using System.Text.Json.Serialization;

namespace Merlin.Models.ApiData
{
    public class AlphaVantageEconomicData
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }  // Exemple : "2023-01-01"

        [JsonPropertyName("value")]
        public decimal Value { get; set; }  // Exemple : 22671.096
    }
}
