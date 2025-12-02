using Genies.Utilities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Genies.Avatars
{
    /// <summary>
    /// Settings used by the <see cref="MegaSkinGenieMaterial"/> to setup the tattoos render texture and slots.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MegaSkinTattooSettings", menuName = "Genies/MegaSkin Tattoo Settings")]
#endif
    public sealed class MegaSkinTattooSettings : ScriptableObject
    {
        [Tooltip("The number of tattoo slots available")]
        public int slots = 8;
        public int slotTextureWidth = 512;
        public int slotTextureHeight = 512;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public FilterMode filterMode = FilterMode.Bilinear;
        public int anisoLevel = 1;
        public bool mipMaps = true;
        public float mipMapBias = 0.0f;

        public RenderTexture CreateTattoosRenderTexture()
        {
            // setup the texture config
            var config = new TextureConfig
            {
                Width = slotTextureWidth,
                Height = slotTextureHeight,
#if UNITY_2023_1_OR_NEWER
                GraphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormatUsage.Render),
#else
                GraphicsFormat = SystemInfo.GetCompatibleFormat(GraphicsFormat.R8G8B8A8_UNorm, FormatUsage.Render),
#endif

                Dimension = TextureDimension.Tex2DArray,
                WrapMode = wrapMode,
                WrapModeU = wrapMode,
                WrapModeV = wrapMode,
                WrapModeW = wrapMode,
                FilterMode = filterMode,
                AnisoLevel = anisoLevel,
                MipMaps = mipMaps,
                MipMapBias = mipMapBias,
            };

            RenderTextureDescriptor descriptor = config.GetRenderTextureDescriptor();
            descriptor.volumeDepth = slots;

            return config.CreateRenderTexture(descriptor);
        }
    }
}
