using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Material generator utility for the <see cref="WearableRandomizer"/>.
    /// </summary>
    internal static class CuratedMaterialGenerator
    {
        public static RegionTextures GetRandomMaterials(int regionCount, WearableRandomizerData data)
        {
            // default to no material (will use base material when loaded) and 5.0 material scale (it is the official default used in the MegaShader)
            var materials = new RegionTextures(null, 5.0f, null, 5.0f, null, 5.0f, null, 5.0f);

            // pick 2 random basic and 1 random accent material IDs
            string basicId1 = data.BasicMaterialIds[Random.Range(0, data.BasicMaterialIds.Count)];
            string basicId2 = data.BasicMaterialIds[Random.Range(0, data.BasicMaterialIds.Count)];
            string accentId = data.AccentMaterialIds[Random.Range(0, data.AccentMaterialIds.Count)];

            // special case, choose randomly from all three textures
            if (regionCount == 1)
            {
                switch (Random.Range(0, 3))
                {
                    case 0:
                        materials.SetId(0, basicId1);
                        materials.SetScale(0, Random.Range(3.0f, 8.0f));
                        break;

                    case 1:
                        materials.SetId(0, basicId2);
                        materials.SetScale(0, Random.Range(3.0f, 8.0f));
                        break;

                    case 3:
                        materials.SetId(0, accentId);
                        materials.SetScale(0, Random.Range(2.5f, 6.0f));
                        break;
                }

                return materials;
            }

            // decide whether to use the accent texture in one region only (85% chance) or in multiple regions (15% chance)
            bool useAccentOnce = Random.value < 0.85f;
            int accentRegionIndex = Random.Range(0, regionCount); // choose a random region to use the accent texture

            for (int i = 0; i < regionCount; ++i)
            {
                // use accent texture in the chosen region, or randomly if useAccentOnce is false
                if ((useAccentOnce && i == accentRegionIndex) || (!useAccentOnce && Random.value < 0.20f))
                {
                    materials.SetId(i, accentId);
                    materials.SetScale(i, Random.Range(2.5f, 6.0f));
                }
                else
                {
                    // if not accent then choose a random basic texture
                    materials.SetId(i, Random.value < 0.5f ? basicId1 : basicId2);
                    materials.SetScale(i, Random.Range(3.0f, 8.0f));
                }
            }

            return materials;
        }
    }
}
