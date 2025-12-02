using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Custom JsonConverter for UnityEngine.Color with higher precision than <see cref="UnityColorConverter"/>. It saves
    /// the colors as a float array for RGB components rather than as a hex string.
    /// </summary>
    [Obsolete("All unity types should be convertable by default now that we use the Json.NET Converters package. There is no need to use any of our custom Unity converters anymore")]
    public class PreciseUnityColorConverter : JsonConverter<Color>
    {
        // Add parameterless constructor
        public PreciseUnityColorConverter() { }

        public override void WriteJson(JsonWriter writer, Color color, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(color.r);
            writer.WriteValue(color.g);
            writer.WriteValue(color.b);
            writer.WriteEndArray();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType is not JsonToken.StartArray)
            {
                Debug.LogError("Failed to parse Unity color");
                return Color.magenta;
            }

            var color = serializer.Deserialize<float[]>(reader);
            return new Color(color[0], color[1], color[2]);
        }
    }
}
