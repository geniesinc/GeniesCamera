using System.Collections.Generic;
using UnityEngine;

namespace Genies.FeatureFlags
{
    /// <summary>
    /// A collection of sensitive data info of the feature flags
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FeatureFlagsDataInfo", menuName = "GeniesParty/Feature Flags/FeatureFlagsDataInfo")]
#endif
    public class FeatureFlagsDataInfo : ScriptableObject
    {
        [SerializeField] private List<string> _data;
        public List<string> Data => _data;
    }
}
