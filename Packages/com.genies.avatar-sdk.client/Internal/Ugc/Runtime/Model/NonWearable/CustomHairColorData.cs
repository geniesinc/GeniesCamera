using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Custom JSON converter for Unity Color that handles both hex strings and Color objects
    /// </summary>
    public class FlexibleColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            // Write as hex string for consistency and to avoid circular reference
            string hexString = $"#{ColorUtility.ToHtmlStringRGBA(value)}";
            writer.WriteValue(hexString);
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                // Handle hex string format for backward compatibility
                string hexString = reader.Value.ToString();
                if (ColorUtility.TryParseHtmlString(hexString, out Color color))
                {
                    return color;
                }
                // If parsing fails, return black as fallback
                return Color.black;
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                // Handle Color object format
                return serializer.Deserialize<Color>(reader);
            }
            
            // Fallback to black if we can't parse
            return Color.black;
        }
    }

    /// <summary>
    /// Data for custom hair.
    /// </summary>
    [Serializable]
    public class CustomHairColorData
    {
        [JsonProperty("Id")]
        public string Id;
        
        [JsonProperty("ColorBase")]
        [JsonConverter(typeof(FlexibleColorConverter))]
        public Color ColorBase = Color.black;
        
        [JsonProperty("ColorR")]
        [JsonConverter(typeof(FlexibleColorConverter))]
        public Color ColorR = Color.black;
        
        [JsonProperty("ColorG")]
        [JsonConverter(typeof(FlexibleColorConverter))]
        public Color ColorG = Color.black;
        
        [JsonProperty("ColorB")]
        [JsonConverter(typeof(FlexibleColorConverter))]
        public Color ColorB = Color.black;
    }
}
