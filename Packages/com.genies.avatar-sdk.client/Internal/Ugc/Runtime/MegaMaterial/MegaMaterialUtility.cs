using Genies.Shaders;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Some static utility methods to work with <see cref="Material"/> instances using the mega shader.
    /// </summary>
    public static class MegaMaterialUtility
    {
        public static Region[] GetRegions(Material material, int count)
        {
            if (!material)
            {
                return null;
            }

            var regions = new Region[count];

            for (int i = 0; i < count; ++i)
            {
                regions[i] = GetRegion(material, i);
            }

            return regions;
        }

        public static Style[] GetStyles(Material material, int count)
        {
            if (!material)
            {
                return null;
            }

            var styles = new Style[count];

            for (int i = 0; i < count; ++i)
            {
                styles[i] = GetStyle(material, i);
            }

            return styles;
        }

        public static Region GetRegion(Material material, int regionIndex)
        {
            if (!material)
            {
                return null;
            }

            return new Region()
            {
                RegionNumber = regionIndex + 1,
                Style = GetStyle(material, regionIndex),
            };
        }

        public static Style GetStyle(Material material, int regionIndex)
        {
            if (!material)
            {
                return null;
            }

            var pattern = new Pattern
            {
                Type = material.GetFloat(MegaShaderRegionProperty.PatternDuotone, regionIndex) == 0.0f ? PatternType.Textured : PatternType.Duotone,
                TextureId = material.GetTexture(MegaShaderRegionProperty.PatternTexture, regionIndex)?.name,
                Rotation = material.GetFloat(MegaShaderRegionProperty.PatternRotation, regionIndex),
                Scale = material.GetFloat(MegaShaderRegionProperty.PatternScale, regionIndex),
                Hue = material.GetFloat(MegaShaderRegionProperty.PatternHue, regionIndex),
                Saturation = material.GetFloat(MegaShaderRegionProperty.PatternSaturation, regionIndex),
                Gain = material.GetFloat(MegaShaderRegionProperty.PatternGain, regionIndex),
                DuoContrast = material.GetFloat(MegaShaderRegionProperty.PatternDuoContrast, regionIndex),
                DuoColor1 = material.GetColor(MegaShaderRegionProperty.PatternDuoColor1, regionIndex),
                DuoColor2 = material.GetColor(MegaShaderRegionProperty.PatternDuoColor2, regionIndex)
            };

            var style = new Style
            {
                Pattern = pattern,
                Color = material.GetColor(MegaShaderRegionProperty.Color, regionIndex),
                SurfaceTextureId = material.GetTexture(MegaShaderRegionProperty.Material, regionIndex)?.name,
                SurfaceScale = material.GetFloat(MegaShaderRegionProperty.MaterialScale, regionIndex)
            };

            return style;
        }
    }
}
