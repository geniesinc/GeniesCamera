using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    /// <summary>
    /// Exports a texture map from any <see cref="Material"/> that uses a shader that supports map exporting.
    /// You can configure how to get each color channel from different export indices and the texture configuration for
    /// the final texture output.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialMapExporter", menuName = "Genies/Material Baking/Material Map Exporter")]
#endif
    public sealed class MaterialMapExporter : ScriptableObject
    {
        private const string _normalShaderName = "Genies/Utils/RGB to AG Normal";

        public static readonly int ExportChannelPropertyId = Shader.PropertyToID("_ChannelExport");

        private static Material _normalSwizzleMaterial;

        private static GraphicsFormat _renderTextureFormat;
        private static GraphicsFormat _texture2DFormat;

        [Header("Color Channels")][Space(8)]
        [Tooltip("Used for the channels where read None is specified")]
        public Color defaultChannels = Color.clear;
        public Channel rChannel = new() { readChannel = TextureChannel.R };
        public Channel gChannel = new() { readChannel = TextureChannel.G };
        public Channel bChannel = new() { readChannel = TextureChannel.B };
        public Channel aChannel = new() { readChannel = TextureChannel.A };
        [Tooltip("Check this so proper channel swizzling is applied when DXTnm format is being used by the current platform")]
        public bool isNormalMap = false;

        [Space(16)]
        [Tooltip("Default texture settings to use for exports when no override is given")]
        public TextureSettings defaultTextureSettings;

        /// <summary>
        /// Whether or not the exported map is a direct raw export from a single export channel (no remapping of channels).
        /// </summary>
        public bool IsRawExport =>
            rChannel.exportIndex == gChannel.exportIndex &&
            gChannel.exportIndex == bChannel.exportIndex &&
            bChannel.exportIndex == aChannel.exportIndex &&
            rChannel.readChannel is TextureChannel.R &&
            gChannel.readChannel is TextureChannel.G &&
            bChannel.readChannel is TextureChannel.B &&
            aChannel.readChannel is TextureChannel.A;

        /// <summary>
        /// If this is a raw export map this will return the export index, else it will return -1.
        /// </summary>
        public int RawExportIndex => IsRawExport ? rChannel.exportIndex : -1;

        private void OnEnable()
        {
#if UNITY_2023_1_OR_NEWER
            _renderTextureFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormatUsage.Render);
            _texture2DFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormatUsage.ReadPixels);
#else
            _renderTextureFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_UNorm, FormatUsage.Render);
            _texture2DFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_UNorm, FormatUsage.ReadPixels);
