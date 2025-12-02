using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Ensures that there is no assets from slots not included in the available slots.
    /// You can use the <see cref="SetAvailableSlots"/> method to update the currently
    /// available slots.
    /// </summary>
    public sealed class EnsureAllAssetsAreFromAvailableSlots : IAssetsValidationRule<OutfitAsset>
    {
        // state
        private readonly HashSet<string> _availableSlots;

        public EnsureAllAssetsAreFromAvailableSlots(IEnumerable<string> availableSlots = null)
        {
            _availableSlots = new HashSet<string>();
            SetAvailableSlots(availableSlots);
        }
        
        public void Apply(HashSet<OutfitAsset> outfit)
        {
            if (_availableSlots.Count == 0)
            {
                return;
            }

            outfit.RemoveWhere(asset => !_availableSlots.Contains(asset.Metadata.Slot));
        }

        public void SetAvailableSlots(IEnumerable<string> availableSlots)
        {
            if (availableSlots is null)
            {
                return;
            }

            _availableSlots.Clear();
            _availableSlots.UnionWith(availableSlots);
        }
    }
}