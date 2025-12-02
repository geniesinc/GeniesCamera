using System;

namespace Genies.Models {
    public enum WardrobeSlot {
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
        legs,
        underwearBottom,
        socks,
        shoes,
        bag,
        wrist,
        earrings,
        glasses,
        hat,
        mask,
        skinColor
    }

    public static class WardrobeSlotExtensions {
        public static bool IsTop(this WardrobeSlot slot) {
            return slot == WardrobeSlot.shirt ||
                   slot == WardrobeSlot.hoodie ||
                   slot == WardrobeSlot.jacket;
        }

        public static bool IsTop(string slotName) {
            var slot = WardrobeSlotExtensions.FromString(slotName);
            return slot.IsTop();
        }

        public static WardrobeSlot FromString(string slotName) {
            if (Enum.TryParse(slotName, out WardrobeSlot slot))
            {
                return slot;
            }

            return WardrobeSlot.none;
        }
    }


}
