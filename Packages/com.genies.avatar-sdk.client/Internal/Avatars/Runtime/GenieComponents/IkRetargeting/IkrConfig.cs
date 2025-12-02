using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "IKRetargetingConfig", menuName = "Genies/Genie Components/Configs/IK Retargeting Config")]
#endif
    public sealed class IkrConfig : ScriptableObject
    {
        public List<Goal> goals = new();

        [Serializable]
        public struct Goal
        {
            public AvatarIKGoal goal;
            public List<TransformIkrTarget.Config> transformTargets;
        }
    }
}
