using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Removes from the outfit any assets occupying slots that suppresses the asset being adjusted.
    /// It uses an <see cref="OutfitSlotsData"/> instance to check what slots are suppressed by other slots.
    /// </summary>
    public sealed class RemoveSuppressingSlots : IAssetsAdjustmentRule<OutfitAsset>
    {
        private readonly OutfitSlotsData _slotsData;

        public RemoveSuppressingSlots(OutfitSlotsData slotsData)
        {
            _slotsData = slotsData;
        }

        public void Apply(HashSet<OutfitAsset> outfit, OutfitAsset adjustedAsset)
        {
            foreach (OutfitSlotsData.Slot slot in _slotsData.Slots)
            {
                // if the current adjusted asset's slot is in the suppressed slots then remove any assets from this slot
                if (slot.SuppressedSlots.Contains(adjustedAsset.Metadata.Slot))
                {
                    outfit.RemoveWhere(asset => asset?.Metadata.Slot == slot.Name);
                }
            }
        }
    }
}
