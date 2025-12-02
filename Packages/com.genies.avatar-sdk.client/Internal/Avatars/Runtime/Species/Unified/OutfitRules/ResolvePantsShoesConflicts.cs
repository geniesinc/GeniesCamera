using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Resolves some conflicts with certain pants-shoes asset combinations. Only for the Unified species.
    /// </summary>
    public sealed class ResolvePantsShoesConflicts : IAssetsValidationRule<OutfitAsset>
    {
        private const string PantsBlendShapeName = "pants_geo_blendShape.pants_geo_over";

        // dependencies
        private readonly IEditableGenie _genie;

        public ResolvePantsShoesConflicts(IEditableGenie genie)
        {
            _genie = genie;
        }

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            float value = HasBlendingPantsAndShoes(outfit) ? 1.0f : 0.0f;
            // ensure the blend shape is baked so it has no performance cost and also it is applied right after rebuilding the genie
            _genie.SetBlendShape(PantsBlendShapeName, value, baked: true);
        }

        /// <summary>
        /// Returns true if the given outfit has both blending pants and shoes assets equipped.
        /// </summary>
        private static bool HasBlendingPantsAndShoes(HashSet<OutfitAsset> outfit)
        {
            bool hasBlendingPants = false;
            bool hasShoes = false;

            foreach (OutfitAsset asset in outfit)
            {
                /**
                 * We look for the pants subcategory because it is more specific than the legs slot. Pants, shorts and skirts all use the
                 * legs slot, but we specifically need to look only for pants here.
                 */
                hasBlendingPants |= asset.Metadata.Subcategory == UnifiedOutfitSubcategory.Pants && asset.Metadata.CollisionData.Mode is OutfitCollisionMode.Blend;
                hasShoes |= asset.Metadata.Slot == UnifiedOutfitSlot.Shoes;

                // break execution if we have found both already
                if (hasBlendingPants && hasShoes)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
