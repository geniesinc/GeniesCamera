using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.SDKServices.Model;

namespace Genies.Closet.Gear
{
    /// <summary>
    /// Defines the contract for managing gear items including creation, retrieval, and updates.
    /// Gear items represent wearable and collectible items that can be managed through the Genies platform.
    /// </summary>
    public interface IGearService
    {
        /// <summary>
        /// Retrieves a paginated list of gear items.
        /// </summary>
        /// <param name="cursor">Optional cursor for pagination to retrieve the next set of results.</param>
        /// <param name="limit">Optional limit on the number of gear items to retrieve per request.</param>
        /// <returns>A task that completes with a response containing the list of gear items.</returns>
        UniTask<GearListResponse> GetGearListAsync(string cursor = null, decimal? limit = null);

        /// <summary>
        /// Creates a new gear item with the specified properties.
        /// </summary>
        /// <param name="body">The request body containing gear creation parameters.</param>
        /// <returns>A task that completes with a response containing the created gear information.</returns>
        UniTask<GearCreateResponse> CreateGearAsync(GearCreateRequest body);

        /// <summary>
        /// Retrieves gear items by their unique identifiers.
        /// </summary>
        /// <param name="gearIds">List of gear IDs to retrieve.</param>
        /// <returns>A task that completes with a response containing the requested gear items.</returns>
        UniTask<GearGetByIdsResponse> GetGearsByIdsAsync(List<string> gearIds);

        /// <summary>
        /// Updates an existing gear item with new properties.
        /// </summary>
        /// <param name="body">The request body containing gear update parameters.</param>
        /// <param name="gearId">The unique identifier of the gear item to update.</param>
        /// <returns>A task that completes with a response indicating the success of the update operation.</returns>
        UniTask<MessageResponse> UpdateGearAsync(GearUpdateRequest body, string gearId);
    }
}
