using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Avatars.Behaviors;
using Genies.ColorPresetManager;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.MakeupPresets;
using Genies.Refs;
using Genies.ServiceManagement;
using Toolbox.Core;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ColorPresetItemPickerDataSource", menuName = "Genies/Customizer/DataSource/ColorPresetItemPickerDataSource")]
#endif
    public class ColorPresetItemPickerDataSource :
        CustomizationItemPickerDataSource
    {
        [SerializeField]
        private ColorMainTypes _mainType;

        /// <summary>
        /// The camera focus point for the tattoo body area
        /// </summary>
        [SerializeField]
        private GeniesVirtualCameraCatalog _virtualCamera;

        [SerializeField]
        [Preset(nameof(_AllCategories))]
        private string _subcategory;

        /// <summary>
        /// Used for exposing the categories for the <see cref="_mainType"/> in editor. <see cref="_subcategory"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private IReadOnlyList<string> _AllCategories
        {
            get
            {
                return _mainType switch
                {
                    ColorMainTypes.Makeup => ColorPresetUtils.MakeupColorCategories,
                    ColorMainTypes.Base => ColorPresetUtils.DnaColorCategories,
                    ColorMainTypes.Skin => ColorPresetUtils.SkinColorCategories,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }


        /// <summary>
        /// The slot id of the color preset <see cref="GetColorCategoryData"/>
        /// </summary>
        private string _colorSlotId;

        /// <summary>
        /// The event name to dispatch to analytics
        /// </summary>
        private string _colorAnalyticsEventName;

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                var config = UIDataProviderConfigs.DefaultColorPresetsConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        protected override void OnAfterInitialized()
        {
            GetColorCategoryData(out _colorSlotId, out _colorAnalyticsEventName);
        }

        public override void StartCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStarted);

            if (CurrentVirtualCameraController != null)
            {
                //Aim the camera at the body area
                CurrentVirtualCameraController.ActivateVirtualCamera(_virtualCamera);
            }

            _customizer.View.SecondaryItemPicker.Show(this).Forget();
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStopped);

            if (CurrentVirtualCameraController != null)
            {
                //Aim the camera at the body area
                CurrentVirtualCameraController.ActivateVirtualCamera(GeniesVirtualCameraCatalog.FullBodyFocusCamera);
            }

            _customizer.View.SecondaryItemPicker.Hide();
        }

        public override ItemPickerCtaConfig GetCtaConfig()
        {
            return null;
        }

        public override ItemPickerLayoutConfig GetLayoutConfig()
        {
            return new ItemPickerLayoutConfig()
            {
                horizontalOrVerticalLayoutConfig = new HorizontalOrVerticalLayoutConfig() { padding = new RectOffset(16,                  16, 28, 28), spacing = 12 },
                gridLayoutConfig = new GridLayoutConfig() { cellSize = new Vector2(56, 56), columnCount = 5, padding = new RectOffset(16, 16, 24, 8), spacing = new Vector2(16, 16) }
            };
        }

        /// <summary>
        /// Get which color preset index is currently selected for the current <see cref="_mainType"/>
        /// and its <see cref="_subcategory"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<SimpleColorUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<ColoredInventoryAsset, SimpleColorUiData>(index, "ColorPresetItemPicker");
        }

        /// <summary>
        /// Get the slot id to equip the color preset to based on <see cref="_mainType"/> of the color preset
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void GetColorCategoryData(out string avatarSlotId, out string analyticsEventName)
        {
            analyticsEventName = CustomizationAnalyticsEvents.SkinColorPresetClickEvent;

            //For skin type we don't have a slot since its a single category.
            avatarSlotId = null;

            //Mapping for DNA
            if (_mainType == ColorMainTypes.Base)
            {
                var subcategory = (ColorPresetCategory)Enum.Parse(typeof(ColorPresetCategory), _subcategory);
                (avatarSlotId, analyticsEventName) = subcategory switch
                {
                    ColorPresetCategory.EyeColor => (UnifiedMaterialSlot.Eyes, CustomizationAnalyticsEvents.EyeColorPresetClickEvent),
                    ColorPresetCategory.EyeBrowColor => (UnifiedMaterialSlot.Eyebrows, CustomizationAnalyticsEvents.EyeBrowColorPresetClickEvent),
                    ColorPresetCategory.FacialHairColor => (UnifiedMaterialSlot.FacialHair, CustomizationAnalyticsEvents.FacialHairColorPresetClickEvent),
                    ColorPresetCategory.HairColor => (UnifiedMaterialSlot.Hair, CustomizationAnalyticsEvents.HairColorPresetClickEvent),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            //Mapping for Makeup
            else if (_mainType == ColorMainTypes.Makeup)
            {
                var subcategory = (MakeupPresetCategory)Enum.Parse(typeof(MakeupPresetCategory), _subcategory);

                analyticsEventName = CustomizationAnalyticsEvents.MakeupColorPresetClickEvent;
                avatarSlotId = subcategory switch
                {
                    MakeupPresetCategory.Stickers => MakeupSlot.Stickers,
                    MakeupPresetCategory.Lipstick => MakeupSlot.Lipstick,
                    MakeupPresetCategory.Freckles => MakeupSlot.Freckles,
                    MakeupPresetCategory.FaceGems => MakeupSlot.FaceGems,
                    MakeupPresetCategory.Eyeshadow => MakeupSlot.Eyeshadow,
                    MakeupPresetCategory.Blush => MakeupSlot.Blush,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        /// <summary>
        /// Business logic for what happens when a cell is clicked.
        /// </summary>
        /// <param name="index"> Index of the cell </param>
        /// <param name="clickedCell"> The view of the cell that was clicked </param>
        /// <param name="wasSelected"> If it was already selected </param>
        /// <param name="cancellationToken"> Cancellation token </param>
        /// <returns></returns>
        public override async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            //Load the ui data.
            if (TryGetLoadedData<SimpleColorUiData>(index, out var data) is false)
            {
                return false;
            }

            if (wasSelected && data.Item.IsEditable)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            //Get which color modification command to execute based on the color preset _mainType.
            //each type has a specific way its applied on the avatar
            ICommand command = new EquipNativeAvatarAssetCommand(data.Item.AssetId, CurrentCustomizableAvatar);

            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var props = new AnalyticProperties();
            props.AddProperty("name", $"{data.Item.DisplayName}");
            AnalyticsReporter.LogEvent(_colorAnalyticsEventName, props);

            _customizer.RegisterCommand(command);

            return true;
        }


        /// <summary>
        /// Initialize the cell view when its visible.
        /// </summary>
        /// <param name="view"> The view to initialize </param>
        /// <param name="index"> Cell index </param>
        /// <param name="isSelected"> If its already selected </param>
        /// <param name="cancellationToken"> The cancellation token </param>
        /// <returns></returns>
        public override async UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            return await InitializeCellViewBaseAsync<ColoredInventoryAsset, SimpleColorUiData>(view, index, cancellationToken);
        }
    }
}
