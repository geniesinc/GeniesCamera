using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Behaviors;
using Genies.CameraSystem;
using Genies.UI.Components.Widgets;
using UnityEngine;
using Genies.Customization.Framework;
using Genies.Looks.Customization.Commands;
using Genies.MegaEditor;
using Genies.Naf;
using Genies.ServiceManagement;
using Genies.Ugc;
using Genies.Ugc.CustomSkin;
using Genies.UIFramework.Widgets;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Controller for the customize color view.
    /// </summary>
    public class CustomSkinColorCustomizationController : BaseCustomizationController
    {
        [SerializeField]
        private CustomizeColorView _customizeColorViewPrefab;
        private CustomizeColorView _customizeColorViewInstance;

        [SerializeField]
        private SkinColorItemPickerDataSource dataSource;

        // Service to save skin color
        private SkinColorService _SkinColorService => this.GetService<SkinColorService>();

        // skin color data to be used by the service
        private SkinColorData _editedData;

        private PictureInPictureController _PictureInPictureController => this.GetService<PictureInPictureController>();
        private VirtualCameraController<GeniesVirtualCameraCatalog> _VirtualCameraController => this.GetService<VirtualCameraController<GeniesVirtualCameraCatalog>>();
        private static Color _CurrentEditingSkinColor => CurrentCustomizableAvatar.GetColor(GenieColor.Skin) ?? Color.black;

        public override async UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;

            _customizeColorViewInstance = _customizer.View.GetOrCreateViewInLayer("custom-skin-color-view",
                CustomizerViewLayer.CustomizationEditorFullScreen, _customizeColorViewPrefab);

            return await UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _PictureInPictureController.canBeDisabled = false;
            _PictureInPictureController.Enable();
            _VirtualCameraController.SetFullScreenModeInFocusCameras(true);

            // Initialize the customize color view with the current skin color
            Color initialColor = dataSource.CurrentSkinColorData.BaseColor;
            _customizeColorViewInstance.Initialize(initialColor);

            // Add listener to the color picker color change event for regions
            _customizeColorViewInstance.OnColorSelected.AddListener(UpdateAvatarSkinColor);

            // Force one-time avatar skin color update to the initial color
            UpdateAvatarSkinColor(initialColor);
        }

        public override void StopCustomization()
        {
            _PictureInPictureController.canBeDisabled = true;
            _PictureInPictureController.Disable();
            _VirtualCameraController.SetFullScreenModeInFocusCameras(false);

            CurrentCustomColorViewState = CustomColorViewState.Normal;
        }

        public override bool HasSaveAction()
        {
            return true;
        }

        public override async UniTask<bool> OnSaveAsync()
        {
            // if we have a selected item, clicking this save button will override its color data.
            // else it will create a new color data, whose id can be null (cloud save will auto assign a guid to it).
            switch (CurrentCustomColorViewState)
            {
                case CustomColorViewState.Edit:
                    _editedData = dataSource.CurrentSkinColorData;
                    _editedData.BaseColor = _CurrentEditingSkinColor;
                    break;

                case CustomColorViewState.CreateNew:
                    _editedData = new SkinColorData(){ Id = "", BaseColor = _CurrentEditingSkinColor };
                    break;

                case CustomColorViewState.Normal:
                default:
                    return true;
            }

            SkinColorData savedData = await _SkinColorService.CreateOrUpdateCustomSkin(_editedData);

            // Update current color data to the saved color.
            dataSource.CurrentSkinColorData = savedData;

            await UpdateAvatarSkinColorUsingCommand(savedData.Id);

            return true;
        }

        public override bool HasDiscardAction()
        {
            return true;
        }

        public override async UniTask<bool> OnDiscardAsync()
        {
            var previousSkinColorData = dataSource.PreviousSkinColorData;
            dataSource.CurrentSkinColorData = previousSkinColorData;

            await UpdateAvatarSkinColorUsingCommand(previousSkinColorData.Id);

            return true;
        }

        /// <summary>
        /// Updates the skin color of the avatar without using the command pattern.
        /// </summary>
        /// <param name="color">the given skin color</param>
        private static async void UpdateAvatarSkinColor(Color color)
        {
            NativeUnifiedGenieController avatarController = CurrentCustomizableAvatar;

            if (avatarController != null)
            {
                await avatarController.SetColorAsync(GenieColor.Skin, color);
            }
            else
            {
                Debug.LogError("Current Customizable Avatar is null");
            }
        }

        private async UniTask UpdateAvatarSkinColorUsingCommand(string id)
        {
            ICommand command = new EquipNativeAvatarAssetCommand(id, CurrentCustomizableAvatar);
            await command.ExecuteAsync();
        }

        public override void Dispose()
        {
            _customizeColorViewInstance.Dispose();
        }
    }
}
