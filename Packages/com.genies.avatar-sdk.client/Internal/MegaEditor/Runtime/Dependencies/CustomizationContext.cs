using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.CameraSystem;
using Genies.CameraSystem.Focusable;
using Genies.Components.CreatorTools.TexturePlacement;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.Looks.Core;
using Genies.Looks.Service;
using Genies.Naf;
using Genies.ServiceManagement;
using Genies.Ugc;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// States to manage behavior and system logic of UIs such as save and discard.
    /// </summary>
    public enum CustomColorViewState
    {
        /// <summary>
        /// Not creating new or editing.
        /// </summary>
        Normal,

        /// <summary>
        /// Creating a new item.
        /// </summary>
        CreateNew,

        /// <summary>
        /// Editing to override an item.
        /// </summary>
        Edit,
    }

    public enum UgcRegionEditState
    {
        Regions,
        PlaceImage,
    }

    /// <summary>
    /// Service for tracking which genies customizable entities are active and all the dependencies needed
    /// for getting the UX needed during customization.
    /// </summary>
    public static class CustomizationContext
    {
        public static IRealtimeLookView CurrentRealtimeLookView { get; private set; }
        public static NativeUnifiedGenieController CurrentCustomizableAvatar { get; private set; }
        public static ICustomizableUgcWearable CurrentCustomizableWearable { get; private set; }
        public static IWearableRender PreviewWearableRender { get; private set; }
        public static VirtualCameraController<GeniesVirtualCameraCatalog> CurrentVirtualCameraController { get; private set; }
        public static IFocusable CurrentFocusableAvatar { get; private set; }
        public static IStyleOptionsMenu CurrentStyleOptionsMenu { get; } = null; // Will be set by UGC package
        public static CustomHairColorData CurrentCustomizableHairColor { get; private set; }

        /// <summary> Current state of customization to track the logic of creating new or editing. </summary>
        public static CustomColorViewState CurrentCustomColorViewState { get; set; } = CustomColorViewState.Normal;

        //current avatar DNA subcategory selected for customization (currently used for facechaos customization)
        public static AvatarBaseCategory CurrentDnaCustomizationViewState { get; set; } = AvatarBaseCategory.None;

        public static UgcRegionEditState CurrentUgcRegionEditState { get; set; } = UgcRegionEditState.Regions;

        public static bool IsUploadingCustomImage { get; set; }
        public static string CurrentSelectedImagePlacementAssetId { get; set; }
        public static int CurrentSelectedImagePlacementIndex { get; set; }

        /// <summary> Sets to true when a texture has been projected but not necessarily save in the definition yet </summary>
        public static bool HasImagePlacementBeenApplied { get; set; }

        /// <summary> Stores current skin color Ids. </summary>
        /// <remarks> Assign these values when entering the customizing flow
        /// to prevent users from deleting current equipped skin colors
        /// (e.g. by disabling the edit/delete button on the UI).</remarks>
        public static string EquippedUnifiedGenieSkinColorId;

        public static string EquippedUnifiedGenieHairColorId;
        public static HashSet<string> EquippedSkinColorIds { get; } = new HashSet<string>();
        public static HashSet<string> EquippedHairColorIds { get; } = new HashSet<string>();

        /// <summary>
        /// Updates the hashsets of equipped custom color ids to prevent
        /// user from deleting them and avoid breaking saved Looks
        /// </summary>
        /// <remarks>
        /// Call this method each time a new look is created or deleted.
        /// To-do: add OnLookCreated and OnLookDeleted events and have this method listen to them.
        /// </remarks>
        public static async UniTask UpdateEquippedColorIds()
        {
            EquippedSkinColorIds.Clear();
            EquippedHairColorIds.Clear();

            EquippedSkinColorIds.Add(EquippedUnifiedGenieSkinColorId);
            EquippedHairColorIds.Add(EquippedUnifiedGenieHairColorId);

            var service = ServiceManager.Get<ILooksService>();
            var savedLooks = await service.GetAllLooksAsync();

            foreach (var look in savedLooks)
            {
                var avatarDefinitionJson = look.AvatarDefinition;

                Avatars.AvatarDefinition deserializedAvatarDefinition = null;
                try
                {
                    deserializedAvatarDefinition = JsonConvert.DeserializeObject<Avatars.AvatarDefinition>(avatarDefinitionJson);
                }
                catch (Exception e)
                {
                    CrashReporter.LogWarning(e);
                }

                if (deserializedAvatarDefinition == null)
                {
                    continue;
                }

                var skinColorId = deserializedAvatarDefinition.SkinMaterial;

                EquippedSkinColorIds.Add(skinColorId);

                var hairColorId = deserializedAvatarDefinition.HairMaterial;

                EquippedHairColorIds.Add(hairColorId);
            }
        }

        /// <summary> Find material matching current wearable and clear the ProjectedTexture parameter. </summary>
        public static void ClearLiveMaterial()
        {
            var skinnedMeshRenderer = CurrentCustomizableAvatar.Genie.Root.GetComponentInChildren<SkinnedMeshRenderer>();
            var mats = skinnedMeshRenderer.sharedMaterials;
            var targetMaterialName = CurrentCustomizableWearable.Wearable.TemplateId;
            foreach (var mat in mats)
            {
                if (mat.name.Contains(targetMaterialName))
                {
                    mat.SetTexture(Tattooenator.ProjectedTextureAlbedoPropertyId, Texture2D.blackTexture);
                    return;
                }
            }

            skinnedMeshRenderer.sharedMaterials = mats;
        }

        static CustomizationContext()
        {
            // CurrentCustomizableWearable will be initialized by the UGC package
            CurrentCustomizableWearable = null;
        }

        public static void SetCustomHairData(CustomHairColorData data)
        {
            CurrentCustomizableHairColor = data;
        }

        public static void SetVirtualCameraController(VirtualCameraController<GeniesVirtualCameraCatalog> virtualCameraController)
        {
            CurrentVirtualCameraController = virtualCameraController;
        }

        public static void SetCustomizableAvatar(NativeUnifiedGenieController controller, IFocusable focusable)
        {
            CurrentCustomizableAvatar = controller;
            CurrentFocusableAvatar = focusable;
        }

        public static void SetRealtimeLookView(IRealtimeLookView lookView)
        {
            CurrentRealtimeLookView = lookView;
        }

        public static void FinishPreviewWearableRender()
        {
            PreviewWearableRender?.Dispose();
        }

        public static void Dispose()
        {
            CurrentCustomizableWearable.Dispose();
            PreviewWearableRender?.Dispose();
            SetCustomizableAvatar(null, null);
            SetVirtualCameraController(null);
        }
    }
}
