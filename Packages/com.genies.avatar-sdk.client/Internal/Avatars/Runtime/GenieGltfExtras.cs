using System;
using System.Collections.Generic;
using Genies.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the extras included with our genies glTF exports.
    /// </summary>
    [Serializable, JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public sealed class GenieGltfExtras
    {
        public const string CurrentVersion = "1.0.0";
        public const string VersionKey = "version";
        
        [JsonProperty(VersionKey)]
        public readonly string Version = CurrentVersion;
        
        public string                       species;
        public string                       lod;
        public bool                         isHuman;
        public string                       modelRootPath;
        public string                       skeletonRootPath;
        public SerializableHumanDescription humanDescription;
        public List<JToken>                 components;
        
        public static GenieGltfExtras BuildExtras(IGenie genie)
        {
            return new GenieGltfExtras
            {
                species          = genie.Species,
                lod              = genie.Lod,
                isHuman          = TryGetSerializableHumanDescription(genie, out SerializableHumanDescription humanDescription),
                modelRootPath    = genie.ModelRoot.transform.GetPathRelativeTo(genie.Root.transform),
                skeletonRootPath = genie.SkeletonRoot.GetPathRelativeTo(genie.Root.transform),
                humanDescription = humanDescription,
                components = SerializeComponents(genie),
            };
        }

        public static JToken Serialize(GenieGltfExtras extras)
        {
            return JToken.FromObject(extras);
        }
        
        public static GenieGltfExtras Deserialize(JToken token)
        {
            return token.ToObject<GenieGltfExtras>();
        }

        private static bool TryGetSerializableHumanDescription(IGenie genie, out SerializableHumanDescription serializableHumanDescription)
        {
            Animator animator = genie.Animator;
            Avatar avatar = animator.avatar;
            
            if (animator.isHuman && avatar && avatar.isValid && avatar.isHuman)
            {
                serializableHumanDescription = SerializableHumanDescription.Convert(avatar.humanDescription);
                return true;
            }
            
            serializableHumanDescription = null;
            return false;
        }

        private static List<JToken> SerializeComponents(IGenie genie)
        {
            List<JToken> tokens = genie.Components.SerializeAll();
            return tokens.Count == 0 ? null : tokens;
        }
    }
}