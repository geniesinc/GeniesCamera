using Genies.Utilities;
using Toolbox.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    /// <summary>
    /// Settings used by the <see cref="LodUgcOutfitAssetBuilder"/> to export the texture maps for each split.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SplitTextureSettings", menuName = "Genies/UGC/Split Texture Settings")]
#endif
    public sealed class SplitTextureSettings : ScriptableObject
    {
        [FormerlySerializedAs("textureSettings")] public TextureSettings TextureSettings;
        [FormerlySerializedAs("useSurfacePixelDensity")] [Tooltip("Calculates the map resolution based on a target surface pixel density. Leave it disabled to use the resolution specified in the texture settings")]
        public bool UseSurfacePixelDensity;
        [FormerlySerializedAs("surfacePixelDensity")] [DisableIf(nameof(UseSurfacePixelDensity), false)]
        public SurfacePixelDensity SurfacePixelDensity;
    }
}
