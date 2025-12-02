using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Ugc
{
    /// <summary>
    /// Defines the contract for rendering wearables and individual elements within the UGC system.
    /// This interface provides factory methods for creating render instances that can display
    /// UGC content with full styling and animation support.
    /// </summary>
    public interface IWearableRenderer
    {
        /// <summary>
        /// Creates and returns a wearable render instance for the specified wearable configuration.
        /// The render instance handles all elements within the wearable and their interactions.
        /// </summary>
        /// <param name="wearable">The wearable configuration to render.</param>
        /// <returns>A task that completes with a fully configured wearable render instance.</returns>
        UniTask<IWearableRender> RenderWearableAsync(Wearable wearable);
        
        /// <summary>
        /// Creates and returns an element render instance for the specified element ID.
        /// </summary>
        /// <param name="elementId">The unique identifier of the element to render.</param>
        /// <param name="materialVersion">Optional material version identifier for versioning support.</param>
        /// <returns>A task that completes with a fully configured element render instance.</returns>
        UniTask<IElementRender> RenderElementAsync(string elementId, string materialVersion = null);
        
        /// <summary>
        /// Creates and returns an element render instance for the specified element asset reference.
        /// </summary>
        /// <param name="elementRef">Reference to the UGC element asset to render.</param>
        /// <param name="materialVersion">Optional material version identifier for versioning support.</param>
        /// <returns>A task that completes with a fully configured element render instance.</returns>
        UniTask<IElementRender> RenderElementAsync(Ref<UgcElementAsset> elementRef, string materialVersion = null);
    }
}
