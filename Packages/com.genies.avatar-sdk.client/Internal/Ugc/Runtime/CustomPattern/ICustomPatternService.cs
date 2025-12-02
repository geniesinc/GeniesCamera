using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc.CustomPattern
{
    /// <summary>
    /// Defines the contract for managing custom patterns in the UGC system.
    /// This interface provides methods for creating, retrieving, updating, and deleting custom patterns
    /// that can be applied to avatar wearables and elements.
    /// </summary>
    public interface ICustomPatternService
    {
        /// <summary>
        /// Initializes the custom pattern service and prepares it for use.
        /// </summary>
        /// <returns>A task that completes when the service has been initialized.</returns>
        UniTask InitializeAsync();
        
        /// <summary>
        /// Checks if a custom pattern with the specified ID exists in the system.
        /// </summary>
        /// <param name="patternId">The unique identifier of the pattern to check.</param>
        /// <returns>A task that completes with true if the pattern exists; otherwise, false.</returns>
        UniTask<bool> DoesCustomPatternExistAsync(string patternId);
        
        /// <summary>
        /// Checks if a custom pattern belongs to another user.
        /// </summary>
        /// <param name="patternId">The unique identifier of the pattern to check.</param>
        /// <returns>A task that completes with the user ID if the pattern belongs to another user; otherwise, null.</returns>
        UniTask<string> DoesCustomPatternFromOtherUser(string patternId);
        
        /// <summary>
        /// Gets the total count of custom patterns available in the system.
        /// </summary>
        /// <returns>A task that completes with the total number of custom patterns.</returns>
        UniTask<int> GetCustomPatternsCountAsync();
        
        /// <summary>
        /// Retrieves all custom pattern IDs available in the system.
        /// </summary>
        /// <returns>A task that completes with a list of all custom pattern identifiers.</returns>
        UniTask<List<string>> GetAllCustomPatternIdsAsync();
        
        /// <summary>
        /// Loads the texture for a custom pattern by its ID.
        /// </summary>
        /// <param name="customPatternId">The unique identifier of the custom pattern.</param>
        /// <returns>A task that completes with a reference to the pattern's texture.</returns>
        UniTask<Ref<Texture2D>> LoadCustomPatternTextureAsync(string customPatternId);
        
        /// <summary>
        /// Loads the texture for a custom pattern by user ID and pattern ID.
        /// </summary>
        /// <param name="userId">The ID of the user who owns the pattern.</param>
        /// <param name="customPatternId">The unique identifier of the custom pattern.</param>
        /// <returns>A task that completes with a reference to the pattern's texture.</returns>
        UniTask<Ref<Texture2D>> LoadCustomPatternTextureAsync(string userId, string customPatternId);
        
        /// <summary>
        /// Loads the pattern data and configuration for a custom pattern.
        /// </summary>
        /// <param name="customPatternId">The unique identifier of the custom pattern.</param>
        /// <returns>A task that completes with the pattern configuration data.</returns>
        UniTask<Pattern> LoadCustomPatternAsync(string customPatternId);

        /// <summary>
        /// Creates or updates a custom pattern with the specified texture and configuration.
        /// This method can either create a new pattern (when customPatternId is null) or update an existing one.
        /// </summary>
        /// <param name="newPattern">The texture to use for the new or updated pattern.</param>
        /// <param name="pattern">The pattern configuration data. If null, a default configuration will be created.</param>
        /// <param name="customPatternId">Set to null to create a new pattern, or provide an existing pattern ID to update.</param>
        /// <returns>A task that completes with the pattern ID on success, or null on failure.</returns>
        UniTask<string> CreateOrUpdateCustomPatternAsync(Texture2D newPattern, Pattern pattern = null, string customPatternId = null);

        /// <summary>
        /// Deletes a custom pattern from the system.
        /// </summary>
        /// <param name="customPatternId">The unique identifier of the pattern to delete.</param>
        /// <returns>A task that completes with true if the pattern was successfully deleted; otherwise, false.</returns>
        UniTask<bool> DeletePatternAsync(string customPatternId);
        
        /// <summary>
        /// Deletes all custom patterns from the system.
        /// </summary>
        /// <returns>A task that completes when all patterns have been deleted.</returns>
        UniTask DeleteAllPatternsAsync();
    }
}
