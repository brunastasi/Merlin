using System.Text.Json.Serialization;

namespace Moana.Models.ApiData
{
    public class LongShortRatioApiResponse
    {
        [JsonPropertyName("longShortRatio")]
        public decimal LongShortRatio { get; set; } // Ratio long/short

        [JsonPropertyName("longAccount")]
        public decimal LongAccount { get; set; }   // Volume de positions longues

        [JsonPropertyName("shortAccount")]
        public decimal ShortAccount { get; set; }  // Volume de positions courtes

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }        // Timestamp UNIX
    }
}
