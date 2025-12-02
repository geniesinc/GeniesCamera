using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Ensures that theres is only one blend shape equipped per slot.
    /// </summary>
    public sealed class EnsureOnlyOneBlendShapePerSlot : IAssetsValidationRule<BlendShapeAsset>
    {
        // helpers
        private readonly List<BlendShapeAsset> _equippedAssets;
        private readonly HashSet<string> _equippedSlots;

        public EnsureOnlyOneBlendShapePerSlot()
        {
            _equippedAssets = new List<BlendShapeAsset>();
            _equippedSlots = new HashSet<string>();
        }

        public void Apply(HashSet<BlendShapeAsset> assets)
        {
            _equippedAssets.AddRange(assets);

            foreach (BlendShapeAsset asset in _equippedAssets)
            {
                // if the slot is already occupied by other blend shape, then remove this blend shape from the assets
                if (_equippedSlots.Contains(asset.Slot))
                {
                    assets.Remove(asset);
                }

                _equippedSlots.Add(asset.Slot);
            }
            
            _equippedAssets.Clear();
            _equippedSlots.Clear();
        }
    }
}