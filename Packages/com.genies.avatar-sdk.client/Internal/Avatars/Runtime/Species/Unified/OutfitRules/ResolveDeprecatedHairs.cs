using System.Collections.Generic;

namespace Genies.Avatars
{
    public class ResolveDeprecatedHairs : IAssetsValidationRule<OutfitAsset>
    {
        private HashSet<string> deprecatedHairs = new HashSet<string>()
        {
            "hair-0005-fade",
            "hair-0008-afro",
            "hair-0010-jb",
            "hair-0013-short",
            "hair-0014-longBraids",
            "hair-0019-pinStraight",
            "hair-0021-curledPony",
            "hair-0024-combOver",
            "hair-0029-bubblePony",
            "hair-0034-roundPuffPony",
            "hair-0036-wavyPony",
            "hair-0060-curlyLongFade",
            "hair-0072-cornrowBun",
            "hair-0073-sideSweepBraidBob",
            "hair-0117-tapperFade",
            "hair-0105-middleMushroomPart",
        };

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            if (!HasHair(outfit, out OutfitAsset hair))
            {
                return;
            }

            if (IsHairDeprecated(hair.Id))
            {
                outfit.Remove(hair);
            }
        }

        private bool IsHairDeprecated(string id)
        {
            var underscoreIndex = id.IndexOf('_');
            if (underscoreIndex != -1)
            {
                var withoutPrefix = id.Substring(0, underscoreIndex);
                return deprecatedHairs.Contains(withoutPrefix);
            }
            return false;
        }

        private static bool HasHair(HashSet<OutfitAsset> outfit, out OutfitAsset hair)
        {
            hair = default;

            bool hasHair = false;

            foreach (OutfitAsset asset in outfit)
            {
                if (asset.Metadata.Slot == UnifiedOutfitSlot.Hair)
                {
                    hair = asset;
                    hasHair = true;
                }

                if (hasHair)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
