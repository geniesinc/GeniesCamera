using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    public static class TextureExtensions
    {
        /// <summary>
        /// Gets the <see cref="TextureConfig"/> for the given <see cref="Texture"/> instance that can be used to create textures with the same configuration.
        /// </summary>
        public static TextureConfig GetConfig(this Texture texture)
        {
            if (texture is null)
            {
                return default;
            }

            return new TextureConfig
            {
                Width = texture.width,
                Height = texture.height,
                GraphicsFormat = texture.graphicsFormat,
                Dimension = texture.dimension,
                WrapMode = texture.wrapMode,
                WrapModeU = texture.wrapModeU,
                WrapModeV = texture.wrapModeV,
                WrapModeW = texture.wrapModeW,
                FilterMode = texture.filterMode,
                AnisoLevel = texture.anisoLevel,
                MipMaps = texture.mipmapCount > 1,
                MipMapBias = texture.mipMapBias,
            };
        }

#region RenderTexture extensions
        public static RenderTexture CreateEmpty(this RenderTexture renderTexture, bool forceNoMips = false)
        {
            return CreateEmpty(renderTexture, renderTexture.GetConfig(), forceNoMips);
        }

        public static RenderTexture CreateEmpty(this RenderTexture renderTexture, TextureConfig config, bool forceNoMips = false)
        {
            return CreateEmpty(renderTexture, config, temporary: false, forceNoMips);
        }
        
        public static RenderTexture CreateTemporaryEmpty(this RenderTexture renderTexture, bool forceNoMips = false)
        {
            return CreateTemporaryEmpty(renderTexture, renderTexture.GetConfig(), forceNoMips);
        }

        public static RenderTexture CreateTemporaryEmpty(this RenderTexture renderTexture, TextureConfig config, bool forceNoMips = false)
        {
            return CreateEmpty(renderTexture, config, temporary: true, forceNoMips);
        }
        
        public static RenderTexture CreateCopy(this RenderTexture renderTexture, bool forceNoMips = false)
        {
            return CreateCopy(renderTexture, renderTexture.GetConfig(), forceNoMips);
        }

        public static RenderTexture CreateCopy(this RenderTexture renderTexture, TextureConfig config, bool forceNoMips = false)
        {
            return CreateCopy(renderTexture, config, temporary: false, forceNoMips);
        }
        
        public static RenderTexture CreateTemporaryCopy(this RenderTexture renderTexture, bool forceNoMips = false)
        {
            return CreateTemporaryCopy(renderTexture, renderTexture.GetConfig(), forceNoMips);
        }
        
        public static RenderTexture CreateTemporaryCopy(this RenderTexture renderTexture, TextureConfig config, bool forceNoMips = false)
        {
            return CreateCopy(renderTexture, config, temporary: true, forceNoMips);
        }

        public static Texture2D ToTexture2D(this RenderTexture renderTexture, bool nonReadable = true)
        {
            return ToTexture2D(renderTexture, renderTexture.GetConfig(), compress: false, highQualityCompress: false, nonReadable);
        }

        public static Texture2D ToTexture2D(this RenderTexture renderTexture, TextureConfig config, bool nonReadable = true)
        {
            return ToTexture2D(renderTexture, config, compress: false, highQualityCompress: false, nonReadable);
        }

        public static Texture2D ToCompressedTexture2D(this RenderTexture renderTexture, bool highQualityCompress, bool nonReadable = true)
        {
            return ToTexture2D(renderTexture, renderTexture.GetConfig(), compress: true, highQualityCompress, nonReadable);
        }

        public static Texture2D ToCompressedTexture2D(this RenderTexture renderTexture, TextureConfig config, bool highQualityCompress, bool nonReadable = true)
        {
            return ToTexture2D(renderTexture, config, compress: true, highQualityCompress, nonReadable);
        }
#endregion

#region Texture2D extensions
        public static Texture2D CreateCopy(this Texture2D texture, bool nonReadable = true, bool forceNotCompressed = false)
        {
            return CreateCopy(texture, texture.GetConfig(), compress: false, highQualityCompress: false, nonReadable, forceNotCompressed);
        }

        public static Texture2D CreateCopy(this Texture2D texture, TextureConfig config, bool nonReadable = true, bool forceNotCompressed = false)
        {
            return CreateCopy(texture, config, compress: false, highQualityCompress: false, nonReadable, forceNotCompressed);
        }

        public static Texture2D CreateCompressedCopy(this Texture2D texture, bool highQualityCompress, bool nonReadable = true)
        {
            return CreateCopy(texture, texture.GetConfig(), compress: true, highQualityCompress, nonReadable, forceNotCompressed: false);
        }

        public static Texture2D CreateCompressedCopy(this Texture2D texture, TextureConfig config, bool highQualityCompress, bool nonReadable = true)
        {
            return CreateCopy(texture, config, compress: true, highQualityCompress, nonReadable, forceNotCompressed: false);
        }

        public static RenderTexture ToRenderTexture(this Texture2D texture)
        {
            return ToRenderTexture(texture, texture.GetConfig());
        }

        public static RenderTexture ToRenderTexture(this Texture2D texture, TextureConfig config)
        {
            if (!texture)
            {
                return null;
            }

            RenderTexture renderTexture = config.CreateRenderTexture();

            // always try to do GPU copying which is much faster than the other method
            if (CanGpuCopy(texture, renderTexture))
            {
                Graphics.CopyTexture(texture, renderTexture);
                return renderTexture;
            }

            Graphics.Blit(texture, renderTexture);

            // make sure mips are generated for the render texture if not done automatically
            if (renderTexture.useMipMap && !renderTexture.autoGenerateMips)
            {
                renderTexture.GenerateMips();
            }

            return renderTexture;
        }
        
        public static Texture2D ResizeTexture(this Texture2D originalTexture, int width, int height)
        {
            Texture2D newTexture = new Texture2D(width, height);
            Color[] pixels = originalTexture.GetPixels(0, 0, originalTexture.width, originalTexture.height);
            Color[] newPixels = new Color[width * height];

            float scaleX = (float)originalTexture.width / width;
            float scaleY = (float)originalTexture.height / height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float newX = x * scaleX;
                    float newY = y * scaleY;
                    newPixels[y * width + x] = originalTexture.GetPixelBilinear(newX / originalTexture.width, newY / originalTexture.height);
                }
            }

            newTexture.SetPixels(newPixels);
            newTexture.Apply();
            return newTexture;
        }
