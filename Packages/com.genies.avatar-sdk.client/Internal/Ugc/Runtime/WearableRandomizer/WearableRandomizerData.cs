using System;
using System.Collections.Generic;
using Genies.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Holds the data necessary for <see cref="WearableRandomizer"/> to work.
    /// </summary>
    [Serializable]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public sealed class WearableRandomizerData
    {
        public List<string> BasicMaterialIds = new();
        public List<string> AccentMaterialIds = new();
        public List<Pattern> Patterns = new();

        [Serializable]
        public struct Pattern
        {
            public string Id;

            public Color[] DominantColors;
        }

        public static string Serialize(WearableRandomizerData data)
        {
            try
            {
                return JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to serialize {nameof(WearableRandomizerData)}.\n{exception}");
                return null;
            }
        }

        public static WearableRandomizerData Deserialize(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<WearableRandomizerData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to deserialize {nameof(WearableRandomizerData)}.\n{exception}");
                return null;
            }
        }
    }
}
