using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapeAnimatorFeature", menuName = "Genies/Genie Components/Animation Features/Blend Shape Animator")]
#endif
    [SerializableAs(typeof(IAnimationFeature), "blend-shape-animator-feature")]
    public sealed class BlendShapeAnimatorFeatureAsset : AnimationFeatureAsset, IGenieComponentCreator
    {
        [Tooltip("If enabled, all input channel parameters must be present on the Animator to support the feature")]
        public bool requiresAllChannels;
        public BlendShapeAnimatorConfig config;

        public GenieComponent CreateComponent()
        {
            return new BlendShapeAnimator(name, config);
        }

        public override bool SupportsParameters(AnimatorParameters parameters)
        {
            if (requiresAllChannels)
            {
                foreach (BlendShapeAnimatorConfig.Channel channel in config.channels)
                {
                    if (!parameters.Contains(channel.inputChannelName))
                    {
                        return false;
                    }
                }

                return true;
            }

            // if we don't require all channels, then parameters are supported when at least one channel is contained
            foreach (BlendShapeAnimatorConfig.Channel channel in config.channels)
            {
                if (parameters.Contains(channel.inputChannelName))
                {
                    return true;
                }
            }

            return false;
        }

        public override GenieComponent CreateFeatureComponent(AnimatorParameters parameters)
        {
            return new BlendShapeAnimator(name, config);
        }

        public JToken Serialize()
            => JScriptableObject.FromObject(this);
        public static IAnimationFeature Deserialize(JToken token)
            => Deserialize<BlendShapeAnimatorFeatureAsset>(token);
    }
}
