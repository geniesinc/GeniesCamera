using System.Collections.Generic;
using Genies.Components.ShaderlessTools;
using UMA;
using UnityEngine;
//TODO DEPRECATE
namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "UgcTemplate", menuName = "Genies/UGCW/UgcTemplate", order = 0)]
#endif
    public class UgcTemplate : ScriptableObject , IShaderlessAsset
    {
        [SerializeField] private string assetId;
        [SerializeField] private string slot;
        [SerializeField] private string subcategory;
        [SerializeField] private Texture2D icon;
        [SerializeField] private string[] tags;

        [SerializeField] private bool isLockAvailable;
        [SerializeField] private List<int> elementsPerSplit;
        [SerializeField] private List<MeshHideAsset> meshHideAssets;
        [SerializeField] private CollisionData collisionData;
        [SerializeField]private ShaderlessMaterials shaderlessMaterials;
        public List<Object> extras = new();

        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }

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

        public Texture2D Icon
        {
            get => icon;
            set => icon = value;
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

        public List<int> ElementsPerSplit
        {
            get => elementsPerSplit;
            set => elementsPerSplit = value;
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
            return isLockAvailable && elementsPerSplit.Count == 1;
        }
    }
}
