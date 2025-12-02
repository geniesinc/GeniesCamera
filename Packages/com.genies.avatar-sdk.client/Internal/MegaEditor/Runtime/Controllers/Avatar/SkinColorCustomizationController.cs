using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars.Behaviors;
using Genies.Customization.Framework;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.Ugc;
using Genies.Ugc.CustomSkin;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    public class SkinColorCustomizationController : BaseCustomizationController
    {
        public SkinColorItemPickerDataSource dataSource;

        /// <summary>
        /// The focus camera to activate and set as active on this customization controller.
        /// </summary>
        public GeniesVirtualCameraCatalog virtualCamera;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;
            dataSource.Initialize(customizer);

            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            //Activate the selected virtual camera
            CurrentVirtualCameraController.ActivateVirtualCamera(virtualCamera);

            CurrentCustomColorViewState = CustomColorViewState.Normal;

            AddListeners();

            dataSource.StartCustomization();

            // check this logic to understand initialization and how data source is shown
            _customizer.View.SecondaryItemPicker.Show(dataSource).Forget();
        }

        public override void StopCustomization()
        {
            //Aim the camera at the full body
            CurrentVirtualCameraController.ActivateVirtualCamera(GeniesVirtualCameraCatalog.FullBodyFocusCamera);

            RemoveListeners();

            _customizer.View.EditOrDeleteController.DeactivateButtonsImmediately();

            _customizer.View.SecondaryItemPicker.Hide();

            dataSource.StopCustomization();
        }

        private void AddListeners()
        {
            _customizer.View.EditOrDeleteController.OnEditClicked += EditCustomSkinColorData;
            _customizer.View.EditOrDeleteController.OnDeleteClicked += DeleteCustomSkinColorData;
            _customizer.View.SecondaryItemPicker.OnScroll += CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
        }

        private void RemoveListeners()
        {
            _customizer.View.EditOrDeleteController.OnEditClicked -= EditCustomSkinColorData;
            _customizer.View.EditOrDeleteController.OnDeleteClicked -= DeleteCustomSkinColorData;
            _customizer.View.SecondaryItemPicker.OnScroll -= CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
        }

        public override void Dispose()
        {
            base.Dispose();
            dataSource.Dispose();
        }

        private void EditCustomSkinColorData()
        {
            CurrentCustomColorViewState = CustomColorViewState.Edit;
            _customizer.GoToEditItemNode();
        }

        private async void DeleteCustomSkinColorData()
        {
            // The overall logic of deleting is to first update visuals (avatar skin color, UI) for immediate feedback,
            // while deleting the data in the backend async.
            var deletedDataId = dataSource.CurrentLongPressColorData.AssetId; // this Id is same for AssetId, and UiData.AssetId

            // Trigger the animation of closing the edit and delete button and forget.
            _customizer.View.EditOrDeleteController.DisableAndDeactivateButtons().Forget();

            // For avatar skin color change, equip the next color available in the UI list.
            // Currently we should have the preset ones always available (since they are non-editable), so next will always be available.
            // In the future, we might want to make the preset ones editable, in which case if next is not available, equip the previous one.
            // If previous is not available (that means we only have one in the list before deleting), set to the default.
            var nextIndexToEquip = dataSource.CurrentLongPressIndex + 1;
            Ref<SimpleColorUiData> nextUiDataRef = await dataSource.GetDataForIndexAsync(nextIndexToEquip); // this can be sync if the data exists in the cache

            // Set the current skin color data to the next item
            if (nextUiDataRef.Item?.InnerColor != null)
            {
                dataSource.CurrentSkinColorData = new SkinColorData { BaseColor = nextUiDataRef.Item.InnerColor};
            }

            // Update avatar skin color
            await SetSkinColorUsingCommandAsync(nextUiDataRef.Item.AssetId);

            // Delete the data in the backend
            //await SkinColorServiceInstance.DeleteCustomSkinAsync(deletedDataId);

            // Dispose current data source, reload data from backend, and reinitialize
            dataSource.Dispose();
            await dataSource.InitializeAndGetCountAsync(new());

            // Call the picker show the updated view
            _customizer.View.SecondaryItemPicker.Show(dataSource).Forget();
        }

        public override UniTask<bool> OnDiscardAsync()
        {
            UpdateAvatarSkinColor(dataSource.CurrentSkinColorData.BaseColor);
            return UniTask.FromResult(true);
        }

        private void CloseEditOrDeleteButtonsWhenCrossingLeftMargin()
        {
            var editOrDeleteController = _customizer.View.EditOrDeleteController;
            if (editOrDeleteController.IsActive && editOrDeleteController.transform.localPosition.x < -120)
            {
                editOrDeleteController.DeactivateButtonsImmediately();
            }
        }

        private static async UniTask SetSkinColorUsingCommandAsync(string colorId)
        {
            await CurrentCustomizableAvatar.UnsetColorAsync(GenieColor.Skin);
            ICommand command = new EquipNativeAvatarAssetCommand(colorId, CurrentCustomizableAvatar);
            await command.ExecuteAsync(new CancellationTokenSource().Token);
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
    }
}
