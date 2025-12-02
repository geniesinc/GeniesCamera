using System.Collections.Generic;

namespace Genies.Avatars
{
    public static class UnifiedOutfitSlotsData
    {
        /// <summary>
        /// Outfit slot data for the unified species.
        /// </summary>
        public static readonly OutfitSlotsData Instance = new OutfitSlotsData
        (
            // SLOTS
            new[]
            {
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Hair,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Eyebrows,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Eyelashes,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.FacialHair,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.UnderwearTop,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Hoodie,
                    IncompatibleSlots = new HashSet<string> { UnifiedOutfitSlot.Dress },
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Shirt,
                    IncompatibleSlots = new HashSet<string> { UnifiedOutfitSlot.Dress },
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Jacket,
                    IncompatibleSlots = new HashSet<string> { UnifiedOutfitSlot.Dress },
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Dress,
                    IncompatibleSlots = new HashSet<string>
                    {
                        UnifiedOutfitSlot.Shirt,
                        UnifiedOutfitSlot.Jacket,
                        UnifiedOutfitSlot.Hoodie,
                        UnifiedOutfitSlot.Legs
                    },
                    SuppressedSlots = new HashSet<string>
                    {
                        UnifiedOutfitSlot.UnderwearTop, UnifiedOutfitSlot.UnderwearBottom
                    }
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Legs,
                    IncompatibleSlots = new HashSet<string> { UnifiedOutfitSlot.Dress },
                    SuppressedSlots = new HashSet<string> { UnifiedOutfitSlot.UnderwearBottom }
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.UnderwearBottom,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Socks,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Shoes,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Bag,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Wrist,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Earrings,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Glasses,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Hat,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>()
                },
                new OutfitSlotsData.Slot
                {
                    Name = UnifiedOutfitSlot.Mask,
                    IncompatibleSlots = new HashSet<string>(),
                    SuppressedSlots = new HashSet<string>
                    {
                        UnifiedOutfitSlot.Hair,
                        UnifiedOutfitSlot.Hat,
                        UnifiedOutfitSlot.Glasses,
                        UnifiedOutfitSlot.Earrings,
                        UnifiedOutfitSlot.FacialHair
                    }
                }
            },

            // COLLISION GROUPS
            new[]
            {
                new OutfitSlotsData.CollisionGroup
                {
                    Name = "tops",
                    Slots = new HashSet<string>
                    {
                        UnifiedOutfitSlot.Shirt,
                        UnifiedOutfitSlot.Hoodie,
                        UnifiedOutfitSlot.Jacket,
                        UnifiedOutfitSlot.UnderwearTop
                    }
                }
            }
        );
    }
}
