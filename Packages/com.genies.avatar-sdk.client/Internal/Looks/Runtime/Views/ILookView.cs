using Cysharp.Threading.Tasks;
using Genies.Looks.Models;
using UnityEngine;

namespace Genies.Looks.Core
{
    /// <summary>
    /// Defines the contract for loading and displaying looks (avatar appearance configurations).
    /// This interface provides methods for managing look data, thumbnails, and presentation state.
    /// </summary>
    public interface ILookView
    {
        /// <summary>
        /// Gets the unique identifier of the look being displayed.
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// Sets the look definition data for this view.
        /// </summary>
        /// <param name="look">The look data containing avatar configuration and metadata.</param>
        /// <returns>A task that completes when the look definition has been set.</returns>
        public UniTask SetDefinition(LookData look);
        
        /// <summary>
        /// Retrieves the current look definition from this view.
        /// </summary>
        /// <returns>A task that completes with the look data currently configured in this view.</returns>
        public UniTask<LookData> GetDefinition();
        
        /// <summary>
        /// Pre-downloads any required assets for displaying the look.
        /// This helps ensure smooth rendering when the look is actually shown.
        /// </summary>
        /// <returns>A task that completes when all required assets have been downloaded.</returns>
        public UniTask PreDownload();
        
        /// <summary>
        /// Retrieves the thumbnail image for the look.
        /// </summary>
        /// <returns>A task that completes with the look's thumbnail texture.</returns>
        public UniTask<Texture2D> GetThumbnail();
        
        /// <summary>
        /// Checks if the look is ready to be displayed to the user.
        /// </summary>
        /// <returns>A task that completes with true if the look is ready to show; otherwise, false.</returns>
        public UniTask<bool> IsReadyToShowAsync();
        
        /// <summary>
        /// Cleans up resources used by the look view.
        /// This should be called when the look is no longer needed to free memory and resources.
        /// </summary>
        public void Clean();
    }
}
