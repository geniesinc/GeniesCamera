using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Naf;
using GnWrappers;
using UnityEngine;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Thin wrapper over <see cref="NativeUnifiedGenieController"/> that also surfaces the high-level IGenie handles.
    /// Provides a unified interface for avatar manipulation including equipment, colors, body attributes, and tattoos.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAvatar
#else
    public class GeniesAvatar
#endif
    {
        private readonly IGenie _genie;
        private readonly NativeUnifiedGenieController _controller;

#region Construction / Identity

        /// <summary>
        /// Initializes a new instance of the GeniesAvatar class with the specified IGenie and optional controller.
        /// </summary>
        /// <param name="genie">The IGenie instance representing the avatar. Cannot be null.</param>
        /// <param name="controller">Optional NativeUnifiedGenieController for advanced avatar operations. If null, some operations may not be available.</param>
        /// <exception cref="ArgumentNullException">Thrown when genie parameter is null.</exception>
        public GeniesAvatar(IGenie genie, NativeUnifiedGenieController controller = null)
        {
            _genie = genie ?? throw new ArgumentNullException(nameof(genie));
            _controller = controller;
        }

        /// <summary>Species ID of this avatar (from IGenie).</summary>
        public string Species => _genie.Species;

        /// <summary>Current LOD of this avatar (from IGenie).</summary>
        public string Lod => _genie.Lod;

        /// <summary>Root GameObject holding the avatar hierarchy.</summary>
        public GameObject Root => _genie.Root;

        /// <summary>Sub-root GameObject where the visual model is parented.</summary>
        public GameObject ModelRoot => _genie.ModelRoot;

        /// <summary>Skeleton root transform.</summary>
        public Transform SkeletonRoot => _genie.SkeletonRoot;

        /// <summary>Animator bound to the avatar rig.</summary>
        public Animator Animator => _genie.Animator;

        /// <summary>For advanced scenarios where direct access is needed.</summary>
        public NativeUnifiedGenieController Controller => _controller;

        /// <summary>
        /// Sets the runtime animator controller for this avatar's Animator component.
        /// This allows customization of the avatar's animation behaviors and state machine.
        /// </summary>
        /// <param name="playerAnimationController">The RuntimeAnimatorController to apply to the avatar. Can be null to clear the current controller.</param>
        public void SetAnimatorController(RuntimeAnimatorController playerAnimationController)
        {
            if (_genie?.Animator != null)
            {
                _genie.Animator.runtimeAnimatorController = playerAnimationController;
            }
        }

#endregion

#region Assets (single & batch)

        /// <summary>
        /// Equips a specific asset (clothing, accessory, etc.) to the avatar asynchronously.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to equip.</param>
        /// <param name="parameters">Optional parameters for asset configuration (e.g., colors, materials, variants).</param>
        /// <returns>A UniTask that completes when the asset has been equipped.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask EquipAssetAsync(string assetId, Dictionary<string, string> parameters = null)
        {
            EnsureController();
            return _controller.EquipAssetAsync(assetId, parameters);
        }

        /// <summary>
        /// Removes a specific asset from the avatar asynchronously.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to unequip.</param>
        /// <returns>A UniTask that completes when the asset has been removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask UnequipAssetAsync(string assetId)
        {
            EnsureController();
            return _controller.UnequipAssetAsync(assetId);
        }

        /// <summary>
        /// Equips multiple assets to the avatar in a single batch operation asynchronously.
        /// This is more efficient than calling EquipAssetAsync multiple times.
        /// </summary>
        /// <param name="assets">Collection of tuples containing asset IDs and their configuration parameters.</param>
        /// <returns>A UniTask that completes when all assets have been equipped.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask EquipAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets)
        {
            EnsureController();
            return _controller.EquipAssetsAsync(assets);
        }

        /// <summary>
        /// Removes multiple assets from the avatar in a single batch operation asynchronously.
        /// This is more efficient than calling UnequipAssetAsync multiple times.
        /// </summary>
        /// <param name="assetIds">Collection of asset IDs to unequip.</param>
        /// <returns>A UniTask that completes when all specified assets have been removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask UnequipAssetsAsync(IEnumerable<string> assetIds)
        {
            EnsureController();
            return _controller.UnequipAssetsAsync(assetIds);
        }

        /// <summary>
        /// Sets the complete set of equipped assets for the avatar, replacing any currently equipped assets.
        /// This is equivalent to unequipping all current assets and then equipping the specified ones.
        /// </summary>
        /// <param name="assets">Collection of tuples containing asset IDs and their configuration parameters that should be equipped.</param>
        /// <returns>A UniTask that completes when the avatar's asset configuration has been updated.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask SetEquippedAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets)
        {
            EnsureController();
            return _controller.SetEquippedAssetsAsync(assets);
        }

        /// <summary>
        /// Checks whether a specific asset is currently equipped on the avatar.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to check.</param>
        /// <returns>True if the asset is currently equipped, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public bool IsAssetEquipped(string assetId)
        {
            EnsureController();
            return _controller.IsAssetEquipped(assetId);
        }

        /// <summary>
        /// Adds the IDs of all currently equipped assets to the provided collection.
        /// This is useful for building lists without creating new collections.
        /// </summary>
        /// <param name="results">The collection to add the equipped asset IDs to.</param>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public void AddEquippedAssetIds(ICollection<string> results)
        {
            EnsureController();
            _controller.AddEquippedAssetIds(results);
        }

        /// <summary>
        /// Gets a list containing the IDs of all currently equipped assets on the avatar.
        /// </summary>
        /// <returns>A new List containing all equipped asset IDs.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public List<string> GetEquippedAssetIds()
        {
            EnsureController();
            return _controller.GetEquippedAssetIds();
        }

#endregion

#region Colors

        /// <summary>
        /// Sets a specific color property on the avatar asynchronously (e.g., hair color, skin tone, clothing color).
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to modify.</param>
        /// <param name="color">The Color value to apply to the specified property.</param>
        /// <returns>A UniTask that completes when the color has been applied to the avatar.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask SetColorAsync(string colorId, Color color)
        {
            EnsureController();
            return _controller.SetColorAsync(colorId, color);
        }

        /// <summary>
        /// Sets multiple color properties on the avatar in a single batch operation asynchronously.
        /// This is more efficient than calling SetColorAsync multiple times.
        /// </summary>
        /// <param name="colors">Collection of GenieColorEntry objects containing color IDs and their corresponding color values.</param>
        /// <returns>A UniTask that completes when all colors have been applied to the avatar.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask SetColorsAsync(IEnumerable<GenieColorEntry> colors)
        {
            EnsureController();
            return _controller.SetColorsAsync(colors);
        }

        /// <summary>
        /// Gets the current color value for a specific color property on the avatar.
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to retrieve.</param>
        /// <returns>The current Color value if the property exists and has a color set, null otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public Color? GetColor(string colorId)
        {
            EnsureController();
            return _controller.GetColor(colorId);
        }

        /// <summary>
        /// Removes/resets a specific color property on the avatar asynchronously, reverting it to its default state.
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to reset.</param>
        /// <returns>A UniTask that completes when the color property has been reset to its default state.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask UnsetColorAsync(string colorId)
        {
            EnsureController();
            return _controller.UnsetColorAsync(colorId);
        }

        /// <summary>
        /// Removes/resets all color customizations on the avatar asynchronously, reverting all colors to their default states.
        /// </summary>
        /// <returns>A UniTask that completes when all color properties have been reset to their default states.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask UnsetAllColorsAsync()
        {
            EnsureController();
            return _controller.UnsetAllColorsAsync();
        }

        /// <summary>
        /// Checks whether a specific color property is available for customization on this avatar.
        /// </summary>
        /// <param name="colorId">The unique identifier of the color property to check availability for.</param>
        /// <returns>True if the color property exists and can be customized, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public bool IsColorAvailable(string colorId)
        {
            EnsureController();
            return _controller.IsColorAvailable(colorId);
        }

#endregion

#region Body attributes & presets

        /// <summary>
        /// Sets a specific body attribute (e.g., muscle mass, height, body proportions) with a given weight value.
        /// </summary>
        /// <param name="attributeId">The unique identifier of the body attribute to modify.</param>
        /// <param name="weight">The weight/strength of the attribute, typically ranging from 0.0 to 1.0.</param>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public void SetBodyAttribute(string attributeId, float weight)
        {
            EnsureController();
            _controller.SetBodyAttribute(attributeId, weight);
        }

        /// <summary>
        /// Gets the current weight value for a specific body attribute on the avatar.
        /// </summary>
        /// <param name="attributeId">The unique identifier of the body attribute to retrieve.</param>
        /// <returns>The current weight/strength value of the specified body attribute.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public float GetBodyAttribute(string attributeId)
        {
            EnsureController();
            return _controller.GetBodyAttribute(attributeId);
        }

        /// <summary>
        /// Applies a predefined body attributes preset to the avatar, which sets multiple body attributes to specific values.
        /// </summary>
        /// <param name="preset">The BodyAttributesPreset to apply, containing a collection of body attribute configurations.</param>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public void SetBodyPreset(BodyAttributesPreset preset)
        {
            EnsureController();
            _controller.SetBodyPreset(preset);
        }

        /// <summary>
        /// Resets all body attributes on the avatar to their default values.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public void ResetAllBodyAttributes()
        {
            EnsureController();
            _controller.ResetAllBodyAttributes();
        }

        /// <summary>
        /// Checks whether a specific body attribute is available for customization on this avatar.
        /// </summary>
        /// <param name="attributeId">The unique identifier of the body attribute to check availability for.</param>
        /// <returns>True if the body attribute exists and can be customized, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public bool IsBodyAttributeAvailable(string attributeId)
        {
            EnsureController();
            return _controller.IsBodyAttributeAvailable(attributeId);
        }

        // Deprecated compatibility (still exposed to mirror controller)
        /// <summary>
        /// Sets a body preset using the legacy GSkelModifierPreset format.
        /// This method is deprecated - use SetBodyPreset(BodyAttributesPreset) instead for new implementations.
        /// </summary>
        /// <param name="preset">The legacy GSkelModifierPreset to apply.</param>
        /// <returns>A UniTask that completes when the body preset has been applied.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        /// <remarks>This method is maintained for backwards compatibility and may be removed in future versions.</remarks>
        public UniTask SetBodyPresetAsync(GSkelModifierPreset preset)
        {
            EnsureController();
            return _controller.SetBodyPresetAsync(preset);
        }

        /// <summary>
        /// Gets the current body preset in the legacy GSkelModifierPreset format.
        /// This method is deprecated - consider using individual body attribute methods instead.
        /// </summary>
        /// <returns>The current GSkelModifierPreset representing the avatar's body configuration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        /// <remarks>This method is maintained for backwards compatibility and may be removed in future versions.</remarks>
        public GSkelModifierPreset GetBodyPreset()
        {
            EnsureController();
            return _controller.GetBodyPreset();
        }

        /// <summary>
        /// Gets the current body variation identifier for the avatar.
        /// Body variations represent different base body types or configurations.
        /// </summary>
        /// <returns>A string identifier representing the current body variation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public string GetBodyVariation()
        {
            EnsureController();
            return _controller.GetBodyVariation();
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
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask EquipTattooAsync(MegaSkinTattooSlot slot, string assetId, Dictionary<string, string> parameters = null)
        {
            EnsureController();
            return _controller.EquipTattooAsync(slot, assetId, parameters);
        }

        /// <summary>
        /// Removes a tattoo from a specific slot on the avatar's skin asynchronously.
        /// </summary>
        /// <param name="slot">The specific tattoo slot/location from which to remove the tattoo.</param>
        /// <returns>A UniTask that completes when the tattoo has been removed from the specified slot.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask UnequipTattooAsync(MegaSkinTattooSlot slot)
        {
            EnsureController();
            return _controller.UnequipTattooAsync(slot);
        }

        /// <summary>
        /// Removes all tattoos from the avatar asynchronously.
        /// </summary>
        /// <returns>A UniTask that completes when all tattoos have been removed from the avatar.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask UnequipAllTattoosAsync()
        {
            EnsureController();
            return _controller.UnequipAllTattoosAsync();
        }

        /// <summary>
        /// Checks whether a specific tattoo asset is currently equipped in a specific slot on the avatar.
        /// </summary>
        /// <param name="slot">The tattoo slot to check.</param>
        /// <param name="assetId">The unique identifier of the tattoo asset to check for.</param>
        /// <returns>True if the specified tattoo is equipped in the specified slot, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public bool IsTattooEquipped(MegaSkinTattooSlot slot, string assetId)
        {
            EnsureController();
            return _controller.IsTattooEquipped(slot, assetId);
        }

        /// <summary>
        /// Gets the asset ID of the tattoo currently equipped in a specific slot on the avatar.
        /// </summary>
        /// <param name="slot">The tattoo slot to query.</param>
        /// <returns>The asset ID of the equipped tattoo in the specified slot, or null if no tattoo is equipped.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public string GetEquippedTattoo(MegaSkinTattooSlot slot)
        {
            EnsureController();
            return _controller.GetEquippedTattoo(slot);
        }

#endregion

#region Definition import/export

        /// <summary>
        /// Exports the current avatar configuration as a JSON definition string.
        /// This definition includes all equipped assets, colors, body attributes, and tattoos.
        /// </summary>
        /// <returns>A JSON string containing the complete avatar definition.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public string GetDefinition()
        {
            EnsureController();
            return _controller.GetDefinition();
        }

        /// <summary>
        /// Applies a complete avatar configuration from a JSON definition string asynchronously.
        /// This will update all aspects of the avatar including assets, colors, body attributes, and tattoos.
        /// </summary>
        /// <param name="definition">A JSON string containing the avatar definition to apply.</param>
        /// <returns>A UniTask that completes when the avatar has been updated with the new definition.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the controller is not available.</exception>
        public UniTask SetDefinitionAsync(string definition)
        {
            EnsureController();
            return _controller.SetDefinitionAsync(definition);
        }

#endregion

#region Lifecycle

        /// <summary>
        /// Disposes native resources via controller and (optionally) caller should also destroy the GameObject hosting this avatar if needed.
        /// </summary>
        public void Dispose()
        {
            // Dispose the native side
            _controller?.Dispose();
            // Do not auto-destroy _genie.Root here; leave ownership/lifetime to caller/scene.
        }

#endregion

#region Helpers

        private void EnsureController()
        {
            if (_controller == null)
            {
                throw new InvalidOperationException("NativeUnifiedGenieController is not set on this GeniesAvatar.");
            }
        }

#endregion
    }
}
