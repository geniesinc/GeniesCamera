using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public class RemoveDnaForGAP : IAssetsValidationRule<OutfitAsset>
    {
        private static readonly HashSet<string> dnaSlots = new HashSet<string>
        {
            UnifiedOutfitSlot.Hair,
            UnifiedOutfitSlot.Eyebrows,
            UnifiedOutfitSlot.Eyelashes,
            UnifiedOutfitSlot.FacialHair,
        };
        
        public void Apply(HashSet<OutfitAsset> outfit)
        {
            // Go through applicable DNA slots
            foreach (var slot in dnaSlots)
            {
                // Remove asset if on avatar
                if (HasSlot(outfit, slot, out OutfitAsset asset))
                {
                    outfit.Remove(asset);
                }
            }
        }
        
        private static bool HasSlot(HashSet<OutfitAsset> outfit, string slot, out OutfitAsset outAsset)
        {
            outAsset = default;

            bool hasSlot = false;

            foreach (OutfitAsset asset in outfit)
            {
                if (asset.Metadata.Slot == slot)
                {
                    outAsset = asset;
                    hasSlot = true;
                }

                if (hasSlot)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
