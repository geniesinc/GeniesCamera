using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.CrashReporting;
using Genies.Naf;
using GnWrappers;
using Genies.ServiceManagement;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Gender types for avatar body configuration
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum GenderType
#else
    public enum GenderType
#endif
    {
        Male,
        Female,
        Androgynous
    }

    /// <summary>
    /// Body size types for avatar body configuration
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum BodySize
#else
    public enum BodySize
#endif
    {
        Skinny,
        Medium,
        Heavy
    }

    /// <summary>
    /// Static convenience facade for opening and closing the Avatar Editor.
    /// - Auto-initializes required services on first use
    /// - Provides public static methods for opening and closing the editor
    /// - Follows the same pattern as GeniesAvatarsSdk for consistency
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarEditorSDK
#else
    public static class AvatarEditorSDK
#endif
    {
        private static bool IsInitialized =>
            InitializationCompletionSource is not null
            && InitializationCompletionSource.Task.Status == UniTaskStatus.Succeeded;
        private static UniTaskCompletionSource InitializationCompletionSource { get; set; }

        private static IAvatarEditorSdkService CachedService { get; set; }
        private static bool EventsSubscribed { get; set; }

        /// <summary>
        /// Event raised when the editor is opened.
        /// </summary>
        public static event Action EditorOpened = delegate { };

        /// <summary>
        /// Event raised when the editor is closed.
        /// </summary>
        public static event Action EditorClosed = delegate { };

        #region Initialization / Service Access

        /// <summary>
        /// Ensures Avatar Editor SDK dependencies are initialized with the specified target environment.
        /// This method calls ServiceManager.InitializeAppAsync internally with the required installers.
        /// If the consuming application requires specific initialization order or custom installers,
        /// it should call ServiceManager.InitializeAppAsync before using any Avatar Editor SDK API.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise</returns>
        public static async UniTask<bool> InitializeAsync()
        {
            return await AvatarEditorInitializer.Instance.InitializeAsync();
        }

        /// <summary>
        /// Checks that the AvatarEditorSdkService exists and automatically initializes one if not present.
        /// </summary>
        internal static async UniTask<IAvatarEditorSdkService> GetOrCreateAvatarEditorSdkInstance()
        {
            if (await InitializeAsync() is false)
            {
                CrashReporter.LogError("Avatar editor could not be initialized.");
                return default;
            }

            var service = ServiceManager.Get<IAvatarEditorSdkService>();
            SubscribeToServiceEvents(service);
            return service;
        }

        /// <summary>
        /// Subscribes to service events and forwards them to static events.
        /// </summary>
        private static void SubscribeToServiceEvents(IAvatarEditorSdkService service)
        {
            if (service == null)
            {
                return;
            }

            if (ReferenceEquals(service, CachedService)
                && EventsSubscribed)
            {
                return;
            }

            // Cache the service and subscribe to its events
            CachedService = service;
            CachedService.EditorOpened += OnEditorOpened;
            CachedService.EditorClosed += OnEditorClosed;

            EventsSubscribed = true;
        }

        /// <summary>
        /// Handles the editor opened event from the service and forwards it to the static event.
        /// </summary>
        private static void OnEditorOpened()
        {
            EditorOpened?.Invoke();
        }

        /// <summary>
        /// Handles the editor closed event from the service and forwards it to the static event.
        /// </summary>
        private static void OnEditorClosed()
        {
            EditorClosed?.Invoke();
        }

        #endregion

        #region Public Static API

        /// <summary>
        /// Opens the avatar editor with the specified avatar and camera.
        /// </summary>
        /// <param name="avatar">The avatar to edit. If null, loads the current user's avatar.</param>
        /// <param name="camera">The camera to use for the editor. If null, uses Camera.main.</param>
        /// <returns>A UniTask that completes when the editor is opened.</returns>
        public static async UniTask OpenEditorAsync(GeniesAvatar avatar, Camera camera = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.OpenEditorAsync(avatar, camera);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to open avatar editor: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the avatar editor and cleans up resources.
        /// </summary>
        /// <returns>A UniTask that completes when the editor is closed.</returns>
        public static async UniTask CloseEditorAsync()
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.CloseEditorAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to close avatar editor: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the currently active avatar being edited in the editor.
        /// </summary>
        /// <returns>The currently active GeniesAvatar, or null if no avatar is currently being edited</returns>
        public static GeniesAvatar GetCurrentActiveAvatar()
        {
            try
            {
                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                return avatarEditorSdkService?.GetCurrentActiveAvatar();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get current active avatar: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets whether the avatar editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public static bool IsEditorOpen
        {
            get
            {
                try
                {
                    var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                    return avatarEditorSdkService?.IsEditorOpen ?? false;
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"Failed to get editor open state: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public static async UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GetWearableAssetInfoListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get wearable asset info list: {ex.Message}");
                return new List<WearableAssetInfo>();
            }
        }

        /// <summary>
        /// Grants an asset to a user, adding it to their inventory
        /// </summary>
        /// <param name="assetId">Id of the asset</param>
        public static async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GiveAssetToUserAsync(assetId);
            }
            catch (Exception ex)
            {
                string error = $"Failed to give asset to user: {ex.Message}";
                CrashReporter.LogError(error);
                return (false, error);
            }
        }

        /// <summary>
        /// Gets the assets of the logged in user
        /// </summary>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public static async UniTask<List<WearableAssetInfo>> GetUsersAssetsAsync()
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GetUsersAssetsAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get wearable asset info list: {ex.Message}");
                return new List<WearableAssetInfo>();
            }
        }

        /// <summary>
        /// Equips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to equip the asset on</param>
        /// <param name="wearableId">The ID of the wearable to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask EquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipOutfitAsync(avatar, wearableId, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip outfit: {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to equip the asset on</param>
        /// <param name="wearableId">The ID of the wearable to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask UnEquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.UnEquipOutfitAsync(avatar, wearableId, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip outfit: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a skin color on the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to equip the skin color on</param>
        /// <param name="skinColor">The color to apply as skin color</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask SetSkinColorAsync(GeniesAvatar avatar, Color skinColor, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SetSkinColorAsync(avatar, skinColor, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip skin color: {ex.Message}");
            }
        }

        /// <summary>
        /// Equips a tattoo on the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to equip the tattoo on</param>
        /// <param name="tattooId">The ID of the tattoo to equip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo should be placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask EquipTattooAsync(GeniesAvatar avatar, string tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipTattooAsync(avatar, tattooId, tattooSlot, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip tattoo: {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips a tattoo from the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the tattoo from</param>
        /// <param name="tattooId">The ID of the tattoo to unequip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo is placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask UnEquipTattooAsync(GeniesAvatar avatar, string tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.UnEquipTattooAsync(avatar, tattooId, tattooSlot, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip tattoo: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the body preset for the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to set the body preset on</param>
        /// <param name="preset">The GSkelModifierPreset to apply</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask SetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, GSkelModifierPreset preset, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SetNativeAvatarBodyPresetAsync(avatar, preset, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set body preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the avatar body type with specified gender and body size.
        /// </summary>
        /// <param name="avatar">The avatar to set the body type on</param>
        /// <param name="genderType">The gender type (Male, Female, Androgynous)</param>
        /// <param name="bodySize">The body size (Skinny, Medium, Heavy)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask representing the async operation</returns>
        public static async UniTask SetAvatarBodyTypeAsync(GeniesAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SetAvatarBodyTypeAsync(avatar, genderType, bodySize, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar body type: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current avatar definition locally or to the cloud based on the current editing mode.
        /// </summary>
        /// <returns>A UniTask that completes when the save operation is finished.</returns>
        public static async UniTask SaveAvatarDefinitionAsync(GeniesAvatar avatar)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SaveAvatarDefinitionAsync(avatar);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The avatar to save.</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public static async UniTask SaveAvatarDefinitionLocallyAsync(GeniesAvatar avatar, string profileId = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                avatarEditorSdkService.SaveAvatarDefinitionLocally(avatar, profileId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition locally: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current avatar definition to the cloud.
        /// </summary>
        /// <returns>A UniTask that completes when the cloud save operation is finished.</returns>
        public static async UniTask SaveAvatarDefinitionToCloudAsync(GeniesAvatar avatar)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SaveAvatarDefinitionAsync(avatar);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition to cloud: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads an avatar definition from a string and starts editing with it.
        /// </summary>
        /// <param name="profileId">The profile to load</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public static async UniTask<GeniesAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                var avatar = await avatarEditorSdkService.LoadFromLocalAvatarDefinitionAsync(profileId, cancellationToken);
                if (avatar == null)
                {
                    CrashReporter.LogError($"Failed to load avatar with profileId: {profileId}");
                    return null;
                }

                return avatar;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar from definition: {ex.Message}");
                return null;
            }
        }

        public static async UniTask<GeniesAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                var avatar = await avatarEditorSdkService.LoadFromLocalGameObjectAsync(profileId, cancellationToken);
                if (avatar == null)
                {
                    CrashReporter.LogError($"Failed to load avatar from game object: {profileId}");
                    return null;
                }

                return avatar;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar definition: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the save option for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public static async UniTask SetEditorSaveOptionAsync(AvatarSaveOption saveOption)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    CrashReporter.LogError("AvatarEditorSdkService not found. Cannot set save option.");
                    return;
                }

                avatarEditorSdkService.SetEditorSaveOption(saveOption);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set editor save option: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the save option and profile ID for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        /// <param name="profileId">The profile ID to use when saving locally</param>
        public static async UniTask SetEditorSaveOptionAsync(AvatarSaveOption saveOption, string profileId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    throw new NullReferenceException("AvatarEditorSdkService not found");
                }

                avatarEditorSdkService.SetEditorSaveOption(saveOption, profileId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set editor save option: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the save settings for the avatar editor.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public static async UniTask SetEditorSaveSettingsAsync(AvatarSaveSettings saveSettings)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    throw new NullReferenceException("AvatarEditorSdkService not found");
                }

                avatarEditorSdkService.SetEditorSaveSettings(saveSettings);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set editor save settings: {ex.Message}");
            }
        }
        #endregion
    }
}
