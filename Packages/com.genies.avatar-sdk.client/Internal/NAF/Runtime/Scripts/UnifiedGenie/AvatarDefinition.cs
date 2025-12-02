using System;
using System.Collections.Generic;
using GnWrappers;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Naf
{

    /// <summary>
    /// NAF avatar definition for the <see cref="NativeUnifiedGenieController"/>.
    ///
    /// Requires 'Newtonsoft.Json-for-Unity.Converters.asset' from 'com.genies.utilities' in the project to deserialize.
    /// Otherwise, deserializing will crash the application.
    ///
    /// Requires:
    /// https://github.com/geniesinc/guppy/blob/main/external/com.genies.utilities/Runtime/Resources/Newtonsoft.Json-for-Unity.Converters.asset
    /// </summary>
    [Serializable]
    public sealed class AvatarDefinition
    {
        [JsonProperty("JsonVersion", Required = Required.Always)]
        public readonly string JsonVersion = "2-0-0";

        public List<string>                           equippedAssetIds  = new();
        public Dictionary<string, Color>              colors            = new();
        public Dictionary<string, float>              bodyAttributes    = new();
        public Dictionary<MegaSkinTattooSlot, string> equippedTattooIds = new();
    }
}
