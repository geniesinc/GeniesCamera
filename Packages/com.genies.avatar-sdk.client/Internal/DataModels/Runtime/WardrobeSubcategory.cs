using System;

namespace Genies.Models {
    public enum WardrobeSubcategory {
        none,
        hair,
        eyebrows,
        eyelashes,
        facialHair,
        underwearTop,
        hoodie,
        shirt,
        jacket,
        dress,
        pants,
        shorts,
        skirt,
        underwearBottom,
        socks,
        shoes,
        bag,
        bracelet,
        earrings,
        glasses,
        hat,
        mask,
        watch,
        all
    }

    public static class WardrobeSubcategoryExtensions {
        public static WardrobeSubcategory FromString(string wardrobeSubcategoryName) {
            if (Enum.TryParse(wardrobeSubcategoryName, true, out WardrobeSubcategory wardrobeSubcategory))
            {
                return wardrobeSubcategory;
            }

            return WardrobeSubcategory.none;
        }

        public static WardrobeSubcategory FromAssetName(string assetName) {
            foreach (var subcategory in (WardrobeSubcategory[]) Enum.GetValues(typeof(WardrobeSubcategory))) {
                if (assetName.ToLower().StartsWith(subcategory.ToString().ToLower()))
                {
                    return subcategory;
                }
            }

            return WardrobeSubcategory.none;
        }
    }
}
