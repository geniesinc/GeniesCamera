using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.Avatars.Services;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Login.Native;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Refs;
using GnWrappers;
using Genies.ServiceManagement;
using Genies.Utilities;
using Genies.VirtualCamera;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Implementation of IAvatarEditorSdkService providing avatar editor functionality.
    /// Handles opening and closing the editor with proper initialization and cleanup.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarEditorSdkService : IAvatarEditorSdkService, IDisposable
#else
    public class AvatarEditorSdkService : IAvatarEditorSdkService, IDisposable
#endif
    {
        private const string _avatarEditorPath = "Prefabs/AvatarEditor";
        private static GameObject _avatarEditorPrefab, _avatarEditorInstance;
        private static Camera _currentCamera;
        private static UniTaskCompletionSource _editorOpenedSource, _editorClosedSource;
        private readonly object _editorOpenedLock = new(), _editorClosedLock = new();
        private const string _headTransformPath = "Root/Hips/Spine/Spine1/Spine2/Neck/Head";
        private GeniesAvatar _currentActiveAvatar;
        private AvatarSaveSettings? _pendingSaveSettings;

        // Static persistent save settings that survive across editor sessions
        private static AvatarSaveSettings _persistentSaveSettings = new(AvatarSaveOption.SaveRemotelyAndExit);
        private static bool _hasInitializedPersistentSettings = false;

        private readonly HashSet<Ref<Sprite>> _spritesGivenToUser = new();

        /// <summary>
        /// Gets the current persistent save settings, initializing with defaults if needed.
        /// These settings persist within the same play session due to static variable behavior.
        /// </summary>
        private static AvatarSaveSettings GetPersistentSaveSettings()
        {
            if (!_hasInitializedPersistentSettings)
            {
                _persistentSaveSettings = new AvatarSaveSettings(AvatarSaveOption.SaveRemotelyAndContinue);
                _hasInitializedPersistentSettings = true;
            }

            return _persistentSaveSettings;
        }

        /// <summary>
        /// Opens the avatar editor with the specified avatar and camera.
        /// If camera is null, attempts to get the camera with tag 'MainCamera' (Camera.main).
        /// </summary>
        public async UniTask OpenEditorAsync(GeniesAvatar avatar, Camera camera = null)
        {
            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You need to be logged in to initialize the avatar editor");
                return;
            }

            if (_editorOpenedSource != null)
            {
                await _editorOpenedSource.Task;
                return;
            }

            lock (_editorOpenedLock)
            {
                if (_editorOpenedSource == null)
                {
                    _editorOpenedSource = new UniTaskCompletionSource();
                }
            }

            try
            {
                if (avatar == null)
                {
                    throw new NullReferenceException("An avatar is required in order to open the editor.");
                }

                if (camera == null)
                {
                    camera = Camera.main;
                    if (camera == null)
                    {
                        throw new NullReferenceException("A valid camera must be passed or a camera with tag 'MainCamera' must exist in the scene.");
                    }
                }

                if (_avatarEditorInstance != null)
                {
                    if (!_avatarEditorInstance.activeInHierarchy)
                    {
                        _avatarEditorInstance.SetActive(true);
                    }

                    _currentActiveAvatar = avatar;
                    await InitializeEditing(avatar, camera);
                    EditorOpened?.Invoke();
                    return;
                }

                if (_avatarEditorPrefab == null)
                {
                    _avatarEditorPrefab = Resources.Load<GameObject>(_avatarEditorPath);
                }

                if (_avatarEditorPrefab == null)
                {
                    CrashReporter.LogError($"AvatarEditor prefab not found at path: {_avatarEditorPath}");
                    return;
                }

                _currentActiveAvatar = avatar;
                _avatarEditorInstance = Object.Instantiate(_avatarEditorPrefab);

                await InitializeEditing(avatar, camera);

                EditorOpened?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to open avatar editor: {ex.Message}");
            }
            finally
            {
                FinishEditorOpenedSource();
            }
        }


        /// <summary>
        /// Opens the avatar editor with the specified camera and loads the current user's avatar.
        /// </summary>
        public async UniTask OpenEditorAsync(Camera camera)
        {
            await OpenEditorAsync(null, camera);
        }

        /// <summary>
        /// Closes the avatar editor and cleans up resources.
        /// </summary>
        public async UniTask CloseEditorAsync()
        {
            _currentActiveAvatar = null;

            if (_editorClosedSource != null)
            {
                await _editorClosedSource.Task;
                return;
            }

            lock (_editorClosedLock)
            {
                if (_editorClosedSource == null)
                {
                    _editorClosedSource = new UniTaskCompletionSource();
                }
            }

            if (_avatarEditorInstance == null)
            {
                return;
            }

            try
            {
                // - Return avatar definition to what it was before editing
                // - Return camera to previous position before editing
                var avatarEditingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
                if (avatarEditingScreen != null && avatarEditingScreen.EditingBehaviour is not null)
                {
                    await avatarEditingScreen.EditingBehaviour.DiscardAndExit();
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogWarning($"Exception caught while trying to clean up the avatar editor instance. This is usually ok since we're destroying the entire object instance anyway.\nException: {ex.Message}");
            }
            finally
            {
                if (_avatarEditorInstance != null)
                {
                    GameObject.Destroy(_avatarEditorInstance);
                    _avatarEditorInstance = null;
                }

                EditorClosed?.Invoke();
                FinishEditorClosedSource();

                // Clear any pending save settings when editor is closed (but keep persistent settings)
                _pendingSaveSettings = null;
            }
        }

        public void Dispose()
        {
            _ = CloseEditorAsync();
        }

        /// <summary>
        /// Gets the currently active avatar being edited in the editor.
        /// </summary>
        /// <returns>The currently active GeniesAvatar, or null if no avatar is currently being edited</returns>
        public GeniesAvatar GetCurrentActiveAvatar()
        {
            return _currentActiveAvatar;
        }

        public async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            try
            {
                if (string.IsNullOrEmpty(assetId))
                {
                    string error = "Asset id cannot be null or empty";
                    CrashReporter.LogError(error);
                    return (false, error);
                }

                if (!GeniesLoginSdk.IsUserSignedIn())
                {
                    string error = "You need to be logged in to give an asset to a user";
                    CrashReporter.LogError(error);
                    return (false, error);
                }

                var defaultInventoryService = ServiceManager.Get<IDefaultInventoryService>();
                return await defaultInventoryService.GiveAssetToUserAsync(assetId);
            }
            catch (Exception ex)
            {
                string error = $"Failed to give asset to user: {ex.Message}";
                CrashReporter.LogError(error);
                return (false, error);
            }
        }

        public async UniTask<List<WearableAssetInfo>> GetUsersAssetsAsync()
        {
            try
            {
                if (!GeniesLoginSdk.IsUserSignedIn())
                {
                    CrashReporter.LogError("You need to be logged in to get a user's assets");
                    return new();
                }

                var defaultInventoryService = ServiceManager.Get<IDefaultInventoryService>();
                var wearables = await defaultInventoryService.GetUserWearables();

                var provider = new InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.UserWearablesConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                var wearableAssetInfoList = await UniTask.WhenAll(
                    wearables.Select(async wearable =>
                    {
                        var data = await provider.GetDataForAssetId(wearable.AssetId);

                        var info = new WearableAssetInfo
                        {
                            AssetId = wearable.AssetId,
                            AssetType = wearable.AssetType,
                            Name = wearable.Name,
                            Category = wearable.Category,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return wearableAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get user assets: {ex.Message}");
                return new();
            }
        }

        /// <summary>
        /// Gets whether the editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public bool IsEditorOpen
        {
            get => _avatarEditorInstance != null && _avatarEditorInstance.activeInHierarchy;
        }

        /// <summary>
        /// Event raised when the editor is opened.
        /// </summary>
        public event Action EditorOpened;

        /// <summary>
        /// Event raised when the editor is closed.
        /// </summary>
        public event Action EditorClosed;

        /// <summary>
        /// Equips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to equip the asset on</param>
        /// <param name="wearableId">The ID of the wearable to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask EquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                // If avatar is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to equip an asset.");
                    return;
                }

                // If controller is already equipped with the asset, return
                if (avatar.Controller.IsAssetEquipped(wearableId))
                {
                    CrashReporter.LogWarning("Asset is already equipped.");
                    return;
                }

                // Get the list of default wearables from the inventory service
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                var defaultWearables = await defaultInventoryService.GetDefaultWearables();

                if (defaultWearables == null || !defaultWearables.Any())
                {
                    CrashReporter.LogError("No default wearables found in inventory service");
                    return;
                }

                // Verify that the wearable passed in has a match in Inventory
                var matchingWearable = defaultWearables.FirstOrDefault(w =>
                    w.AssetId.Equals(wearableId, StringComparison.OrdinalIgnoreCase));

                if (matchingWearable == null)
                {
                    CrashReporter.LogError($"No wearable found with ID: {wearableId}");
                    return;
                }

                // Create and execute the equip command
                var command = new EquipNativeAvatarAssetCommand(matchingWearable.AssetId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip outfit with ID '{wearableId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the asset from</param>
        /// <param name="wearableId">The ID of the wearable to unequip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask UnEquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to unequip an asset.");
                    return;
                }

                // If controller is not equipped with the asset, return
                if (!avatar.Controller.IsAssetEquipped(wearableId))
                {
                    CrashReporter.LogError("Asset is already not equipped.");
                    return;
                }

                // Create and execute the unequip command
                var command = new UnequipNativeAvatarAssetCommand(wearableId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip outfit with ID '{wearableId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Equips a skin color on the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to equip the skin color on</param>
        /// <param name="skinColor">The color to apply as skin color</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask SetSkinColorAsync(GeniesAvatar avatar, Color skinColor, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set a skin color.");
                    return;
                }

                // Create and execute the equip skin color command
                var command = new EquipSkinColorCommand(skinColor, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
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
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask EquipTattooAsync(GeniesAvatar avatar, string tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to equip a tattoo.");
                    return;
                }

                // If tattooId is null or empty, return
                if (string.IsNullOrEmpty(tattooId))
                {
                    CrashReporter.LogError("Tattoo ID cannot be null or empty");
                    return;
                }

                // Create and execute the equip tattoo command
                var command = new EquipNativeAvatarTattooCommand(tattooId, tattooSlot, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip tattoo with ID '{tattooId}' at slot '{tattooSlot}': {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips a tattoo from the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the tattoo from</param>
        /// <param name="tattooId">The ID of the tattoo to unequip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo is placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask UnEquipTattooAsync(GeniesAvatar avatar, string tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to unequip a tattoo.");
                    return;
                }

                // If tattooId is null or empty, return
                if (string.IsNullOrEmpty(tattooId))
                {
                    CrashReporter.LogError("Tattoo ID cannot be null or empty");
                    return;
                }

                // Create and execute the unequip tattoo command
                var command = new UnequipNativeAvatarTattooCommand(tattooSlot, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip tattoo with ID '{tattooId}' at slot '{tattooSlot}': {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the body preset for the specified controller.
        /// </summary>
        /// <param name="avaatr">The avatar to set the body preset on</param>
        /// <param name="preset">The GSkelModifierPreset to apply</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask SetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, GSkelModifierPreset preset, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set a body preset.");
                    return;
                }

                // If preset is null, return
                if (preset == null)
                {
                    CrashReporter.LogError("Body preset cannot be null");
                    return;
                }

                // Create and execute the set body preset command
                var command = new SetNativeAvatarBodyPresetCommand(preset, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
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
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask SetAvatarBodyTypeAsync(GeniesAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set a body type.");
                    return;
                }

                // Determine preset name based on gender and body size combination
                string presetName = GetPresetName(genderType, bodySize);

                // Load the corresponding preset from Resources
                var bodyPreset = AssetPath.Load<GSkelModifierPreset>(presetName);

                if (bodyPreset == null)
                {
                    CrashReporter.LogError($"Failed to load body preset: {presetName}");
                    return;
                }

                // Create and execute the set body preset command
                var command = new SetNativeAvatarBodyPresetCommand(bodyPreset, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar body type: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the preset name based on gender type and body size combination.
        /// </summary>
        /// <param name="genderType">The gender type</param>
        /// <param name="bodySize">The body size</param>
        /// <returns>The preset name string</returns>
        private string GetPresetName(GenderType genderType, BodySize bodySize)
        {
            return (genderType, bodySize) switch
            {
                (GenderType.Male, BodySize.Skinny) => "Resources/Body/gSkelModifierPresets/maleSkinny_gSkelModifierPreset",
                (GenderType.Male, BodySize.Medium) => "Resources/Body/gSkelModifierPresets/maleMedium_gSkelModifierPreset",
                (GenderType.Male, BodySize.Heavy) => "Resources/Body/gSkelModifierPresets/maleHeavy_gSkelModifierPreset",
                (GenderType.Female, BodySize.Skinny) => "Resources/Body/gSkelModifierPresets/femaleSkinny_gSkelModifierPreset",
                (GenderType.Female, BodySize.Medium) => "Resources/Body/gSkelModifierPresets/femaleMedium_gSkelModifierPreset",
                (GenderType.Female, BodySize.Heavy) => "Resources/Body/gSkelModifierPresets/femaleHeavy_gSkelModifierPreset",
                (GenderType.Androgynous, BodySize.Skinny) => "Resources/Body/gSkelModifierPresets/androgynousSkinny_gSkelModifierPreset",
                (GenderType.Androgynous, BodySize.Medium) => "Resources/Body/gSkelModifierPresets/androgynousMedium_gSkelModifierPreset",
                (GenderType.Androgynous, BodySize.Heavy) => "Resources/Body/gSkelModifierPresets/androgynousHeavy_gSkelModifierPreset",
                _ => throw new ArgumentException($"Invalid combination: {genderType}, {bodySize}")
            };
        }

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public async UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the list of default wearables from the inventory service
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                var defaultWearables = await defaultInventoryService.GetDefaultWearables();

                if (defaultWearables == null || !defaultWearables.Any())
                {
                    CrashReporter.LogError("No default wearables found in inventory service");
                    return new List<WearableAssetInfo>();
                }

                var provider = new InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.DefaultWearablesConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                var wearableAssetInfoList = await UniTask.WhenAll(
                    defaultWearables.Select(async wearable =>
                    {
                        var data = await provider.GetDataForAssetId(wearable.AssetId);

                        var info = new WearableAssetInfo
                        {
                            AssetId = wearable.AssetId,
                            AssetType = wearable.AssetType,
                            Name = wearable.Name,
                            Category = wearable.Category,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return wearableAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get wearable asset info list: {ex.Message}");
                return new List<WearableAssetInfo>();
            }
        }

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The avatar to save locally.</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public void SaveAvatarDefinitionLocally(GeniesAvatar avatar, string profileId)
        {
            try
            {
                var headshotPath = CapturePNG(avatar.Controller, profileId);
                LocalAvatarProcessor.SaveOrUpdate(profileId, avatar.Controller.GetDefinitionType(), headshotPath);

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
        public async UniTask SaveAvatarDefinitionAsync(GeniesAvatar avatar)
        {
            try
            {
                if (_avatarEditorInstance == null)
                {
                    CrashReporter.LogError("Avatar editor is not open. Cannot save avatar definition to cloud.");
                    return;
                }

                var avatarService = this.GetService<IAvatarService>();
                if (avatarService == null)
                {
                    CrashReporter.LogError("AvatarService not found. Cannot save avatar definition.");
                    return;
                }

                var avatarDefinition = avatar.Controller.GetDefinitionType();
                await avatarService.UpdateAvatarAsync(avatarDefinition);

            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition to cloud: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads an avatar from local definition using a profile ID and starts editing.
        /// </summary>
        /// <param name="profileId">The profile ID to load the avatar definition from</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public async UniTask<GeniesAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(profileId))
                {
                    CrashReporter.LogError("Profile ID cannot be null or empty");
                    return null;
                }

                // Load the avatar definition string from the profile ID
                var avatarDefinitionString = LocalAvatarProcessor.LoadFromJson(profileId);

                // Parse the avatar definition string into an AvatarDefinition object
                var avatarDefinition = avatarDefinitionString.Definition;

                if (avatarDefinition == null)
                {
                    CrashReporter.LogError($"Failed to parse avatar definition for profile ID: {profileId}");
                    return null;
                }

                // Return the avatar controller with the parsed definition
                return await GeniesAvatarsSdk.LoadAvatarControllerWithClassDefinition(avatarDefinition);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar definition from profile ID '{profileId}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads an avatar definition from gam object using profile ID and starts editing.
        /// </summary>
        /// <param name="profileId">The profile ID to load the avatar definition from</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public async UniTask<GeniesAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(profileId))
                {
                    CrashReporter.LogError("Profile ID cannot be null or empty");
                    return null;
                }

                // Load the avatar definition string from the profile ID
                var avatarDefinitionString = LocalAvatarProcessor.LoadFromResources(profileId, null);

                // Parse the avatar definition string into an AvatarDefinition object
                var avatarDefinition = avatarDefinitionString.Definition;

                if (avatarDefinition == null)
                {
                    CrashReporter.LogError($"Failed to parse avatar definition for profile ID: {profileId}");
                    return null;
                }

                // Return the avatar controller with the parsed definition
                return await GeniesAvatarsSdk.LoadAvatarControllerWithClassDefinition(avatarDefinition);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar definition from profile ID '{profileId}': {ex.Message}");
            }

            return null;
        }

        public async UniTask UploadAvatarImageAsync(byte[] imageData, string avatarId)
        {
            try
            {
                if (_avatarEditorInstance == null)
                {
                    CrashReporter.LogError("Avatar editor is not open. Cannot save avatar definition to cloud.");
                    return;
                }

                var avatarService = this.GetService<IAvatarService>();
                if (avatarService == null)
                {
                    CrashReporter.LogError("AvatarService not found. Cannot save avatar definition.");
                    return;
                }

                await avatarService.UploadAvatarImageAsync(imageData, avatarId);

            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition to cloud: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the save option for the avatar editor.
        /// Can be called before or after the editor is opened.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption)
        {
            SetEditorSaveSettings(new AvatarSaveSettings(saveOption));
        }

        /// <summary>
        /// Sets the save option and profile ID for the avatar editor.
        /// Can be called before or after the editor is opened.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        /// <param name="profileId">The profile ID to use when saving locally</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption, string profileId)
        {
            SetEditorSaveSettings(new AvatarSaveSettings(saveOption, profileId));
        }

        /// <summary>
        /// Sets the save settings for the avatar editor.
        /// These settings persist across multiple editor sessions within the same play session.
        /// Settings are automatically reset when play mode exits
        /// Can be called before or after the editor is opened.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public void SetEditorSaveSettings(AvatarSaveSettings saveSettings)
        {
            // Store settings both for immediate use and persistent session storage
            _pendingSaveSettings = saveSettings;
            _persistentSaveSettings = saveSettings;
            _hasInitializedPersistentSettings = true;

            // If editor is already open, apply the save settings immediately
            if (_avatarEditorInstance != null)
            {
                ApplySaveSettings(saveSettings);
            }
        }

        #region Helpers

        /// <summary>
        /// Applies the save settings to the avatar editing screen if the editor is currently open.
        /// If the editor is not open, the settings will be applied when the editor is initialized.
        /// </summary>
        /// <param name="saveSettings">The save settings to apply</param>
        private void ApplySaveSettings(AvatarSaveSettings saveSettings)
        {
            // If the editor is not currently open, the settings are already stored in _pendingSaveSettings
            // and will be applied when the editor is opened in InitializeEditing()
            if (_avatarEditorInstance == null)
            {
                return; // This is not an error - the editor is simply not open yet
            }

            var avatarEditingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
            if (avatarEditingScreen == null)
            {
                CrashReporter.LogError("AvatarEditingScreen not found. Cannot apply save settings to open editor.");
                return;
            }

            avatarEditingScreen.SetSaveSettings(saveSettings);
        }

        private async UniTask InitializeEditing(GeniesAvatar avatar, Camera camera)
        {
            var virtualCameraManager = _avatarEditorInstance.GetComponentInChildren<VirtualCameraManager>();
            Assert.IsNotNull(virtualCameraManager);

            _avatarEditorInstance.transform.SetPositionAndRotation(avatar.Root.transform.position, avatar.Root.transform.rotation);

            virtualCameraManager.Activate(camera);

            var editingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
            Assert.IsNotNull(editingScreen);
            await editingScreen.Initialize(avatar, camera, virtualCameraManager);

            // Apply save settings after initialization - use pending first, then persistent, then default
            AvatarSaveSettings settingsToApply;
            if (_pendingSaveSettings.HasValue)
            {
                settingsToApply = _pendingSaveSettings.Value;
            }
            else
            {
                settingsToApply = GetPersistentSaveSettings();
            }

            ApplySaveSettings(settingsToApply);
        }

        private static void FinishEditorOpenedSource()
        {
            var source = _editorOpenedSource;
            source?.TrySetResult();
            _editorOpenedSource = null;
        }

        private static void FinishEditorClosedSource()
        {
            var source = _editorClosedSource;
            _editorClosedSource = null;
            source?.TrySetResult();
        }

        private void KeepSpriteReference(Ref<Sprite> spriteRef)
        {
            _spritesGivenToUser.Add(spriteRef);
        }

        public void RemoveSpriteReference(Ref<Sprite> spriteRef)
        {
            spriteRef.Dispose();

            if (_spritesGivenToUser.Contains(spriteRef))
            {
                _spritesGivenToUser.Remove(spriteRef);
            }
        }

        private string CapturePNG(NativeUnifiedGenieController currentCustomizedAvatar, string profileId = null)
        {
            // Use profile ID in filename if provided, otherwise use default
            var filename = string.IsNullOrEmpty(profileId) ? "avatar-headshot.png" : $"{profileId}-headshot.png";

            // Ensure the headshot directory exists
            if (!System.IO.Directory.Exists(LocalAvatarProcessor.HeadshotPath))
            {
                System.IO.Directory.CreateDirectory(LocalAvatarProcessor.HeadshotPath);
            }

            var headShotPath = System.IO.Path.Combine(LocalAvatarProcessor.HeadshotPath, filename);
            GameObject genieRoot = currentCustomizedAvatar.Genie.Root;
            var head = genieRoot.transform.Find(_headTransformPath);

            AvatarPngCapture.CaptureHeadshotPNG(genieRoot, head,
                width: 512,
                height: 512,
                savePath: headShotPath, // writes the file here
                transparentBackground: true,
                msaa: 8,
                fieldOfView: 25f,
                headRadiusMeters: 0.23f, // tweak per your scale
                forwardDistance: 0.8f, // how tight you want it before FOV fit
                cameraUpOffset: new Vector3(0f, 0.05f, 0f));

            return headShotPath;
        }

        #endregion
    }
}
