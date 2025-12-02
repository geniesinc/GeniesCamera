using Cysharp.Threading.Tasks;
using UnityEngine;
using Genies.Avatars.Behaviors;
using Genies.Customization.Framework;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.Ugc.CustomHair;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Controller for the hair editing UI view.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "HairCustomizationController", menuName = "Genies/Customizer/Controllers/Hair Customization Controller")]
#endif
    public class HairCustomizationController : BaseCustomizationController
    {
        private HairColorService _HairColorService => this.GetService<HairColorService>();

        public HairItemPickerDataSource hairItemDataSource;

        public HairColorItemPickerDataSource hairColorDataSource;

        /// <summary>
        /// The focus camera to activate and set as active on this customization controller.
        /// </summary>
        public GeniesVirtualCameraCatalog virtualCamera;

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;

            hairItemDataSource.Initialize(_customizer);
            if (useCustomLayoutConfig)
            {
                hairItemDataSource.OverrideWithCustomLayoutConfig(customLayoutConfig);
            }

            hairColorDataSource.Initialize(_customizer);


            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName, nameof(HairCustomizationController),
                "open hair category");
            //Activate the selected virtual camera
            CurrentVirtualCameraController.ActivateVirtualCamera(virtualCamera);

            CurrentCustomColorViewState = CustomColorViewState.Normal;

            AddListeners();

            // check this logic to understand initialization and how data source is shown
            hairItemDataSource.StartCustomization();
            _customizer.View.PrimaryItemPicker.Show(hairItemDataSource).Forget();


            hairColorDataSource.StartCustomization();
            _customizer.View.SecondaryItemPicker.Show(hairColorDataSource).Forget();

        }

        public override void StopCustomization()
        {
            FinishPreviousSpan();
            _InstrumentationManager.FinishChildSpan(_categorySpan);

            //Aim the camera at the full body
            CurrentVirtualCameraController.ActivateVirtualCamera(GeniesVirtualCameraCatalog.FullBodyFocusCamera);

            RemoveListeners();

            _customizer.View.EditOrDeleteController.DeactivateButtonsImmediately();

            _customizer.View.PrimaryItemPicker.Hide();
            _customizer.View.SecondaryItemPicker.Hide();

            hairItemDataSource.StopCustomization();
            hairColorDataSource.StopCustomization();
        }

        public override void OnUndoRedo()
        {
            FinishPreviousSpan();
            //When an undo/redo happens we want to refresh the current selection
            _customizer.View.PrimaryItemPicker.RefreshSelection().Forget();
            _customizer.View.SecondaryItemPicker.RefreshSelection().Forget();
        }

        private void FinishPreviousSpan()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
        }

        private void SpanHairItem(string assetId)
        {
            string currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,assetId, "hair asset id");
            FinishPreviousSpan();
            _previousSpan = currentPoseSpan;
        }

        private void AddListeners()
        {
            _customizer.View.EditOrDeleteController.OnEditClicked += EditCustomHairColorData;
            _customizer.View.EditOrDeleteController.OnDeleteClicked += DeleteCustomHairColorData;
            _customizer.View.SecondaryItemPicker.OnScroll += CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
            // performance monitoring
            hairItemDataSource.OnNoneSelected += FinishPreviousSpan;
            hairItemDataSource.OnItemClicked += SpanHairItem;
        }

        private void RemoveListeners()
        {
            _customizer.View.EditOrDeleteController.OnEditClicked -= EditCustomHairColorData;
            _customizer.View.EditOrDeleteController.OnDeleteClicked -= DeleteCustomHairColorData;
            _customizer.View.SecondaryItemPicker.OnScroll -= CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
            // performance monitoring
            hairItemDataSource.OnNoneSelected -= FinishPreviousSpan;
            hairItemDataSource.OnItemClicked -= SpanHairItem;
        }

        public override void Dispose()
        {
            base.Dispose();

            _categorySpan = null;
            _previousSpan = null;

            hairItemDataSource.Dispose();
            hairColorDataSource.Dispose();
        }

        private void EditCustomHairColorData()
        {
            hairColorDataSource.Dispose();

            // Set the current hair color ID from the long-pressed data (similar to delete method)
            if (hairColorDataSource.CurrentLongPressColorData != null)
            {
                hairColorDataSource.PreviousHairColorId = hairColorDataSource.CurrentHairColorId;
                hairColorDataSource.CurrentHairColorId = hairColorDataSource.CurrentLongPressColorData.AssetId;
            }
            CurrentCustomColorViewState = CustomColorViewState.Edit;
            _customizer.GoToEditItemNode();
        }

        private async void DeleteCustomHairColorData()
        {
            // The overall logic of deleting is to first update visuals (avatar skin color, UI) for immediate feedback,
            // while deleting the data in the backend async.
            var deletedDataId = hairColorDataSource.CurrentLongPressColorData.AssetId; // this Id is same for AssetId, and UiData.AssetId

            // Trigger the animation of closing the edit and delete button and forget.
            _customizer.View.EditOrDeleteController.DisableAndDeactivateButtons().Forget();

            // For avatar skin color change, equip the next color available in the UI list.
            // Currently we should have the preset ones always available (since they are non-editable), so next will always be available.
            // In the future, we might want to make the preset ones editable, in which case if next is not available, equip the previous one.
            // If previous is not available (that means we only have one in the list before deleting), set to the default.
            var nextIndexToEquip = hairColorDataSource.CurrentLongPressIndex + 1;
            Ref<GradientColorUiData> nextUiDataRef = await hairColorDataSource.GetDataForIndexAsync(nextIndexToEquip); // this can be sync if the data exists in the cache

            var correctId = nextUiDataRef.Item.AssetId;
            hairColorDataSource.CurrentHairColorId = correctId;

            // Update avatar hair color
            if (hairColorDataSource.CustomIds != null && hairColorDataSource.CustomIds.Contains(correctId))
            {
                var hairColor = await _HairColorService.CustomColorDataAsync(correctId);

                var newColors = new GenieColorEntry[]
                {
                    new (GenieColor.HairBase, hairColor.ColorBase),
                    new (GenieColor.HairR,    hairColor.ColorR),
                    new (GenieColor.HairG,    hairColor.ColorG),
                    new (GenieColor.HairB,    hairColor.ColorB),
                };

                ICommand command = new SetNativeAvatarColorsCommand(newColors, CurrentCustomizableAvatar);
                await command.ExecuteAsync();
            }
            else
            {
                var colors = HairColorItemPickerDataSource.SafeGetColorsArray(nextUiDataRef.Item);
                var entries = HairColorItemPickerDataSource.MapToHairColors(colors);

                ICommand command = new SetNativeAvatarColorsCommand(entries, CurrentCustomizableAvatar);
                await command.ExecuteAsync();
            }

            // Delete the data in the backend
            await _HairColorService.DeleteCustomHairAsync(deletedDataId);

            // Dispose current data source, reload data from backend, and reinitialize
            hairColorDataSource.Dispose();
            await hairColorDataSource.InitializeAndGetCountAsync(new());

            // Call the picker show the updated view
            _customizer.View.SecondaryItemPicker.Show(hairColorDataSource).Forget();
        }

        private void CloseEditOrDeleteButtonsWhenCrossingLeftMargin()
        {
            var editOrDeleteController = _customizer.View.EditOrDeleteController;
            if (editOrDeleteController.IsActive && editOrDeleteController.transform.localPosition.x < -120)
            {
                editOrDeleteController.DeactivateButtonsImmediately();
            }
        }
    }
}

