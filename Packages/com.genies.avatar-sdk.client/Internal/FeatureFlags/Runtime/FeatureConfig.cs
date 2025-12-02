using System;
using Toolbox.Core;
using UnityEngine;

namespace Genies.FeatureFlags
{
    [Serializable]
    public class FeatureData
    {
        public SerializableFeatureFlag featureFlag;
        public bool enabled;

#if UNITY_EDITOR
        public bool shouldOverrideRemote;
#endif
    }

#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FeatureConfig", menuName = "Genies/FeatureConfig")]
#endif
    public class FeatureConfig : ScriptableObject
    {
        [LabelByChild("featureFlag.featureFlagKey"), ReorderableList(HasLabels = true, Foldable = false)]
        public FeatureData[] features;
    }
}
