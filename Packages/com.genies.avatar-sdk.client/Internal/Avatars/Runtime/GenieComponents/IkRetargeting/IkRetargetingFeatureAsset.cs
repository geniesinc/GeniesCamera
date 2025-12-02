using Genies.Utilities;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// IK retargeting feature for <see cref="AnimationFeatureManager"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "IkRetargetingFeature", menuName = "Genies/Genie Components/Animation Features/IK Retargeting")]
#endif
    [SerializableAs(typeof(IAnimationFeature), "ik-retargeting-feature")]
    public sealed class IkRetargetingFeatureAsset : AnimationFeatureAsset, IGenieComponentCreator
    {
        public IkrConfig config;
        [Tooltip("If true, IK hints will be set to the transforms coming from the animation clip")]
        public bool setIkHints;

        public List<GenieGroundIkrTarget.Config> groundTargetConfigs = new();

        public GenieComponent CreateComponent()
        {
            return new IkRetargeting(config, setIkHints, autoRebuild: true, groundTargetConfigs);
        }

        public override GenieComponent CreateFeatureComponent(AnimatorParameters parameters)
        {
            /**
             * If creating the component as an animation feature then we don't want to have autoRebuild enabled since
             * the animation feature manager will take care of adding/removing the component when the parameters change
             */
            return new IkRetargeting(config, setIkHints, autoRebuild: false, groundTargetConfigs);
        }

        public override bool SupportsParameters(AnimatorParameters parameters)
        {
            // if no config then nothing is supported (should we allow this for ground target configs only?)
            if (!config)
            {
                return false;
            }

            // check if any ground target configs targets an existing parameter
            foreach (GenieGroundIkrTarget.Config groundTarget in groundTargetConfigs)
            {
                if (parameters.Contains(groundTarget.weightProperty))
                {
                    return true;
                }
            }

            // check if any IKR goal configs targets an existing parameter
            foreach (IkrConfig.Goal goal in config.goals)
            {
                foreach (TransformIkrTarget.Config target in goal.transformTargets)
                {
                    if (parameters.Contains(target.weightProperty))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public JToken Serialize()
            => JScriptableObject.FromObject(this);
        public static IAnimationFeature Deserialize(JToken token)
            => Deserialize<IkRetargetingFeatureAsset>(token);
    }
}
