using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;


namespace Genies.Models {
    public enum UtilMeshName {
        bodysuit,
        dress,
        outerwear,
        pants,
        scalp,
        shirt,
        shoes,
        skirt,
        none
    }
    public enum Region {
        wholeTarget,
        biceps,
        calves,
        chest,
        forearms,
        hands,
        head,
        hips,
        neck,
        shoulders,
        thighs,
        waist
    }
    [System.Serializable]
    public class UtilMeshRegion {
        public Region region;
        public Vector3[] uniquePoints;
    }

    [System.Serializable]
    public class UtilMesh {
        public UtilMeshName utilityMesh;
        public List<UtilMeshRegion> uMeshRegions;

    }

#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Editor Utilities/Races/Create Utility Vector Container", fileName = "UtilityVectorContainer.asset")]
#endif
    public class UtilityVectorContainer : ScriptableObject {
        public string vectorName;
        public string version = null;
        public List<UtilMesh> utilMeshes;
        public static Dictionary<string, UtilMeshName> UtilMeshFromAssetCategory =>
            new Dictionary<string, UtilMeshName>{
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

        #region Helper Functions
        public static void GetUniquePoints(Mesh src, out List<Vector3> uniquePts, out List<int> uniqueIndices) {
            Vector3[] srcVerts = src.vertices;

            // For each point in the iterator, creates a tuple of values and indices. Groups those tuples by distinct value, then returns the first tuple from each group.
            var distinct = srcVerts.Select((n, i) => new { Value = n, Index = i }).GroupBy(x => x.Value).Select(grp => grp.FirstOrDefault());
            // Makes list of values from (value, index) tuple
            uniquePts = distinct.Select(x => x.Value).ToList();
            // Makes list of indices from (value, index) tuple
            uniqueIndices = distinct.Select(x => x.Index).ToList();
        }

        public static Vector3[] GetUtilityVectorPoints(UMAWardrobeRecipe recipe, UtilityVectorContainer vectorContainer, Region reg = Region.wholeTarget) {
            // Get util mesh mapping for asset
            UtilMeshName uMeshName = GetUtilityMeshFromUmaRecipe(recipe);
            if (uMeshName == UtilMeshName.none) {
                Debug.LogWarning("Asset is not refittable. Aborting.");
                return null;
            }
            // Get matching util mesh from vector container
            UtilMesh uMesh = vectorContainer.utilMeshes.FirstOrDefault(x => x.utilityMesh == uMeshName);
            // Get selected region
            UtilMeshRegion uRegion = uMesh.uMeshRegions.FirstOrDefault(x => x.region == reg);
            // Return unique deformed points (or source points if vector is "source")
            return uRegion.uniquePoints;
        }

        public static UtilMeshName GetUtilityMeshFromUmaRecipe(UMATextRecipe recipe) {
            //var wardrobeSlot = recipe.wardrobeSlot;
            // Currently our 'wardrobe slot' categories differ from our internal art team asset categories, mainly in that "pants", "shorts", and "skirt" are all encompassed by "legs"
            // The utility meshes were originally designed by the art team categories. Luckily, we can still parse them from the slot naming.
            var wardrobeSlot = recipe.name.Split('_')[1].Split('-')[0];
            return GetUtilityMeshFromAssetCategory(wardrobeSlot);
        }

        public static UtilMeshName GetUtilityMeshFromUmaRecipe(UMAWardrobeRecipe recipe)
        {
            //var wardrobeSlot = recipe.wardrobeSlot;
            // Currently our 'wardrobe slot' categories differ from our internal art team asset categories, mainly in that "pants", "shorts", and "skirt" are all encompassed by "legs"
            // The utility meshes were originally designed by the art team categories. Luckily, we can still parse them from the slot naming.
            Debug.Log("Recipe name: " + recipe.name);
            string[] nameSplits = recipe.name.Split('-');
            var wardrobeSlot = nameSplits[0];
            Debug.Log($"Wardrobe slot: {wardrobeSlot}");
            return GetUtilityMeshFromAssetCategory(wardrobeSlot);
        }

        public static UtilMeshName GetUtilityMeshFromAssetCategory(string assetCategory) {
            if (UtilMeshFromAssetCategory.ContainsKey(assetCategory)) {
                return UtilMeshFromAssetCategory[assetCategory];
            } else {
                Debug.LogWarning("No utility mesh mapping found for category " + assetCategory);
                return UtilMeshName.none;
            }
        }
        #endregion
    }
}
