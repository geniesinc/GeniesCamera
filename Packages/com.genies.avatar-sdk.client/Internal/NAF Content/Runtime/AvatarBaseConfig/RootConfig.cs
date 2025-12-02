using System;
using Newtonsoft.Json;

namespace Genies.Inventory
{
    [Serializable]
    public class RootConfig
    {
        [JsonProperty("AvatarBase")] 
        public AvatarBase avatarBase;
    }
    
    
    [Serializable]
    public class AvatarBase
    {
        [JsonProperty("version")] 
        public string version;
    }
}