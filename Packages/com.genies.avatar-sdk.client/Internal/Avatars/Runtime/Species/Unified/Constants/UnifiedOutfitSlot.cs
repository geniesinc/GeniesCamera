using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the outfit slots available for the unified species. Slots are less specific than subcategories. Slots
    /// are the actual technical slot in which an asset gets added to the UMA avatar. For example: assets using the legs
    /// slot could be pants, shorts or skirts subcategories.
    /// </summary>
    public static class UnifiedOutfitSlot
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
        public const string Legs = "legs";
        public const string UnderwearBottom = "underwearBottom";
        public const string Socks = "socks";
        public const string Shoes = "shoes";
        public const string Bag = "bag";
        public const string Wrist = "wrist";
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
            Legs,
            UnderwearBottom,
            Socks,
            Shoes,
            Bag,
            Wrist,
            Earrings,
            Glasses,
            Hat,
            Mask,
        }.AsReadOnly();

        /// <summary>
        /// Whether or not the given slot is considered a top slot or not.
        /// </summary>
        public static bool IsTop(string slot)
        {
            return slot == Shirt || slot == Hoodie || slot == Jacket;
        }
    }
}
