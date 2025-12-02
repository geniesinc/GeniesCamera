using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Configuration asset to create the mappings between animator parameters and blendshapes used by the <see cref="BlendShapeAnimatorBehaviour"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapeAnimatorConfig", menuName = "Genies/Genie Components/Configs/Blend Shape Animator Config")]
#endif
    public class BlendShapeAnimatorConfig : ScriptableObject
    {
        public List<Channel> channels = new();

        public enum ChannelRetargetBehavior
        {
            Unchanged = 0,
            PositiveControl = 1,
            NegativeControl = 2,
            TargetWeight = 3
        }

        [Serializable]
        public class Channel
        {
            public string inputChannelName;
            public DrivenAttribute[] drivenAttributes;
        }

        [Serializable]
        public class DrivenAttribute
        {
            public string outputChannelName;
            public string[] targetSubmeshes;
            public ChannelRetargetBehavior retargetBehavior;
            public float targetWeight;
        }
    }
}
