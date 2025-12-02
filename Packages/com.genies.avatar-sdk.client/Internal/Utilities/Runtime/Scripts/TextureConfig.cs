using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    /// <summary>
    /// Basic configuration for <see cref="Texture"/> instances. It contains utility methods to create different
    /// types of textures from it. Its also used in the extension methods from <see cref="TextureExtensions"/>.
    /// </summary>
    public struct TextureConfig
    {
        public int Width;
        public int Height;
        public GraphicsFormat GraphicsFormat;
        public TextureDimension Dimension;
        public TextureWrapMode WrapMode;
        public TextureWrapMode WrapModeU;
        public TextureWrapMode WrapModeV;
        public TextureWrapMode WrapModeW;
        public FilterMode FilterMode;
        public int AnisoLevel;
        public bool MipMaps;
        public float MipMapBias;

        public static TextureConfig Default => new()
        {
            Width = 1024,
            Height = 1024,
            GraphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            Dimension = TextureDimension.Tex2D,
            WrapMode = TextureWrapMode.Repeat,
            WrapModeU = TextureWrapMode.Repeat,
            WrapModeV = TextureWrapMode.Repeat,
            WrapModeW = TextureWrapMode.Repeat,
            FilterMode = FilterMode.Bilinear,
            AnisoLevel = 1,
            MipMaps = false,
            MipMapBias = 0.0f,
        };

        public void ApplyTo(Texture texture)
        {
            texture.wrapMode = WrapMode;
            texture.wrapModeU = WrapModeU;
            texture.wrapModeV = WrapModeV;
            texture.wrapModeW = WrapModeW;
            texture.filterMode = FilterMode;
            texture.anisoLevel = AnisoLevel;
            texture.mipMapBias = MipMapBias;
        }

        public RenderTextureDescriptor ApplyTo(RenderTextureDescriptor descriptor)
        {
            descriptor.width = Width;
            descriptor.height = Height;
#if UNITY_2023_1_OR_NEWER
            descriptor.graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat, GraphicsFormatUsage.Render);
#else
            descriptor.graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat, FormatUsage.Render);
#endif
            descriptor.useMipMap = MipMaps;
            descriptor.dimension = Dimension;

            return descriptor;
        }

        public RenderTextureDescriptor GetRenderTextureDescriptor(GraphicsFormat depthStencilFormat = GraphicsFormat.None)
        {
#if UNITY_2023_1_OR_NEWER
            GraphicsFormat graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat, GraphicsFormatUsage.Render);
            depthStencilFormat = SystemInfo.GetCompatibleFormat(depthStencilFormat, GraphicsFormatUsage.StencilSampling);
#else
            GraphicsFormat graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat, FormatUsage.Render);
            depthStencilFormat = SystemInfo.GetCompatibleFormat(depthStencilFormat, FormatUsage.StencilSampling);
#endif
            
            /**
             * Its very important to use this constructor so RenderTextureCreationFlags.AllowVerticalFlip is set since descriptor
             * flags are readonly. Otherwise some graphic devices will be randomly flipping our render textures when blitting or
             * doing Texture2D.ReadPixels
             */
            return new RenderTextureDescriptor(Width, Height, graphicsFormat, depthStencilFormat)
            {
                useMipMap = MipMaps,
                autoGenerateMips = true,
                dimension = Dimension,
            };
        }

        public RenderTexture CreateRenderTexture(GraphicsFormat depthStencilFormat = GraphicsFormat.None)
        {
            var renderTexture = new RenderTexture(GetRenderTextureDescriptor(depthStencilFormat));
            ApplyTo(renderTexture);

            return renderTexture;
        }

        public RenderTexture CreateRenderTexture(RenderTextureDescriptor descriptor)
        {
            var renderTexture = new RenderTexture(ApplyTo(descriptor));
            ApplyTo(renderTexture);

            return renderTexture;
        }

        public RenderTexture GetTemporaryRenderTexture(GraphicsFormat depthStencilFormat = GraphicsFormat.None)
        {
            var renderTexture = RenderTexture.GetTemporary(GetRenderTextureDescriptor(depthStencilFormat));
            ApplyTo(renderTexture);

            return renderTexture;
        }

        public RenderTexture GetTemporaryRenderTexture(RenderTextureDescriptor descriptor)
        {
            var renderTexture = RenderTexture.GetTemporary(ApplyTo(descriptor));
            ApplyTo(renderTexture);

            return renderTexture;
        }

        public Texture2D CreateTexture2D()
        {
            // get a system compatible graphics format for creating sample textures
#if UNITY_2023_1_OR_NEWER
            GraphicsFormat graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat, GraphicsFormatUsage.Sample);
#else
            GraphicsFormat graphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat, FormatUsage.Sample);
#endif
            var textureCreationFlags = MipMaps ? TextureCreationFlags.MipChain : TextureCreationFlags.None;

            // create the texture instance and apply the ration
            var texture = new Texture2D(Width, Height, graphicsFormat, textureCreationFlags);
            ApplyTo(texture);

            return texture;
        }

        public override string ToString()
        {
            return $"TextureConfig:\n{Width}x{Height}\n{GraphicsFormat}\nMipMaps: {MipMaps}";
        }
    }
}
