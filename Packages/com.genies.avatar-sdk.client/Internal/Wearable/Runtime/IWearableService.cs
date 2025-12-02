using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;

namespace Genies.Wearables
{
    /// <summary>
    /// Interface for managing wearable items including creation, retrieval, and caching operations.
    /// Provides methods for creating custom wearables, fetching owned wearables, and managing thriftable wearable collections.
    /// </summary>
    public interface IWearableService
    {
        /// <summary>
        /// Creates a new wearable item asynchronously with the specified configuration and icon.
        /// </summary>
        /// <param name="wearable">The UGC wearable definition containing template and customization data.</param>
        /// <param name="icon">The icon image data as a byte array for the wearable.</param>
        /// <param name="wearableId">Optional existing wearable ID for updates, null for new creation.</param>
        /// <param name="isThriftable">Whether the wearable should be available for thrifting by other users.</param>
        /// <returns>A task that completes with the wearable ID of the created or updated wearable.</returns>
        UniTask<string> CreateWearableAsync(Ugc.Wearable wearable, byte[] icon, string wearableId = null, bool isThriftable = true);
        
        /// <summary>
        /// Retrieves a specific wearable by its unique identifier.
        /// </summary>
        /// <param name="id">The unique wearable identifier to retrieve.</param>
        /// <returns>A task that completes with the wearable data, or null if not found.</returns>
        UniTask<Services.Model.Wearable> GetWearableByIdAsync(string id);
        
        /// <summary>
        /// Retrieves multiple wearables by their unique identifiers in a batch operation.
        /// </summary>
        /// <param name="ids">A list of wearable identifiers to retrieve.</param>
        /// <returns>A task that completes with a list of wearable data objects.</returns>
        UniTask<List<Services.Model.Wearable>> GetWearablesByIdsAsync(List<string> ids);
        
        /// <summary>
        /// Retrieves all wearables owned by the current user.
        /// </summary>
        /// <returns>A task that completes with a list of all owned wearable data.</returns>
        UniTask<List<Services.Model.Wearable>> GetAllOwnedWearablesAsync();
        
        /// <summary>
        /// Retrieves all wearable identifiers owned by the current user.
        /// </summary>
        /// <returns>A task that completes with a list of wearable identifiers.</returns>
        UniTask<List<string>> GetAllOwnedWearableIds();
        
        /// <summary>
        /// Clears the local cache of wearable data, forcing fresh retrieval on next access.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// A method call to fetch thriftable wearables from a user's thrift shop
        /// </summary>
        /// <param name="userId">User ID to fetch thrift shop of</param>
        /// <returns>WearableThriftList object containing wearable list, count and next cursor</returns>
        UniTask<WearableThriftList> GetThriftableWearbles(string userId);
    }
}
