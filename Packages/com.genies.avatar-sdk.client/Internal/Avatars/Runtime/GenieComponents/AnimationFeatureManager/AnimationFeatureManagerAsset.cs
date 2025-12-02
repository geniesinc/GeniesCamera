using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "AnimationFeatureManager", menuName = "Genies/Genie Components/Animation Feature Manager")]
#endif
    public sealed class AnimationFeatureManagerAsset : GenieComponentAsset
    {
        [Tooltip("Whether or not the manager should automatically refresh features when the animator parameters have changed")]
        public bool autoRefreshFeatures = true;
        public List<AnimationFeatureAsset> features = new();


        public override GenieComponent CreateComponent()
        {
            return new AnimationFeatureManager(features)
            {
                AutoRefreshFeatures = autoRefreshFeatures
            };
        }
    }
}
