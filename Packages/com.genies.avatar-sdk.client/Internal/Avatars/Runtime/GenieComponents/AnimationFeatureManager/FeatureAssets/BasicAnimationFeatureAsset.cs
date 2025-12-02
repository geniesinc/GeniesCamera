using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Basic <see cref="AnimationFeatureAsset"/> implementation that checks all given parameters are in the animator
    /// and uses a <see cref="GenieComponentAsset"/> reference to create the components.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BasicAnimationFeature", menuName = "Genies/Genie Components/Animation Features/Basic Animation Feature")]
#endif
    public sealed class BasicAnimationFeatureAsset : AnimationFeatureAsset
    {
        public GenieComponentAsset component;
        public List<string> animatorParameters = new();

        public override GenieComponent CreateFeatureComponent(AnimatorParameters parameters)
        {
            return component.CreateComponent();
        }

        public override bool SupportsParameters(AnimatorParameters parameters)
        {
            // Input parameters must contain all param strings declared on this asset

            foreach (string parameter in animatorParameters)
            {
                if (!parameters.Contains(parameter))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
