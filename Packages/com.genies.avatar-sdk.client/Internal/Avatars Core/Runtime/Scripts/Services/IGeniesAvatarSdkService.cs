using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Core service interface for loading and managing Genies avatars in runtime environments.
    /// Provides methods for creating avatar instances with customizable properties and animation controllers.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IGeniesAvatarSdkService
#else
    public interface IGeniesAvatarSdkService
#endif
    {
        internal UniTask Initialize();

        /// <summary>
        /// Loads the currently logged-in user's avatar as a runtime instance.
        /// </summary>
        /// <param name="avatarName">Optional custom name for the avatar GameObject. If null, uses default naming.</param>
        /// <param name="parent">Transform to parent the avatar under. If null, creates at scene root.</param>
        /// <param name="playerAnimationController">Custom animator controller to apply to the avatar. If null, uses default.</param>
        /// <param name="atlasResolution">Texture atlas resolution for avatar materials. Higher values provide better quality but use more memory.</param>
        /// <param name="waitUntilUserIsLoggedIn">If true, waits for user authentication before loading. If false, may fail if user is not logged in.</param>
        /// <returns>A UniTask that completes with an IGenie instance representing the loaded avatar.</returns>
        public UniTask<IGenie> LoadUserRuntimeAvatarAsync(string avatarName = null,
                                                                  Transform parent = null,
                                                                  RuntimeAnimatorController playerAnimationController = null,
                                                                  int atlasResolution = 512,
                                                                  bool waitUntilUserIsLoggedIn = false
        );

        /// <summary>
        /// Retrieves avatar data for a specific user by their user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose avatar data to retrieve.</param>
        /// <returns>A UniTask that completes with a list of Avatar models containing the user's avatar information.</returns>
        public UniTask<List<Genies.Services.Model.Avatar>> LoadAvatarsDataByUserIdAsync(string userId);

        /// <summary>
        /// Loads a default avatar configuration as a runtime instance.
        /// This provides a fallback avatar when user-specific avatars are unavailable.
        /// </summary>
        /// <param name="avatarName">Optional custom name for the avatar GameObject. If null, uses default naming.</param>
        /// <param name="parent">Transform to parent the avatar under. If null, creates at scene root.</param>
        /// <param name="playerAnimationController">Custom animator controller to apply to the avatar. If null, uses default.</param>
        /// <param name="atlasResolution">Texture atlas resolution for avatar materials. Higher values provide better quality but use more memory.</param>
        /// <returns>A UniTask that completes with an IGenie instance representing the default avatar.</returns>
        public UniTask<IGenie> LoadDefaultRuntimeAvatarAsync(string avatarName = null,
                                                                     Transform parent = null,
                                                                     RuntimeAnimatorController playerAnimationController = null,
                                                                     int atlasResolution = 512
        );

        /// <summary>
        /// Loads an avatar from a JSON definition string as a runtime instance.
        /// </summary>
        /// <param name="definition">JSON string containing the avatar definition with all customization data.</param>
        /// <param name="avatarName">Optional custom name for the avatar GameObject. If null, uses default naming.</param>
        /// <param name="parent">Transform to parent the avatar under. If null, creates at scene root.</param>
        /// <param name="playerAnimationController">Custom animator controller to apply to the avatar. If null, uses default.</param>
        /// <param name="atlasResolution">Texture atlas resolution for avatar materials. Higher values provide better quality but use more memory.</param>
        /// <returns>A UniTask that completes with an IGenie instance representing the loaded avatar.</returns>
        public UniTask<IGenie> LoadRuntimeAvatarAsync(string definition,
                                                              string avatarName = null,
                                                              Transform parent = null,
                                                              RuntimeAnimatorController playerAnimationController = null,
                                                              int atlasResolution = 512
        );

        /// <summary>
        /// Loads an avatar from an AvatarDefinition object as a runtime instance.
        /// </summary>
        /// <param name="definition">Structured AvatarDefinition object containing the avatar configuration.</param>
        /// <param name="avatarName">Optional custom name for the avatar GameObject. If null, uses default naming.</param>
        /// <param name="parent">Transform to parent the avatar under. If null, creates at scene root.</param>
        /// <param name="playerAnimationController">Custom animator controller to apply to the avatar. If null, uses default.</param>
        /// <param name="atlasResolution">Texture atlas resolution for avatar materials. Higher values provide better quality but use more memory.</param>
        /// <returns>A UniTask that completes with an IGenie instance representing the loaded avatar.</returns>
        public UniTask<IGenie> LoadRuntimeAvatarAsync(Naf.AvatarDefinition definition,
                                                              string avatarName = null,
                                                              Transform parent = null,
                                                              RuntimeAnimatorController playerAnimationController = null,
                                                              int atlasResolution = 512
        );

        internal UniTask<Naf.NativeUnifiedGenieController> CreateAvatarAsync(string definition = null,
                                                                   Transform parent = null,
                                                                   int atlasResolution = 512
        );

        /// <summary>
        /// Retrieves the avatar definition for the currently logged-in user as a JSON string.
        /// </summary>
        /// <param name="waitUntilUserIsLoggedIn">If true, waits for user authentication before retrieving. If false, may fail if user is not logged in.</param>
        /// <returns>A UniTask that completes with a JSON string containing the user's avatar definition.</returns>
        public UniTask<string> GetMyAvatarDefinition(bool waitUntilUserIsLoggedIn = false);

        /// <summary>
        /// Loads the avatar definition string for a specific user by their user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose avatar definition to retrieve.</param>
        /// <returns>A UniTask that completes with a JSON string containing the specified user's avatar definition.</returns>
        public UniTask<string> LoadAvatarDefStringByUserId(string userId);
    }
}
