using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Genies.Utilities
{
    /// <summary>
    /// Helper struct that can be used to initialize a <see cref="UnityEngine.RenderTexture"/> based on an
    /// <see cref="TextureSettings"/> asset, optimized for performing rendering operations on it and then calling the
    /// <see cref="FinishRendering"/> method to produce the final texture result based on the used settings.
    /// </summary>
    public struct TextureSettingsRenderer
    {
        public RenderTexture RenderTexture { get; private set; }
        public bool          IsRendering   => RenderTexture;
        
        // saved settings
        private readonly TextureConfig        _textureConfig;
        private readonly TextureSettings.Type _textureType;
        private readonly bool                 _nonReadable;
        private readonly bool                 _compress;
        private readonly bool                 _highQualityCompression;

        public TextureSettingsRenderer(TextureSettings settings,
            GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat depthStencilFormat = GraphicsFormat.None
        ) : this(settings.width, settings.height, settings, graphicsFormat, depthStencilFormat) { }

        public TextureSettingsRenderer(int width, int height, TextureSettings settings,
            GraphicsFormat graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
            GraphicsFormat depthStencilFormat = GraphicsFormat.None
        ) {
            // save relevant settings from the TextureSettings asset
            _textureConfig = settings.GetTextureConfig(graphicsFormat);
            _textureType = settings.textureType;
            _nonReadable = settings.nonReadable;
            _compress = settings.compress;
            _highQualityCompression = settings.highQualityCompression;
            
            // generate a config optimized for rendering (no auto mip maps generation as we will do that manually when finishing)
            TextureConfig optimizedTextureConfig = _textureConfig;
            optimizedTextureConfig.Width = width;
            optimizedTextureConfig.Height = height;
            RenderTextureDescriptor descriptor = optimizedTextureConfig.GetRenderTextureDescriptor(depthStencilFormat);
            descriptor.autoGenerateMips = false;
            
            // update texture config graphics format since the render texture descriptor will contain a system compatible format
            _textureConfig.GraphicsFormat = descriptor.graphicsFormat;
            
            switch (_textureType)
            {
                // if creating a Texture2D then we will create a temporary render texture and generate the final Texture2D later
                case TextureSettings.Type.Texture2D:
                    optimizedTextureConfig.MipMaps = false; // ensure no space is allocated for mipmaps since this render texture will be used only for creating the final Texture2D
                    RenderTexture = optimizedTextureConfig.GetTemporaryRenderTexture(descriptor);
                    break;
                
                case TextureSettings.Type.RenderTexture:
                    RenderTexture = optimizedTextureConfig.CreateRenderTexture(descriptor);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Produces the final texture result from the current <see cref="RenderTexture"/>, based on the settings used
        /// to initialize the renderer. The renderer will become unusable after this.
        /// </summary>
        public Texture FinishRendering()
        {
            if (!RenderTexture)
            {
                throw new Exception($"[{nameof(TextureSettingsRenderer)}] called {nameof(FinishRendering)}() on a non-initialized or finished renderer");
            }

            RenderTexture renderTexture = RenderTexture;
            RenderTexture = null;
            
            if (_textureType is TextureSettings.Type.RenderTexture)
            {
                // if mipmaps where specified on the settings then generate them now
                if (renderTexture.useMipMap && !renderTexture.autoGenerateMips)
                {
                    renderTexture.GenerateMips();
                }

                return renderTexture;
            }
            
            try
            {
                // convert the render texture into the final Texture2D output
                return _compress ?
                    renderTexture.ToCompressedTexture2D(_textureConfig, _highQualityCompression, _nonReadable) :
                    renderTexture.ToTexture2D(_textureConfig, _nonReadable);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}
