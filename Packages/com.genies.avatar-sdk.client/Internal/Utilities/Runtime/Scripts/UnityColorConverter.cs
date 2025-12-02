using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Custom JsonConverter for UnityEngine.Color. It allows us to set Color type fields that are automatically serialized/deserialized to/from json files.
    /// Use the JsonConverterAttribute on properties of type color to apply this converter automatically.
    /// </summary>
    [Obsolete("All unity types should be convertable by default now that we use the Json.NET Converters package. There is no need to use any of our custom Unity converters anymore")]
    public class UnityColorConverter : JsonConverter<Color>
    {
        // Add parameterless constructor
        public UnityColorConverter() { }

        public override void WriteJson(JsonWriter writer, Color color, JsonSerializer serializer)
        {
            writer.WriteValue($"#{ColorUtility.ToHtmlStringRGBA(color)}");
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var stringValue = (string)reader.Value;
            var formattedValue = stringValue.ToLower();

            if (!formattedValue.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                formattedValue = $"#{formattedValue}";
            }

            if (ColorUtility.TryParseHtmlString(formattedValue, out var color))
            {
                return color;
            }

            Debug.LogError($"Failed to parse color: {stringValue}");
            return Color.magenta;
        }
    }
}
