using System;
using Cysharp.Threading.Tasks;
using Genies.AvatarEditor.Core;
using UnityEngine;

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Initializes the Genies Avatar SDK.
        /// Calling is optional as all operations will initialize the SDK if it is not already initialized.
        /// This method is safe to call multiple times - subsequent calls return the cached initialization result.
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public static async UniTask<bool> InitializeAsync() => await Instance.InitializeInternalAsync();

        /// <summary>
        /// Loads a default avatar with optional configuration.
        /// </summary>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadDefaultAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();

            if (IsLoggedIn is false)
            {
                throw new NotImplementedException("Spawning a default avatar while not logged in is not yet supported. Log in first.");
            }

            return await Instance.CoreSdk.AvatarApi.LoadDefaultAvatarAsync(avatarName, parent, playerAnimationController);
        }

        /// <summary>
        /// Loads the authenticated user's avatar with optional configuration. Falls back to default avatar if user is not logged in (TODO).
        /// </summary>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadUserAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.LoadUserAvatarAsync(avatarName, parent, playerAnimationController);
        }

        private static readonly Lazy<AvatarSdk> _instance = new Lazy<AvatarSdk>(() => new AvatarSdk());
        private static AvatarSdk Instance => _instance.Value;

        private CoreSdk CoreSdk { get; }
        private UniTask<bool>? InitializationTask { get; set; }
        private object InitializationLock { get; } = new object();

        private AvatarSdk()
        {
            CoreSdk = new CoreSdk();
        }

        private async UniTask<bool> InitializeInternalAsync()
        {
            if (InitializationTask.HasValue)
            {
                return await InitializationTask.Value;
            }

            lock (InitializationLock)
            {
                if (InitializationTask.HasValue)
                {
                    return InitializationTask.Value.GetAwaiter().GetResult();
                }

                InitializationTask = PerformInitializationAsync().Preserve();
            }

            return await InitializationTask.Value;
        }

        private async UniTask<bool> PerformInitializationAsync()
        {
            try
            {
                var avatarEditorResult = await AvatarEditorSDK.InitializeAsync();
                var coreSdkResult = await CoreSdk.InitializeAsync();
                return avatarEditorResult && coreSdkResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize the Avatar SDK: {ex.Message}");
                return false;
            }
        }
    }
}
