using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Genies.Ugc
{
    [Serializable]
    public class LocalStoredStyleStates
    {
        [JsonProperty("styles")]
        public readonly Dictionary<string, string> Styles = new Dictionary<string, string>();
    }
}