#endregion

#region PostProcessing
        public static void PostProcess(this RenderTexture renderTexture, Material material, int pass = -1)
        {
            RenderTexture tmpCopy = renderTexture.CreateTemporaryCopy(forceNoMips: true);
            Graphics.Blit(tmpCopy, renderTexture, material, pass);
            RenderTexture.ReleaseTemporary(tmpCopy);
        }
        
        public static RenderTexture PostProcess(this Texture2D texture, Material material, int pass = -1)
        {
            RenderTexture renderTexture = texture.GetConfig().CreateRenderTexture();
            Graphics.Blit(texture, renderTexture, material, pass);
            return renderTexture;
        }
        
        public static RenderTexture PostProcessIntoTemporary(this Texture2D texture, Material material, int pass = -1)
        {
            RenderTexture renderTexture = texture.GetConfig().GetTemporaryRenderTexture();
            Graphics.Blit(texture, renderTexture, material, pass);
            return renderTexture;
        }
#endregion

        #region Saving

        public enum SaveType {JPG, PNG}
        
        public static void SaveToDisk(this Texture2D tex, string name, SaveType saveType = SaveType.PNG)
        {
            //encode
            byte[] bytes;
            switch (saveType)
            {
                case SaveType.JPG:
                    bytes = tex.EncodeToJPG();
                    break;
                case SaveType.PNG:
                    bytes = tex.EncodeToPNG();
                    break;
                default:
                    bytes = tex.EncodeToPNG();
                    break;
            }
            
            //create path and ensure directory
            var extension = $".{saveType.ToString().ToLower()}";
            var path = Path.Combine(Application.persistentDataPath, name + extension);
            var directory = Path.GetDirectoryName(path);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            //write
            File.WriteAllBytes(path, bytes);
        }

        #endregion

        private static Texture2D ToTexture2D(RenderTexture renderTexture, TextureConfig config, bool compress, bool highQualityCompress, bool nonReadable)
        {
            if (!renderTexture)
            {
                return null;
            }

            // always try to do GPU copying which is much faster than the other method
            if (TryCreateTexture2DGpuCopy(renderTexture, config, compress, nonReadable, out Texture2D copy, forceNotCompressed: false))
            {
                return copy;
            }

            // copy to Texture2D by reading pixels on CPU instead
            copy = config.CreateTexture2D();
            ReadPixels(renderTexture, copy, compress, highQualityCompress, nonReadable);
            return copy;
        }

        private static void ReadPixels(RenderTexture renderTexture, Texture2D target, bool compress, bool highQualityCompress, bool nonReadable)
        {
            if (!renderTexture || !target)
            {
                return;
            }

            // ReadPixels won't work if the render texture is not the same size
            if (renderTexture.width != target.width || renderTexture.height != target.height)
            {
                // get a temporary render texture of the target size
                RenderTextureDescriptor descriptor = renderTexture.descriptor;
                descriptor.width = target.width;
                descriptor.height = target.height;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                var resizedRenderTexture = RenderTexture.GetTemporary(descriptor);

                // do a blit operation to resample the render texture into the temporary one and read pixels
                Graphics.Blit(renderTexture, resizedRenderTexture);
                ReadPixels(resizedRenderTexture, target, compress, highQualityCompress, nonReadable);

                RenderTexture.ReleaseTemporary(resizedRenderTexture);
                return;
            }

            RenderTexture previousActiveRt = RenderTexture.active;
            RenderTexture.active = renderTexture;

            target.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, recalculateMipMaps: true);
            if (compress)
            {
                target.Compress(highQualityCompress);
            }

            target.Apply(updateMipmaps: false, makeNoLongerReadable: nonReadable);

            RenderTexture.active = previousActiveRt;
        }
        
        private static RenderTexture CreateEmpty(RenderTexture renderTexture, TextureConfig config, bool temporary, bool forceNoMips)
        {
            RenderTextureDescriptor descriptor = renderTexture.descriptor;
            if (forceNoMips)
            {
                config.MipMaps = false;
                descriptor.mipCount = 1;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
            }

            return temporary ? config.GetTemporaryRenderTexture(descriptor) : config.CreateRenderTexture(descriptor);
        }
        
        private static RenderTexture CreateCopy(RenderTexture renderTexture, TextureConfig config, bool temporary, bool forceNoMips)
        {
            if (!renderTexture)
            {
                return null;
            }

            RenderTexture copy = CreateEmpty(renderTexture, config, temporary, forceNoMips);

            if (CanGpuCopy(renderTexture, copy))
            {
                Graphics.CopyTexture(renderTexture, copy);
                return copy;
            }

            Graphics.Blit(renderTexture, copy);

            // make sure mips are generated for the copy if not done automatically
            if (copy.useMipMap && !copy.autoGenerateMips)
            {
                copy.GenerateMips();
            }

            return copy;
        }

        private static Texture2D CreateCopy(Texture2D texture, TextureConfig config, bool compress, bool highQualityCompress, bool nonReadable, bool forceNotCompressed)
        {
            if (!texture)
            {
                return null;
            }

            // always try to do GPU copying which is much faster than the other method
            if (TryCreateTexture2DGpuCopy(texture, config, compress, nonReadable, out Texture2D copy, forceNotCompressed))
            {
                return copy;
            }

            config.Dimension = TextureDimension.Tex2D; // just in case the config dimension is wrong

            // get a tmp render texture (we don't need mipmaps since we are doing ReadPixels on a new Texture2D)
            RenderTextureDescriptor renderTextureDescriptor = config.GetRenderTextureDescriptor();
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.autoGenerateMips = false;
            var renderTexture = RenderTexture.GetTemporary(renderTextureDescriptor);

            // copy the source texture into the tmp rt
            RenderTexture previousRt = RenderTexture.active;
            Graphics.Blit(texture, renderTexture);

            // check if the graphics format is compatible with ReadPixels operation
#if UNITY_2023_1_OR_NEWER
            if (!SystemInfo.IsFormatSupported(config.GraphicsFormat, GraphicsFormatUsage.ReadPixels))
#else
            if (!SystemInfo.IsFormatSupported(config.GraphicsFormat, FormatUsage.ReadPixels))
#endif
            {
                config.GraphicsFormat = renderTexture.graphicsFormat; // the render texture format will never have compression
            }

            // create the final Texture2D and perform read pixels from the tmp rt
            copy = config.CreateTexture2D();
            ReadPixels(renderTexture, copy, compress, highQualityCompress, nonReadable);

            RenderTexture.active = previousRt; // not really necessary but it will avoid some warning logs
            RenderTexture.ReleaseTemporary(renderTexture);

            return copy;
        }

        /**
         * Always try to do a GPU copy when possible since it is much faster. If compress is enabled and the source texture is not compressed
         * then we can't do a GPU copy since the compression is done on CPU, so we have to ReadPixels instead and compress after.
         * If GPU copy is possible and compress is not enabled but the source texture is compressed, then the copy texture will be compressed
         * too. This makes sense since the source is compressed already and we would not gain any quality be "decompressing" it, which would
         * also be slower.
         */
        private static bool TryCreateTexture2DGpuCopy(Texture source, TextureConfig config, bool compress, bool nonReadable, out Texture2D copy, bool forceNotCompressed)
        {
            copy = null;

            // if one texture has mipmaps and the other don't then we cannot do a GPU copy
            if (source.mipmapCount > 1 != config.MipMaps)
            {
                return false;
            }

            // if we need to apply compression and the source is not already compressed then don't do a GPU copy so we can compress later
#if UNITY_2023_1_OR_NEWER
            config.GraphicsFormat = SystemInfo.GetCompatibleFormat(config.GraphicsFormat, GraphicsFormatUsage.Sample);
#else
            config.GraphicsFormat = SystemInfo.GetCompatibleFormat(config.GraphicsFormat, FormatUsage.Sample);
#endif
            bool isCompressedFormat = GraphicsFormatUtility.IsCompressedFormat(config.GraphicsFormat);
            if ((compress && !isCompressedFormat)
                || (!compress && forceNotCompressed && isCompressedFormat))
            {
                return false;
            }

            bool canGpuCopy = source switch
            {
                RenderTexture renderTexture => CanGpuCopyIntoTexture2D(renderTexture, config, nonReadable),
                Texture2D texture => CanGpuCopyIntoTexture2D(texture, config, nonReadable),
                _ => false,
            };

            if (!canGpuCopy)
            {
                return false;
            }

            copy = config.CreateTexture2D();

            // this is a very rare case that may happen if the source texture has a custom number of mipmaps rather than the autogenerated one
            if (source.mipmapCount != copy.mipmapCount)
            {
                Object.Destroy(copy);
                return false;
            }

            // don't worry, Graphics.CopyTexture will still work even if we set the texture non-readable
            // and since there is nothing to apply this doesn't affect performance
            if (nonReadable)
            {
                copy.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            }

            Graphics.CopyTexture(source, copy);

            return true;
        }

        /**
         * The following methods are based on Unity documentation. https://docs.unity3d.com/ScriptReference/Graphics.CopyTexture.html
         * The docs also mention that for some devices it is possible to copy having different formats but it does not specify how
         * to check the compatibility between two formats, so for now we will require the format to be the same.
         */
        private static bool CanGpuCopy(RenderTexture source, RenderTexture dest)
        {
            return (SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) != 0
                && source.graphicsFormat == dest.graphicsFormat
                && source.width == dest.width
                && source.height == dest.height
                && source.mipmapCount == dest.mipmapCount
                && source.antiAliasing == dest.antiAliasing;
        }

        private static bool CanGpuCopy(Texture2D source, RenderTexture dest)
        {
            return (SystemInfo.copyTextureSupport & CopyTextureSupport.TextureToRT) != 0
                && source.graphicsFormat == dest.graphicsFormat
                && source.width == dest.width
                && source.height == dest.height
                && source.mipmapCount == dest.mipmapCount;
        }

        private static bool CanGpuCopyIntoTexture2D(RenderTexture source, TextureConfig texture2DConfig, bool nonReadable)
        {
            // if we want a readable Texture2D from a RenderTexture then GPU copy will not work since RenderTextures only have GPU data
            return nonReadable && (SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) != 0
                && source.graphicsFormat == texture2DConfig.GraphicsFormat
                && source.width == texture2DConfig.Width
                && source.height == texture2DConfig.Height;
        }

        private static bool CanGpuCopyIntoTexture2D(Texture2D source, TextureConfig texture2DConfig, bool nonReadable)
        {
            // for some reason GPU copying fails when using crunch compression on the editor
            #if UNITY_EDITOR
                if (GraphicsFormatUtility.IsCrunchFormat(source.format))
                    return false;
            #endif

            // if we want a non-readable texture then we can copy. But if we want a readable texture and the source is not readable then we skip GPU copy
            return (nonReadable || source.isReadable) && (SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) != 0
                && source.graphicsFormat == texture2DConfig.GraphicsFormat
                && source.width == texture2DConfig.Width
                && source.height == texture2DConfig.Height;
        }
    }
}