#endif
        }

        /// <summary>
        /// Exports the configured map from the given material. It assumes that the material is using a shader that implements our exporting features.
        /// </summary>
        /// <param name="material">the material instance that the texture map will be exported from.</param>
        /// <param name="settings"><see cref="TextureSettings"/> to use for the output. Pass null to use the default settings defined
        /// in this exporter</param>
        public Texture Export(Material material, TextureSettings settings = null, Material postProcessingMaterial = null)
        {
            return ExportAsync(material, settings, postProcessingMaterial, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Exports the configured map from the given material. It assumes that the material is using a shader that implements our exporting features.
        /// </summary>
        /// <param name="material">the material instance that the texture map will be exported from.</param>
        /// <param name="settings"><see cref="TextureSettings"/> to use for the output. Pass null to use the default settings defined
        /// in this exporter</param>
        public UniTask<Texture> ExportAsync(Material material, TextureSettings settings = null, Material postProcessingMaterial = null)
        {
            return ExportAsync(material, settings, postProcessingMaterial, async: true);
        }

        /// <summary>
        /// Converts the configured color channels into a list of the export indices connected to their corresponding channel maps
        /// that can be used with the <see cref="TextureChannelTool"/>. The returned <see cref="ChannelMap"/> data will not contain
        /// the textures, those must be assigned manually.
        /// </summary>
        public List<(int exportIndex, ChannelMap channelMap)> GetChannelMaps()
        {
            bool rChannelNotChecked = rChannel.readChannel is not TextureChannel.None;
            bool gChannelNotChecked = gChannel.readChannel is not TextureChannel.None;
            bool bChannelNotChecked = bChannel.readChannel is not TextureChannel.None;
            bool aChannelNotChecked = aChannel.readChannel is not TextureChannel.None;

            (int exportIndex, ChannelMap channelMap) GetExportChannelMap(int exportIndex)
            {
                (int exportIndex, ChannelMap channelMap) exportChannelMap = (exportIndex, new ChannelMap()
                {
                    rOutput = TextureChannel.None,
                    gOutput = TextureChannel.None,
                    bOutput = TextureChannel.None,
                    aOutput = TextureChannel.None,
                });

                if (rChannelNotChecked && rChannel.exportIndex == exportIndex)
                {
                    exportChannelMap.channelMap.rOutput = rChannel.readChannel;
                    rChannelNotChecked = false;
                }

                if (gChannelNotChecked && gChannel.exportIndex == exportIndex)
                {
                    exportChannelMap.channelMap.gOutput = gChannel.readChannel;
                    gChannelNotChecked = false;
                }

                if (bChannelNotChecked && bChannel.exportIndex == exportIndex)
                {
                    exportChannelMap.channelMap.bOutput = bChannel.readChannel;
                    bChannelNotChecked = false;
                }

                if (aChannelNotChecked && aChannel.exportIndex == exportIndex)
                {
                    exportChannelMap.channelMap.aOutput = aChannel.readChannel;
                    aChannelNotChecked = false;
                }

                return exportChannelMap;
            }

            var exportChannelMaps = new List<(int, ChannelMap)>();

            if (rChannelNotChecked)
            {
                exportChannelMaps.Add(GetExportChannelMap(rChannel.exportIndex));
            }

            if (gChannelNotChecked)
            {
                exportChannelMaps.Add(GetExportChannelMap(gChannel.exportIndex));
            }

            if (bChannelNotChecked)
            {
                exportChannelMaps.Add(GetExportChannelMap(bChannel.exportIndex));
            }

            if (aChannelNotChecked)
            {
                exportChannelMaps.Add(GetExportChannelMap(aChannel.exportIndex));
            }

            return exportChannelMaps;
        }

        private async UniTask<Texture> ExportAsync(Material material, TextureSettings settings, Material postProcessingMaterial, bool async)
        {
            settings = settings ? settings : defaultTextureSettings;
            if (!settings)
            {
                Debug.LogError($"[{nameof(MaterialMapExporter)}] no texture settings were provided and this map exporter doesn't have defaults");
                return null;
            }

            bool isRawExport = IsRawExport;
            RenderTexture renderTexture = CreateRenderTexture(ignoreDefaultChannels: isRawExport, settings);

            // optimize when doing raw exports so we don't have to create extra temporary render textures and use channel maps
            if (isRawExport)
            {
                if (async)
                {
                    await RenderExportAsync(rChannel.exportIndex, material, renderTexture);
                }
                else
                {
                    RenderExport(rChannel.exportIndex, material, renderTexture);
                }

                return await GetFinalTextureAsync(renderTexture, settings, isNormalMap, postProcessingMaterial, async);
            }

            // write all the export channels on the render texture
            List<(int, ChannelMap)> channelMaps = GetChannelMaps();
            RenderTexture tmpTexture = GetTemporaryRenderTexture(settings);

            foreach ((int exportIndex, ChannelMap incompleteChannelMap) in channelMaps)
            {
                // render the export index from the material into a temporary render texture
                if (async)
                {
                    await RenderExportAsync(exportIndex, material, tmpTexture);
                }
                else
                {
                    RenderExport(exportIndex, material, tmpTexture);
                }

                // write the mapped color channels from the export into our render texture
                ChannelMap channelMap = incompleteChannelMap;
                channelMap.texture = tmpTexture;
                renderTexture.WriteChannels(channelMap);
            }

            RenderTexture.ReleaseTemporary(tmpTexture);

            return await GetFinalTextureAsync(renderTexture, settings, isNormalMap, postProcessingMaterial, async);
        }

        // based on config creates a new/temporary render texture for rendering export channels
        private RenderTexture CreateRenderTexture(bool ignoreDefaultChannels, TextureSettings settings)
        {
            RenderTexture renderTexture = settings.textureType switch
            {
                TextureSettings.Type.Texture2D => GetTemporaryRenderTexture(settings),
                TextureSettings.Type.RenderTexture => settings.GetTextureConfig(_renderTextureFormat).CreateRenderTexture(),
                _ => throw new ArgumentOutOfRangeException("Texture Type", $"Unkown shader export map texture type: {settings.textureType}")
            };

            if (ignoreDefaultChannels)
            {
                return renderTexture;
            }

            bool usesDefaultChannels =
                rChannel.readChannel is TextureChannel.None ||
                gChannel.readChannel is TextureChannel.None ||
                bChannel.readChannel is TextureChannel.None ||
                aChannel.readChannel is TextureChannel.None;

            // if using any of the default channels then write them to the render texture
            if (usesDefaultChannels)
            {
                renderTexture.WriteColor(defaultChannels);
            }

            return renderTexture;
        }

        public static bool SupportsMapExporting(Shader shader)
        {
            if (!shader)
            {
                return false;
            }

            var material = new Material(shader);
            bool result = SupportsMapExporting(material);

#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
#else
            Destroy(material);
#endif

            return result;
        }

        public static bool SupportsMapExporting(Material material)
        {
            return material && material.HasProperty(ExportChannelPropertyId);
        }

        public static async UniTask RenderExportAsync(int exportIndex, Material material, RenderTexture destination)
        {
            await OperationQueue.EnqueueAsync(OperationCost.Low);
            RenderExport(exportIndex, material, destination);
        }

        public static void RenderExport(int exportIndex, Material material, RenderTexture destination)
        {
            material.SetFloat(ExportChannelPropertyId, exportIndex);

            // we use the very last pass from our shaders as the rest is just URP stuff
            // Graphics.Blit(null, destination, material, material.shader.passCount - 1);
            Graphics.Blit(null, destination, material);

            if (destination.useMipMap && !destination.autoGenerateMips)
            {
                destination.GenerateMips();
            }
        }

        private static RenderTexture GetTemporaryRenderTexture(TextureSettings settings)
        {
            // get config and descriptor for a temporary render texture (we don't want mips and we want point filter mode)
            TextureConfig textureConfig = settings.GetTextureConfig(_renderTextureFormat);
            textureConfig.MipMaps = false;
            textureConfig.FilterMode = FilterMode.Point;
            RenderTextureDescriptor descriptor = textureConfig.GetRenderTextureDescriptor();
            descriptor.useMipMap = descriptor.autoGenerateMips = false;

            return textureConfig.GetTemporaryRenderTexture(descriptor);
        }

        // given the render texture used to render all channels, creates the final texture and release resources
        private static async UniTask<Texture> GetFinalTextureAsync(RenderTexture renderTexture, TextureSettings settings, bool isNormalMap, Material postProcessingMaterial, bool async)
        {
            // if we have to swizzle normals then do it (and apply post-processing after)
            if (isNormalMap && ShouldDoNormalMapSwizzle())
            {
                RenderTexture tmpCopy;

                if (postProcessingMaterial)
                {
                    tmpCopy = renderTexture.CreateTemporaryEmpty(forceNoMips: true);
                    await RenderPass(renderTexture, tmpCopy, _normalSwizzleMaterial, async);
                    await RenderPass(tmpCopy, renderTexture, postProcessingMaterial, async);
                }
                else
                {
                    tmpCopy = renderTexture.CreateTemporaryCopy(forceNoMips: true);
                    await RenderPass(tmpCopy, renderTexture, _normalSwizzleMaterial, async);
                }

                RenderTexture.ReleaseTemporary(tmpCopy);
            }
            // if no need for normal swizzling but we have a post-processing, then apply it
            else if (postProcessingMaterial)
            {
                RenderTexture tmpCopy = renderTexture.CreateTemporaryCopy(forceNoMips: true);
                await RenderPass(tmpCopy, renderTexture, postProcessingMaterial, async);
                RenderTexture.ReleaseTemporary(tmpCopy);
            }

            if (settings.textureType is TextureSettings.Type.RenderTexture)
            {
                if (renderTexture.useMipMap && !renderTexture.autoGenerateMips)
                {
                    renderTexture.GenerateMips();
                }

                return renderTexture;
            }

            if (async)
            {
                await OperationQueue.EnqueueAsync(OperationCost.High);
            }

            // convert the render texture into the final Texture2D output
            TextureConfig texture2DConfig = settings.GetTextureConfig(_texture2DFormat);
            Texture2D texture = settings.compress ?
                renderTexture.ToCompressedTexture2D(texture2DConfig, settings.highQualityCompression, settings.nonReadable) :
                renderTexture.ToTexture2D(texture2DConfig, settings.nonReadable);

            RenderTexture.ReleaseTemporary(renderTexture);
            return texture;

            async UniTask RenderPass(Texture source, RenderTexture destination, Material material, bool async)
            {
                if (async)
                {
                    await OperationQueue.EnqueueAsync(OperationCost.Low);
                }

                Graphics.Blit(source, destination, material);
            }
        }

        private static bool ShouldDoNormalMapSwizzle()
        {
            if (_normalSwizzleMaterial)
            {
                return true;
            }

            // normal map swizzle is only required on platforms supporting the DXT5nm format for normal maps (shaders will expect AG normals)
            if (GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.UNITY_NO_DXT5nm))
            {
                return false;
            }

            // initialize the normal swizzle material and return true
            var shader = Shader.Find(_normalShaderName);
            if (!shader)
            {
                Debug.LogError($"[{nameof(MaterialMapExporter)}] couldn't load normal swizzle shader from name: {_normalShaderName}. Make sure the shader is included in the build");
                return false;
            }

            _normalSwizzleMaterial = new Material(shader);
            return true;
        }

        [Serializable]
        public struct Channel
        {
            public int exportIndex;
            public TextureChannel readChannel;
        }
    }
}
