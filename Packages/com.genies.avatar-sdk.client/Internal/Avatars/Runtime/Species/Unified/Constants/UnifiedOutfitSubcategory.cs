using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the outfit subcategories available for the unified species. Subcategories are more specific than slots. Slots
    /// are the actual technical slot in which an asset gets added to the UMA avatar, but subcategories contain more fine grained details.
    /// For example: assets from the pants, shorts and skirt all use the legs slot.
    /// </summary>
    public static class UnifiedOutfitSubcategory
    {
        public const string Hair = "hair";
        public const string Eyebrows = "eyebrows";
        public const string Eyelashes = "eyelashes";
        public const string FacialHair = "facialHair";
        public const string UnderwearTop = "underwearTop";
        public const string Hoodie = "hoodie";
        public const string Shirt = "shirt";
        public const string Jacket = "jacket";
        public const string Dress = "dress";
        public const string Pants = "pants";
        public const string Shorts = "shorts";
        public const string Skirt = "skirt";
        public const string UnderwearBottom = "underwearBottom";
        public const string Socks = "socks";
        public const string Shoes = "shoes";
        public const string Bag = "bag";
        public const string Bracelet = "bracelet";
        public const string Watch = "watch";
        public const string Earrings = "earrings";
        public const string Glasses = "glasses";
        public const string Hat = "hat";
        public const string Mask = "mask";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            Hair,
            Eyebrows,
            Eyelashes,
            FacialHair,
            UnderwearTop,
            Hoodie,
            Shirt,
            Jacket,
            Dress,
            Pants,
            Shorts,
            Skirt,
            UnderwearBottom,
            Socks,
            Shoes,
            Bag,
            Bracelet,
            Earrings,
            Glasses,
            Hat,
            Mask,
            Watch,
        }.AsReadOnly();

        /// <summary>
        /// Tries to get the unified outfit slot that corresponds to the given subcategory.
        /// </summary>
        public static bool TryGetSlot(string subcategory, out string slot)
        {
            return _slotsBySubcategory.TryGetValue(subcategory, out slot);
        }

        // maps all subcategories to their corresponding slots
        private static readonly Dictionary<string, string> _slotsBySubcategory = new Dictionary<string, string>
        {
            { Hair, UnifiedOutfitSlot.Hair },

            { Eyebrows, UnifiedOutfitSlot.Eyebrows },

            { Eyelashes, UnifiedOutfitSlot.Eyelashes },

            { FacialHair, UnifiedOutfitSlot.FacialHair },

            { UnderwearTop, UnifiedOutfitSlot.UnderwearTop },

            { Hoodie, UnifiedOutfitSlot.Hoodie },

            { Shirt, UnifiedOutfitSlot.Shirt },

            { Jacket, UnifiedOutfitSlot.Jacket },

            { Dress, UnifiedOutfitSlot.Dress },

            { Pants, UnifiedOutfitSlot.Legs },
            { Shorts, UnifiedOutfitSlot.Legs },
            { Skirt, UnifiedOutfitSlot.Legs },

            { UnderwearBottom, UnifiedOutfitSlot.UnderwearBottom },

            { Socks, UnifiedOutfitSlot.Socks },

            { Shoes, UnifiedOutfitSlot.Shoes },

            { Bag, UnifiedOutfitSlot.Bag },

            { Bracelet, UnifiedOutfitSlot.Wrist },
            { Watch, UnifiedOutfitSlot.Wrist },

            { Earrings, UnifiedOutfitSlot.Earrings },

            { Glasses, UnifiedOutfitSlot.Glasses },

            { Hat, UnifiedOutfitSlot.Hat },

            { Mask, UnifiedOutfitSlot.Mask },
        };
    }
}
