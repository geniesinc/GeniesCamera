using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.AvatarEditor.Core;
using UnityEngine;

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Provides events for SDK notifications.
        /// </summary>
        public static partial class Events
        {
            /// <summary>
            /// Event raised when the avatar editor is opened.
            /// </summary>
            public static event Action AvatarEditorOpened
            {
                add => AvatarEditorSDK.EditorOpened += value;
                remove => AvatarEditorSDK.EditorOpened -= value;
            }

            /// <summary>
            /// Event raised when the avatar editor is closed.
            /// </summary>
            public static event Action AvatarEditorClosed
            {
                add => AvatarEditorSDK.EditorClosed += value;
                remove => AvatarEditorSDK.EditorClosed -= value;
            }
        }

        /// <summary>
        /// Gets whether the avatar editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public static bool IsAvatarEditorOpen => AvatarEditorSDK.IsEditorOpen;

        /// <summary>
        /// Opens the Avatar Editor with the specified avatar and camera.
        /// </summary>
        /// <param name="avatar">The avatar to edit. If null, loads the current user's avatar.</param>
        /// <param name="camera">The camera to use for the editor. If null, uses Camera.main.</param>
        /// <returns>A UniTask that completes when the editor is opened.</returns>
        public static async UniTask OpenAvatarEditorAsync(ManagedAvatar avatar, Camera camera = null)
        {
            var geniesAvatar = avatar?.GeniesAvatar;
            if (geniesAvatar is null)
            {
                Debug.LogWarning("The Avatar Editor requires a valid Avatar instance to be opened.");
                return;
            }

            await AvatarEditorSDK.OpenEditorAsync(geniesAvatar, camera);
        }

        /// <summary>
        /// Closes the Avatar Editor and cleans up resources.
        /// </summary>
        /// <returns>A UniTask that completes when the editor is closed.</returns>
        public static async UniTask CloseAvatarEditorAsync() => await AvatarEditorSDK.CloseEditorAsync();

        /// <summary>
        /// Gets the active avatar being edited in the Avatar Editor.
        /// </summary>
        /// <returns>The currently active ManagedAvatar, or null if no avatar is currently being edited.</returns>
        public static ManagedAvatar GetAvatarEditorAvatar()
        {
            var geniesAvatar = AvatarEditorSDK.GetCurrentActiveAvatar();
            return geniesAvatar != null ? new ManagedAvatar(geniesAvatar) : null;
        }

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category.</returns>
        public static async UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default)
        {
            var internalList = await AvatarEditorSDK.GetWearableAssetInfoListAsync(cancellationToken);
            return WearableAssetInfo.FromInternalList(internalList);
        }

        /// <summary>
        /// Equips an outfit by wearable ID on the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the outfit on.</param>
        /// <param name="wearableId">The ID of the wearable to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipOutfitAsync(ManagedAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.EquipOutfitAsync(avatar.GeniesAvatar, wearableId, cancellationToken);
            }
        }

        /// <summary>
        /// Unequips an outfit by wearable ID from the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the outfit from.</param>
        /// <param name="wearableId">The ID of the wearable to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask UnEquipOutfitAsync(ManagedAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.UnEquipOutfitAsync(avatar.GeniesAvatar, wearableId, cancellationToken);
            }
        }

        /// <summary>
        /// Sets a skin color on the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to set the skin color on.</param>
        /// <param name="skinColor">The color to apply as skin color.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetSkinColorAsync(ManagedAvatar avatar, Color skinColor, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.SetSkinColorAsync(avatar.GeniesAvatar, skinColor, cancellationToken);
            }
        }

        /// <summary>
        /// Gets the assets of the logged in user.
        /// </summary>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category.</returns>
        public static async UniTask<List<WearableAssetInfo>> GetUsersAssetsAsync()
        {
            var internalList = await AvatarEditorSDK.GetUsersAssetsAsync();
            return WearableAssetInfo.FromInternalList(internalList);
        }

        /// <summary>
        /// Grants an asset to a user, adding it to their inventory.
        /// </summary>
        /// <param name="assetId">Id of the asset.</param>
        /// <returns>A UniTask representing the async operation with a bool indicating success status.</returns>
        public static async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId) =>
            await AvatarEditorSDK.GiveAssetToUserAsync(assetId);

        /// <summary>
        /// Sets the avatar body type with specified gender and body size.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to set the body type on.</param>
        /// <param name="genderType">The gender type (Male, Female, Androgynous).</param>
        /// <param name="bodySize">The body size (Skinny, Medium, Heavy).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetAvatarBodyTypeAsync(ManagedAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.SetAvatarBodyTypeAsync(avatar.GeniesAvatar, EnumMapper.ToInternal(genderType), EnumMapper.ToInternal(bodySize), cancellationToken);
            }
        }

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to save.</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public static async UniTask SaveAvatarDefinitionLocallyAsync(ManagedAvatar avatar, string profileId = null)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.SaveAvatarDefinitionLocallyAsync(avatar.GeniesAvatar, profileId);
            }
        }

        /// <summary>
        /// Loads an avatar definition from a string and returns a ManagedAvatar.
        /// </summary>
        /// <param name="profileId">The profile to load.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask that completes with the loaded ManagedAvatar, or null if failed.</returns>
        public static async UniTask<ManagedAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default)
        {
            var geniesAvatar = await AvatarEditorSDK.LoadFromLocalAvatarDefinitionAsync(profileId, cancellationToken);
            return geniesAvatar != null ? new ManagedAvatar(geniesAvatar) : null;
        }

        /// <summary>
        /// Loads an avatar from a local GameObject and returns a ManagedAvatar.
        /// </summary>
        /// <param name="profileId">The profile to load.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask that completes with the loaded ManagedAvatar, or null if failed.</returns>
        public static async UniTask<ManagedAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default)
        {
            var geniesAvatar = await AvatarEditorSDK.LoadFromLocalGameObjectAsync(profileId, cancellationToken);
            return geniesAvatar != null ? new ManagedAvatar(geniesAvatar) : null;
        }

        /// <summary>
        /// Sets the avatar editor to save locally and continue editing.
        /// </summary>
        /// <param name="profileId">The profile ID to use when saving locally. If null, uses the default template name.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveLocallyAndContinueAsync(string profileId) =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveLocallyAndContinue, profileId);

        /// <summary>
        /// Sets the avatar editor to save locally and exit the editor.
        /// </summary>
        /// <param name="profileId">The profile ID to use when saving locally. If null, uses the default template name.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveLocallyAndExitAsync(string profileId) =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveLocallyAndExit, profileId);

        /// <summary>
        /// Sets the avatar editor to save to the cloud and continue editing.
        /// </summary>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveRemotelyAndContinueAsync() =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveRemotelyAndContinue);

        /// <summary>
        /// Sets the avatar editor to save to the cloud and exit the editor.
        /// </summary>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveRemotelyAndExitAsync() =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveRemotelyAndExit);
    }
}
