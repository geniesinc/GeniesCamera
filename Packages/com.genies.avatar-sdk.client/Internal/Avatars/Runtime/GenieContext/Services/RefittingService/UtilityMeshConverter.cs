using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    // TODO we need to find a better way to get UtilMeshName from an asset, this class is currently specific to Unified only
    public static class UtilityMeshConverter
    {
        public static UtilMeshName GetUtilityMeshFromAssetCategory(OutfitAsset asset)
        {
            if (asset.Metadata.Subcategory != null && UtilMeshFromAssetCategory.TryGetValue(asset.Metadata.Subcategory, out UtilMeshName utilMeshName))
            {
                return utilMeshName;
            }

            Debug.LogWarning($"[{nameof(UtilityMeshConverter)}] no utility mesh mapping found for category " + asset.Metadata.Subcategory);
            return UtilMeshName.none;
        }
        
        public static readonly IReadOnlyDictionary<string, UtilMeshName> UtilMeshFromAssetCategory = new Dictionary<string, UtilMeshName>
        {
            { "eyebrows", UtilMeshName.none },
            { "eyes", UtilMeshName.none },
            { "shoes", UtilMeshName.shoes },
            { "shorts", UtilMeshName.pants },
            { "mask", UtilMeshName.none },
            { "glasses", UtilMeshName.none },
            { "watch", UtilMeshName.shirt },
            { "underwear", UtilMeshName.bodysuit },
            { "underwearTop", UtilMeshName.shirt },
            { "underwearBottom", UtilMeshName.pants },
            { "genericGenie", UtilMeshName.none },
            { "hairAccessory", UtilMeshName.scalp },
            { "eyeColor", UtilMeshName.none },
            { "lips", UtilMeshName.none },
            { "customGenie", UtilMeshName.none },
            { "hoodie", UtilMeshName.outerwear },
            { "faceMask", UtilMeshName.none },
            { "skirt", UtilMeshName.skirt },
            { "dress", UtilMeshName.bodysuit },
            { "ring", UtilMeshName.none },
            { "hair", UtilMeshName.scalp },
            { "eyelashes", UtilMeshName.none },
            { "shirt", UtilMeshName.shirt },
            { "jaw", UtilMeshName.none },
            { "bracelet", UtilMeshName.shirt },
            { "facialHair", UtilMeshName.none },
            { "jacket", UtilMeshName.outerwear },
            { "bag", UtilMeshName.none },
            { "gloves", UtilMeshName.none },
            { "nose", UtilMeshName.none },
            { "teeth", UtilMeshName.none },
            { "necklace", UtilMeshName.shirt },
            { "earrings", UtilMeshName.none },
            { "pants", UtilMeshName.pants },
            { "hat", UtilMeshName.none },
            { "ears", UtilMeshName.none }
        };
    }
}