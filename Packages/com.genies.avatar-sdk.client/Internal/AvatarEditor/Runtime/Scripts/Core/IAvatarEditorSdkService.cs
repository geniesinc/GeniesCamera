using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.Inventory;
using Genies.Naf;
using Genies.Refs;
using Genies.ServiceManagement;
using GnWrappers;
using UnityEngine;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Struct containing basic wearable asset information.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class WearableAssetInfo : IDisposable
#else
    public class WearableAssetInfo : IDisposable
#endif
    {
        public string AssetId { get; set; }
        public AssetType AssetType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Ref<Sprite> Icon { get; set; }

        public void Dispose()
        {
            ServiceManager.Get<IAvatarEditorSdkService>()?.RemoveSpriteReference(Icon);
        }
    }

    /// <summary>
    /// Core service interface for managing the Avatar Editor.
    /// Provides methods for opening and closing the avatar editor with customizable properties.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAvatarEditorSdkService
#else
    public interface IAvatarEditorSdkService
#endif
    {
        /// <summary>
        /// Opens the avatar editor with the specified avatar and camera.
        /// </summary>
        /// <param name="avatar">The avatar to edit. If null, loads the current user's avatar.</param>
        /// <param name="camera">The camera to use for the editor. If null, uses Camera.main.</param>
        /// <returns>A UniTask that completes when the editor is opened.</returns>
        public UniTask OpenEditorAsync(GeniesAvatar avatar, Camera camera = null);

        /// <summary>
        /// Closes the avatar editor and cleans up resources.
        /// </summary>
        /// <returns>A UniTask that completes when the editor is closed.</returns>
        public UniTask CloseEditorAsync();

        /// <summary>
        /// Gets the currently active avatar being edited in the editor.
        /// </summary>
        /// <returns>The currently active GeniesAvatar, or null if no avatar is currently being edited</returns>
        public GeniesAvatar GetCurrentActiveAvatar();

        /// <summary>
        /// Gets whether the editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public bool IsEditorOpen { get; }

        /// <summary>
        /// Event raised when the editor is opened.
        /// </summary>
        public event Action EditorOpened;

        /// <summary>
        /// Event raised when the editor is closed.
        /// </summary>
        public event Action EditorClosed;

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the assets of the logged in user
        /// </summary>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public UniTask<List<WearableAssetInfo>> GetUsersAssetsAsync();

        /// <summary>
        /// Grants an asset to a user, adding it to their inventory
        /// </summary>
        /// <param name="assetId">Id of the asset</param>
        /// <returns>UniTask representing the async operation with a bool indicating success status
        /// and string for any failure reason</returns>
        public UniTask<(bool, string)> GiveAssetToUserAsync(string assetId);

        /// <summary>
        /// Equips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to equip the asset on</param>
        /// <param name="wearableId">The ID of the wearable to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask EquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unequips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the asset from</param>
        /// <param name="wearableId">The ID of the wearable to unequip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask UnEquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips a skin color on the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to equip the skin color on</param>
        /// <param name="skinColor">The color to apply as skin color</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask SetSkinColorAsync(GeniesAvatar avatar, Color skinColor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips a tattoo on the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to equip the tattoo on</param>
        /// <param name="tattooId">The ID of the tattoo to equip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo should be placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask EquipTattooAsync(GeniesAvatar avatar, string tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unequips a tattoo from the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the tattoo from</param>
        /// <param name="tattooId">The ID of the tattoo to unequip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo is placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask UnEquipTattooAsync(GeniesAvatar avatar, string tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the body preset for the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to set the body preset on</param>
        /// <param name="preset">The GSkelModifierPreset to apply</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask SetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, GSkelModifierPreset preset, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the avatar body type with specified gender and body size.
        /// </summary>
        /// <param name="avatar">The avatar to set the body type on</param>
        /// <param name="genderType">The gender type (Male, Female, Androgynous)</param>
        /// <param name="bodySize">The body size (Skinny, Medium, Heavy)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask SetAvatarBodyTypeAsync(GeniesAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the current avatar definition to the cloud.
        /// </summary>
        /// <returns>A UniTask that completes when the save operation is finished.</returns>
        public UniTask SaveAvatarDefinitionAsync(GeniesAvatar avatar);

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The avatar to save</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public void SaveAvatarDefinitionLocally(GeniesAvatar avatar, string profileId = null);

        /// <summary>
        /// Loads an avatar definition from a string and starts editing with it.
        /// </summary>
        /// <param name="profileId">The profile to load</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public UniTask<GeniesAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads an avatar definition from local game object and starts editing with it.
        /// </summary>
        /// <param name="profileId">The profile to load</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public UniTask<GeniesAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads an avatar image for the specified avatar.
        /// </summary>
        /// <param name="imageData">The image data as a byte array.</param>
        /// <param name="avatarId">The ID of the avatar to upload the image for.</param>
        /// <returns>A task that completes with the URL of the uploaded image.</returns>
        public UniTask UploadAvatarImageAsync(byte[] imageData, string avatarId);

        /// <summary>
        /// Sets the save option for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption);

        /// <summary>
        /// Sets the save option and profile ID for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        /// <param name="profileId">The profile ID to use when saving locally</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption, string profileId);

        /// <summary>
        /// Sets the save settings for the avatar editor.
        /// Settings persist across multiple editor sessions within the same play session.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public void SetEditorSaveSettings(AvatarSaveSettings saveSettings);

        /// <summary>
        /// Remove a sprite from an internal managed cache so it can be garbage collected
        /// </summary>
        /// <param name="spriteRef">The ref to the sprite</param>
        public void RemoveSpriteReference(Ref<Sprite> spriteRef);

    }
}
