using UnityEngine;

//TODO DEPRECATE
namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for things.
    /// It holds are the data that the thing will use in the addressable bundles
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ThingsTemplate", menuName = "Genies/Things/ThingsTemplate", order = 0)]
#endif
    public class ThingsTemplate : OrderedScriptableObject
    {
        [SerializeField] private string assetId;
        [SerializeField] private string[] tags;
        [SerializeField] private bool isLockAvailable;
        [SerializeField] private UgcSplit[] splits;

        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public string[] Tags
        {
            get => tags;
            set => tags = value;
        }

        public bool IsLockAvailable
        {
            get => isLockAvailable;
            set => isLockAvailable = value;
        }

        public UgcSplit[] Splits
        {
            get => splits;
            set => splits = value;
        }

        public bool IsBasic()
        {
            return isLockAvailable && splits.Length == 1;
        }
    }
}
