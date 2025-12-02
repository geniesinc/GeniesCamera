using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.Naf;
using GnWrappers;
using UnityEngine;

namespace Genies.Sdk
{
    /// <summary>
    /// SDK wrapper class for GeniesAvatar that provides a simplified interface for avatar manipulation.
    /// This wrapper redirects all method calls to the underlying managed GeniesAvatar instance.
    /// </summary>
    public class ManagedAvatar
    {
        private GeniesAvatar Avatar { get; }

        /// <summary>
        /// Gets whether this ManagedAvatar has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The MonoBehaviour component attached to the avatar's root GameObject.
        /// Provides a bridge between Unity's GameObject system and this ManagedAvatar wrapper.
        /// </summary>
        public ManagedAvatarComponent Component { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ManagedAvatar class with the specified GeniesAvatar.
        /// </summary>
        /// <param name="avatar">The GeniesAvatar instance to wrap. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when avatar parameter is null.</exception>
        internal ManagedAvatar(GeniesAvatar avatar)
        {
            Avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));

            // Attach the ManagedAvatarComponent to the root GameObject and establish bidirectional references
            if (Avatar.Root != null)
            {
                Component = Avatar.Root.AddComponent<ManagedAvatarComponent>();
                Component.ManagedAvatar = this;
            }
        }

        #region Identity Properties

        /// <summary>Species ID of this avatar (from IGenie).</summary>
        public string Species => Avatar?.Species;

        /// <summary>Current LOD of this avatar (from IGenie).</summary>
        public string Lod => Avatar?.Lod;

        /// <summary>Root GameObject holding the avatar hierarchy.</summary>
        public GameObject Root => Avatar?.Root;

        /// <summary>Sub-root GameObject where the visual model is parented.</summary>
        public GameObject ModelRoot => Avatar?.ModelRoot;

        /// <summary>Skeleton root transform.</summary>
        public Transform SkeletonRoot => Avatar?.SkeletonRoot;

        /// <summary>Animator bound to the avatar rig.</summary>
        public Animator Animator => Avatar?.Animator;

        /// <summary>For advanced scenarios where direct access is needed.</summary>
        internal NativeUnifiedGenieController Controller => Avatar?.Controller;

        /// <summary>Gets the underlying GeniesAvatar instance.</summary>
        internal GeniesAvatar GeniesAvatar => Avatar;

        #endregion

        #region Animator Control

        /// <summary>
        /// Sets the runtime animator controller for this avatar's Animator component.
        /// This allows customization of the avatar's animation behaviors and state machine.
        /// </summary>
        /// <param name="playerAnimationController">The RuntimeAnimatorController to apply to the avatar. Can be null to clear the current controller.</param>
        public void SetAnimatorController(RuntimeAnimatorController playerAnimationController)
        {
            Avatar?.SetAnimatorController(playerAnimationController);
        }

        #endregion

        #region Assets (single & batch)

