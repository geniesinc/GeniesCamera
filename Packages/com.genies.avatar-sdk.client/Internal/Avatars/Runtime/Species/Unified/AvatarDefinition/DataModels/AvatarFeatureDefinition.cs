using System;
using Newtonsoft.Json;

namespace Genies.Avatars.Definition.DataModels
{
    [Serializable]
    public abstract class AvatarFeatureDefinition
    {
        [JsonProperty("JsonVersion", Required = Required.Always)]
        public string JsonVersion => GetDefinitionVersion();

        [JsonProperty("FeatureKey", Required = Required.Always)]
        public string FeatureKey => GetFeatureKey();
        
        protected abstract string GetFeatureKey();
        protected abstract string GetDefinitionVersion();
    }
}