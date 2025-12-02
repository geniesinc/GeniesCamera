using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Base <see cref="ScriptableObject"/> class to create <see cref="IAnimationFeature"/> assets.
    /// </summary>
    public abstract class AnimationFeatureAsset : ScriptableObject, IAnimationFeature
    {
        public abstract bool SupportsParameters(AnimatorParameters parameters);
        public abstract GenieComponent CreateFeatureComponent(AnimatorParameters parameters);
        
        public static AnimationFeatureAssetRef Deserialize<T>(JToken token)
            where T : AnimationFeatureAsset
        {
            if (token is not JObject obj)
            {
                return null;
            }

            if (token is not JScriptableObject soToken)
            {
                soToken = new JScriptableObject(obj);
            }

            UnityObjectRef<T> assetRef = soToken.ToScriptableObject<T>();
            return new AnimationFeatureAssetRef(assetRef.Object, assetRef);
        }
    }
}