        /// <summary>
        /// Equips a specific asset (clothing, accessory, etc.) to the avatar asynchronously.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to equip.</param>
        /// <param name="parameters">Optional parameters for asset configuration (e.g., colors, materials, variants).</param>
        /// <returns>A UniTask that completes when the asset has been equipped.</returns>
        public UniTask EquipAssetAsync(string assetId, Dictionary<string, string> parameters = null)
        {
            return Avatar?.EquipAssetAsync(assetId, parameters) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Removes a specific asset from the avatar asynchronously.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to unequip.</param>
        /// <returns>A UniTask that completes when the asset has been removed.</returns>
        public UniTask UnequipAssetAsync(string assetId)
        {
            return Avatar?.UnequipAssetAsync(assetId) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Equips multiple assets to the avatar in a single batch operation asynchronously.
        /// This is more efficient than calling EquipAssetAsync multiple times.
        /// </summary>
        /// <param name="assets">Collection of tuples containing asset IDs and their configuration parameters.</param>
        /// <returns>A UniTask that completes when all assets have been equipped.</returns>
        public UniTask EquipAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets)
        {
            return Avatar?.EquipAssetsAsync(assets) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Removes multiple assets from the avatar in a single batch operation asynchronously.
        /// This is more efficient than calling UnequipAssetAsync multiple times.
        /// </summary>
        /// <param name="assetIds">Collection of asset IDs to unequip.</param>
        /// <returns>A UniTask that completes when all specified assets have been removed.</returns>
        public UniTask UnequipAssetsAsync(IEnumerable<string> assetIds)
        {
            return Avatar?.UnequipAssetsAsync(assetIds) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Sets the complete set of equipped assets for the avatar, replacing any currently equipped assets.
        /// This is equivalent to unequipping all current assets and then equipping the specified ones.
        /// </summary>
        /// <param name="assets">Collection of tuples containing asset IDs and their configuration parameters that should be equipped.</param>
        /// <returns>A UniTask that completes when the avatar's asset configuration has been updated.</returns>
        public UniTask SetEquippedAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets)
        {
            return Avatar?.SetEquippedAssetsAsync(assets) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Checks whether a specific asset is currently equipped on the avatar.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to check.</param>
        /// <returns>True if the asset is currently equipped, false otherwise.</returns>
        public bool IsAssetEquipped(string assetId)
        {
            return Avatar?.IsAssetEquipped(assetId) ?? false;
        }

        /// <summary>
        /// Adds the IDs of all currently equipped assets to the provided collection.
        /// This is useful for building lists without creating new collections.
        /// </summary>
        /// <param name="results">The collection to add the equipped asset IDs to.</param>
        public void AddEquippedAssetIds(ICollection<string> results)
        {
            Avatar?.AddEquippedAssetIds(results);
        }

        /// <summary>
        /// Gets a list containing the IDs of all currently equipped assets on the avatar.
        /// </summary>
        /// <returns>A new List containing all equipped asset IDs.</returns>
        public List<string> GetEquippedAssetIds()
        {
            return Avatar?.GetEquippedAssetIds() ?? new List<string>();
        }

        #endregion

        #region Colors

        /// <summary>
        /// Sets a specific color property on the avatar asynchronously (e.g., hair color, skin tone, clothing color).
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to modify.</param>
        /// <param name="color">The Color value to apply to the specified property.</param>
        /// <returns>A UniTask that completes when the color has been applied to the avatar.</returns>
        public UniTask SetColorAsync(string colorId, Color color)
        {
            return Avatar?.SetColorAsync(colorId, color) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Sets multiple color properties on the avatar in a single batch operation asynchronously.
        /// This is more efficient than calling SetColorAsync multiple times.
        /// </summary>
        /// <param name="colors">Collection of GenieColorEntry objects containing color IDs and their corresponding color values.</param>
        /// <returns>A UniTask that completes when all colors have been applied to the avatar.</returns>
        public UniTask SetColorsAsync(IEnumerable<GenieColorEntry> colors)
        {
            return Avatar?.SetColorsAsync(colors) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Gets the current color value for a specific color property on the avatar.
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to retrieve.</param>
        /// <returns>The current Color value if the property exists and has a color set, null otherwise.</returns>
        public Color? GetColor(string colorId)
        {
            return Avatar?.GetColor(colorId);
        }

        /// <summary>
        /// Removes/resets a specific color property on the avatar asynchronously, reverting it to its default state.
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to reset.</param>
        /// <returns>A UniTask that completes when the color property has been reset to its default state.</returns>
        public UniTask UnsetColorAsync(string colorId)
        {
            return Avatar?.UnsetColorAsync(colorId) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Removes/resets all color customizations on the avatar asynchronously, reverting all colors to their default states.
        /// </summary>
        /// <returns>A UniTask that completes when all color properties have been reset to their default states.</returns>
        public UniTask UnsetAllColorsAsync()
        {
            return Avatar?.UnsetAllColorsAsync() ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Checks whether a specific color property is available for customization on this avatar.
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to check availability for.</param>
        /// <returns>True if the color property exists and can be customized, false otherwise.</returns>
        public bool IsColorAvailable(string colorId)
        {
            return Avatar?.IsColorAvailable(colorId) ?? false;
        }

        #endregion

        #region Body attributes & presets

        /// <summary>
        /// Sets a specific body attribute (e.g., muscle mass, height, body proportions) with a given weight value.
        /// </summary>
        /// <param name="attributeId">The unique identifier of the body attribute to modify.</param>
        /// <param name="weight">The weight/strength of the attribute, typically ranging from 0.0 to 1.0.</param>
        public void SetBodyAttribute(string attributeId, float weight)
        {
            Avatar?.SetBodyAttribute(attributeId, weight);
        }

        /// <summary>
        /// Gets the current weight value for a specific body attribute on the avatar.
        /// </summary>
        /// <param name="attributeId">The unique identifier of the body attribute to retrieve.</param>
        /// <returns>The current weight/strength value of the specified body attribute.</returns>
        public float GetBodyAttribute(string attributeId)
        {
            return Avatar?.GetBodyAttribute(attributeId) ?? 0f;
        }

        /// <summary>
        /// Applies a predefined body attributes preset to the avatar, which sets multiple body attributes to specific values.
        /// </summary>
        /// <param name="preset">The BodyAttributesPreset to apply, containing a collection of body attribute configurations.</param>
        internal void SetBodyPreset(BodyAttributesPreset preset)
        {
            Avatar?.SetBodyPreset(preset);
        }

        /// <summary>
        /// Resets all body attributes on the avatar to their default values.
        /// </summary>
        public void ResetAllBodyAttributes()
        {
            Avatar?.ResetAllBodyAttributes();
        }

        /// <summary>
        /// Checks whether a specific body attribute is available for customization on this avatar.
        /// </summary>
        /// <param name="attributeId">The unique identifier of the body attribute to check availability for.</param>
        /// <returns>True if the body attribute exists and can be customized, false otherwise.</returns>
        public bool IsBodyAttributeAvailable(string attributeId)
        {
            return Avatar?.IsBodyAttributeAvailable(attributeId) ?? false;
        }

        /// <summary>
        /// Sets a body preset using the legacy GSkelModifierPreset format.
        /// This method is deprecated - use SetBodyPreset(BodyAttributesPreset) instead for new implementations.
        /// </summary>
        /// <param name="preset">The legacy GSkelModifierPreset to apply.</param>
        /// <returns>A UniTask that completes when the body preset has been applied.</returns>
        /// <remarks>This method is maintained for backwards compatibility and may be removed in future versions.</remarks>
        internal UniTask SetBodyPresetAsync(GSkelModifierPreset preset)
        {
            return Avatar?.SetBodyPresetAsync(preset) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Gets the current body preset in the legacy GSkelModifierPreset format.
        /// This method is deprecated - consider using individual body attribute methods instead.
        /// </summary>
        /// <returns>The current GSkelModifierPreset representing the avatar's body configuration.</returns>
        /// <remarks>This method is maintained for backwards compatibility and may be removed in future versions.</remarks>
        internal GSkelModifierPreset GetBodyPreset()
        {
            return Avatar?.GetBodyPreset();
        }

        /// <summary>
        /// Gets the current body variation identifier for the avatar.
        /// Body variations represent different base body types or configurations.
        /// </summary>
        /// <returns>A string identifier representing the current body variation.</returns>
        public string GetBodyVariation()
        {
            return Avatar?.GetBodyVariation();
        }

        #endregion

        #region Tattoos

        /// <summary>
        /// Equips a tattoo asset to a specific slot on the avatar's skin asynchronously.
        /// </summary>
        /// <param name="slot">The specific tattoo slot/location on the avatar's body where the tattoo should be applied.</param>
        /// <param name="assetId">The unique identifier of the tattoo asset to equip.</param>
        /// <param name="parameters">Optional parameters for tattoo configuration (e.g., colors, opacity, positioning).</param>
        /// <returns>A UniTask that completes when the tattoo has been applied to the avatar.</returns>
        public UniTask EquipTattooAsync(MegaSkinTattooSlot slot, string assetId, Dictionary<string, string> parameters = null)
        {
            return Avatar?.EquipTattooAsync(slot, assetId, parameters) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Removes a tattoo from a specific slot on the avatar's skin asynchronously.
        /// </summary>
        /// <param name="slot">The specific tattoo slot/location from which to remove the tattoo.</param>
        /// <returns>A UniTask that completes when the tattoo has been removed from the specified slot.</returns>
        public UniTask UnequipTattooAsync(MegaSkinTattooSlot slot)
        {
            return Avatar?.UnequipTattooAsync(slot) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Removes all tattoos from the avatar asynchronously.
        /// </summary>
        /// <returns>A UniTask that completes when all tattoos have been removed from the avatar.</returns>
        public UniTask UnequipAllTattoosAsync()
        {
            return Avatar?.UnequipAllTattoosAsync() ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// Checks whether a specific tattoo asset is currently equipped in a specific slot on the avatar.
        /// </summary>
        /// <param name="slot">The tattoo slot to check.</param>
        /// <param name="assetId">The unique identifier of the tattoo asset to check for.</param>
        /// <returns>True if the specified tattoo is equipped in the specified slot, false otherwise.</returns>
        public bool IsTattooEquipped(MegaSkinTattooSlot slot, string assetId)
        {
            return Avatar?.IsTattooEquipped(slot, assetId) ?? false;
        }

        /// <summary>
        /// Gets the asset ID of the tattoo currently equipped in a specific slot on the avatar.
        /// </summary>
        /// <param name="slot">The tattoo slot to query.</param>
        /// <returns>The asset ID of the equipped tattoo in the specified slot, or null if no tattoo is equipped.</returns>
        public string GetEquippedTattoo(MegaSkinTattooSlot slot)
        {
            return Avatar?.GetEquippedTattoo(slot);
        }

        #endregion

        #region Definition import/export

        /// <summary>
        /// Exports the current avatar configuration as a JSON definition string.
        /// This definition includes all equipped assets, colors, body attributes, and tattoos.
        /// </summary>
        /// <returns>A JSON string containing the complete avatar definition.</returns>
        public string GetDefinition()
        {
            return Avatar?.GetDefinition();
        }

        /// <summary>
        /// Applies a complete avatar configuration from a JSON definition string asynchronously.
        /// This will update all aspects of the avatar including assets, colors, body attributes, and tattoos.
        /// </summary>
        /// <param name="definition">A JSON string containing the avatar definition to apply.</param>
        /// <returns>A UniTask that completes when the avatar has been updated with the new definition.</returns>
        public UniTask SetDefinitionAsync(string definition)
        {
            return Avatar?.SetDefinitionAsync(definition) ?? UniTask.CompletedTask;
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Disposes native resources via controller and (optionally) caller should also destroy the GameObject hosting this avatar if needed.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            Avatar?.Dispose();

            // Clean up the component reference before disposing the avatar
            if (Component != null)
            {
                Component.ManagedAvatar = null;

                if (Application.isPlaying)
                {
                    GameObject.Destroy(Component.gameObject);
                }
                else
                {
                    GameObject.DestroyImmediate(Component.gameObject);
                }
                Component = null;
            }
        }

        #endregion
    }
}
