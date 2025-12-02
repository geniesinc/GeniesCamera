using System.Collections.Generic;
using UMA;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for gear templates.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "GearTemplate", menuName = "Genies/Gear/GearTemplate", order = 0)]
#endif
    public class GearTemplate : OrderedScriptableObject
    {
        [SerializeField] private string assetId;
        [SerializeField] private string slot;
        [SerializeField] private string subcategory;
        [SerializeField] private string[] tags;

        [SerializeField] private bool isLockAvailable;
        [SerializeField] private UgcSplit[] splits;
        [SerializeField] private List<MeshHideAsset> meshHideAssets;
        [SerializeField] private CollisionData collisionData;

        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public string Slot
        {
            get => slot;
            set => slot = value;
        }

        public string Subcategory
        {
            get => subcategory;
            set => subcategory = value;
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

        public List<MeshHideAsset> MeshHideAssets
        {
            get => meshHideAssets;
            set => meshHideAssets = value;
        }

        public CollisionData CollisionData
        {
            get => collisionData;
            set => collisionData = value;
        }

        public WardrobeSlot GetSlot()
        {
            return WardrobeSlotExtensions.FromString(Slot);
        }

        public bool IsBasic()
        {
            return isLockAvailable && splits.Length == 1;
        }
    }
}
