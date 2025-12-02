using System;
using Newtonsoft.Json;

namespace Genies.Inventory
{
    [Serializable]
    public class PipelineVersion
    {
        [JsonConverter(typeof(DefaultValueConverter))]
        public int min;

        [JsonConverter(typeof(DefaultValueConverter))]
        public int max;
    }

    public class DefaultValueConverter : JsonConverter<int>
    {
        public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                return Convert.ToInt32(reader.Value);
            }
            catch
            {
                // Return default value (-1) if deserialization fails
                return -1;
            }
        }

        public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
