using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;

namespace Genies.Avatars.Services
{
    /// <summary>
    /// Defines the contract for avatar service functionality including creation, retrieval, and updates of avatar data.
    /// This interface provides methods for managing user avatars through various backend systems.
    /// </summary>
    public interface IAvatarService
    {
        /// <summary>
        /// Gets or sets the currently loaded avatar, automatically handling definition deserialization and error recovery.
        /// </summary>
        Avatar LoadedAvatar { get; }
        
        /// <summary>
        /// Creates a new avatar with the specified gender/body type.
        /// </summary>
        /// <param name="gender">The gender or body type identifier for the new avatar.</param>
        /// <returns>A task that completes with true if the avatar was created successfully; otherwise, false.</returns>
        UniTask<bool> CreateAvatarAsync(string gender);

        /// <summary>
        /// Creates a new avatar using the provided avatar definition.
        /// </summary>
        /// <param name="avatarDefinition">The complete avatar definition containing appearance data.</param>
        /// <returns>A task that completes with true if the avatar was created successfully; otherwise, false.</returns>
        UniTask<bool> CreateAvatarAsync(Genies.Naf.AvatarDefinition avatarDefinition);

        /// <summary>
        /// Retrieves the avatar definition for the current user.
        /// </summary>
        /// <returns>A task that completes with the user's avatar definition.</returns>
        UniTask<Genies.Naf.AvatarDefinition> GetAvatarDefinitionAsync();

        /// <summary>
        /// Retrieves the avatar definition for a specific user by their user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose avatar to retrieve.</param>
        /// <returns>A task that completes with the specified user's avatar definition.</returns>
        UniTask<Genies.Naf.AvatarDefinition> GetUserAvatarDefinitionAsync(string userId);

        /// <summary>
        /// Retrieves all avatars associated with a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose avatars to retrieve.</param>
        /// <returns>A task that completes with a list of the user's avatars.</returns>
        UniTask<List<Avatar>> GetUserAvatarsAsync(string userId);

        /// <summary>
        /// Updates the current user's avatar with the provided definition.
        /// </summary>
        /// <param name="avatarDefinition">The updated avatar definition to apply.</param>
        /// <returns>A task that completes when the avatar has been updated.</returns>
        UniTask UpdateAvatarAsync(Genies.Naf.AvatarDefinition avatarDefinition);

        /// <summary>
        /// Retrieves the current user's avatar definition, or creates a new one if none exists.
        /// </summary>
        /// <param name="bodyType">The body type to use if creating a new avatar.</param>
        /// <returns>A task that completes with the user's existing or newly created avatar definition.</returns>
        UniTask<Genies.Naf.AvatarDefinition> GetOrCreateAvatarAsync(string bodyType);

        UniTask<string> UploadAvatarImageAsync(byte[] imageData, string avatarId);
    }
}
