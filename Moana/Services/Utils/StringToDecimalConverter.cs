using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moana.Services.Utils
{
    public class StringToDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Si la valeur est une chaîne, essaie de la convertir avec une culture appropriée
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }

                throw new JsonException($"Impossible de convertir la valeur JSON en decimal : {stringValue}");
            }

            // Si c'est un nombre, retourne-le directement
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }

            throw new JsonException($"Le type de jeton JSON n'est pas pris en charge : {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

}
