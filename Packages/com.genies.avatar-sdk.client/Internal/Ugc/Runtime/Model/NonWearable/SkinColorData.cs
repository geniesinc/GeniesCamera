using System;
using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Ugc
{
    [Serializable]
    public class SkinColorData
    {
        public string Id;
        [JsonProperty("BaseColor")]
        [JsonConverter(typeof(FlexibleColorConverter))]
        public Color BaseColor;
    }
}
