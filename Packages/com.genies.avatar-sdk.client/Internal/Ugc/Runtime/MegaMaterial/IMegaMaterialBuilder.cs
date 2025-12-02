using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Ugc
{
    /// <summary>
    /// Defines the contract for building MegaMaterial instances from UGC data and configurations.
    /// This interface provides methods for creating materials that support multiple regions, styles,
    /// and complex rendering features required by the UGC system.
    /// </summary>
    public interface IMegaMaterialBuilder
    {
        /// <summary>
        /// Builds a MegaMaterial asynchronously from the specified split configuration.
        /// </summary>
        /// <param name="split">The split configuration containing styling and region data.</param>
        /// <returns>A task that completes with the constructed MegaMaterial instance.</returns>
        UniTask<MegaMaterial> BuildMegaMaterialAsync(Split split);
        
        /// <summary>
        /// Builds a MegaMaterial asynchronously from the specified split configuration and element reference.
        /// </summary>
        /// <param name="split">The split configuration containing styling and region data.</param>
        /// <param name="elementRef">Reference to the UGC element asset to build the material for.</param>
        /// <returns>A task that completes with the constructed MegaMaterial instance.</returns>
        UniTask<MegaMaterial> BuildMegaMaterialAsync(Split split, Ref<UgcElementAsset> elementRef);
        
        /// <summary>
        /// Builds a MegaMaterial synchronously from the specified element reference.
        /// </summary>
        /// <param name="elementRef">Reference to the UGC element asset to build the material for.</param>
        /// <param name="materialVersion">Optional material version identifier for versioning support.</param>
        /// <returns>The constructed MegaMaterial instance.</returns>
        MegaMaterial BuildMegaMaterial(Ref<UgcElementAsset> elementRef, string materialVersion = null);
    }
}
