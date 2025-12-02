using System.Collections.Generic;
using System.Linq;

namespace Genies.Avatars
{
    /// <summary>
    /// Removes from the outfit any assets occupying slots suppressed by other assets. It uses an <see cref="OutfitSlotsData"/> instance
    /// to check what slots are suppressed by other slots. This rule is run before an outfit is loaded and equipped to the avatar, so
    /// we can safely remove the suppressed assets so we can optimize and avoid loading them, otherwise they would be suppressed by UMA
    /// which has the same outcome from the user's perspective.
    /// </summary>
    public sealed class RemoveSuppressedSlots : IAssetsValidationRule<OutfitAsset>
    {
        private readonly OutfitSlotsData _slotsData;
        
        public RemoveSuppressedSlots(OutfitSlotsData slotsData)
        {
            _slotsData = slotsData;
        }
        
        public void Apply(HashSet<OutfitAsset> outfit)
        {
            foreach (OutfitSlotsData.Slot slot in _slotsData.Slots)
            {

                if (!outfit.Any(asset => asset?.Metadata.Slot == slot.Name))
                {
                    continue;
                }

                // the outfit contains an asset on this slot so remove all suppressed slots
                foreach (string suppressedSlot in slot.SuppressedSlots)
                {
                    outfit.RemoveWhere(asset => asset.Metadata.Slot == suppressedSlot);
                }
            }
        }
    }
}