using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    /// <summary>
    /// Texture settings used by <see cref="MaterialMapExporter"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TextureSettings", menuName = "Genies/Material Baking/Texture Settings")]
#endif
    public sealed class TextureSettings : ScriptableObject
    {
        [Header("Settings")][Space(8)]
        public Type            textureType = Type.Texture2D;
        public int             width       = 2048;
        public int             height      = 2048;
        public TextureWrapMode wrapMode    = TextureWrapMode.Repeat;
        public FilterMode      filterMode  = FilterMode.Bilinear;
        public int             anisoLevel  = 1;
        public bool            mipMaps     = true;
        public float           mipMapBias  = 0.0f;

        [Header("Texture2D Settings")][Space(8)]
        public bool nonReadable            = true;
        public bool compress               = true;
        public bool highQualityCompression = true;

        public TextureConfig GetTextureConfig(GraphicsFormat graphicsFormat)
        {
            return new TextureConfig
            {
                Width          = width,
                Height         = height,
                GraphicsFormat = graphicsFormat,
                Dimension      = TextureDimension.Tex2D,
                WrapMode       = wrapMode,
                WrapModeU      = wrapMode,
                WrapModeV      = wrapMode,
                WrapModeW      = wrapMode,
                FilterMode     = filterMode,
                AnisoLevel     = anisoLevel,
                MipMaps        = mipMaps,
                MipMapBias     = mipMapBias,
            };
        }

        public void CopyFrom(TextureSettings source)
        {
            textureType            = source.textureType;
            width                  = source.width;
            height                 = source.height;
            wrapMode               = source.wrapMode;
            filterMode             = source.filterMode;
            anisoLevel             = source.anisoLevel;
            mipMaps                = source.mipMaps;
            mipMapBias             = source.mipMapBias;
            nonReadable            = source.nonReadable;
            compress               = source.compress;
            highQualityCompression = source.highQualityCompression;
        }

        public TextureSettingsRenderer BeginRendering(
            Vector2Int size,
            GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat depthStencilFormat = GraphicsFormat.None
        ) {
            return new TextureSettingsRenderer(size.x, size.y, this, graphicsFormat, depthStencilFormat);
        }

        public TextureSettingsRenderer BeginRendering(
            int width, int height,
            GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat depthStencilFormat = GraphicsFormat.None
        ) {
            return new TextureSettingsRenderer(width, height, this, graphicsFormat, depthStencilFormat);
        }

        public TextureSettingsRenderer BeginRendering(
            GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat depthStencilFormat = GraphicsFormat.None
        ) {
            return new TextureSettingsRenderer(this, graphicsFormat, depthStencilFormat);
        }

        public int GetSettingsHashCode()
        {
            return (
                textureType,
                width,
                height,
                wrapMode,
                filterMode,
                anisoLevel,
                mipMaps,
                mipMapBias,
                nonReadable,
                compress,
                highQualityCompression
            ).GetHashCode();
        }

        public enum Type
        {
            Texture2D = 0,
            RenderTexture = 1,
        }
    }
}
