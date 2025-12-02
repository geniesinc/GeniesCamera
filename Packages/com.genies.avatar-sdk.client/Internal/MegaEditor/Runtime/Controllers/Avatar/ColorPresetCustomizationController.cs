using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Avatars.Behaviors;
using Genies.ColorPresetManager;
using Genies.CrashReporting;
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
    /// <summary>
    /// Wrapper interface to handle both SimpleColorUiData and GradientColorUiData uniformly
    /// </summary>
    public abstract class ColorUiDataWrapper
    {
        public string AssetId;
        public string DisplayName;
        public Material Material;
        public bool IsSimpleColor;
        public SimpleColorUiData SimpleColorData;
        public GradientColorUiData GradientColorData;
        public abstract Color GetPrimaryColor();

        public void Dispose()
        {
            SimpleColorData?.Dispose();
        }
    }

    /// <summary>
    /// Wrapper for SimpleColorUiData
    /// </summary>
    public class SimpleColorWrapper : ColorUiDataWrapper
    {
        public SimpleColorWrapper(SimpleColorUiData data)
        {
            SimpleColorData = data;
            AssetId = data.AssetId;
            DisplayName = data.DisplayName;
            Material = data.Material;
            IsSimpleColor = true;
        }


        public override Color GetPrimaryColor() => SimpleColorData.InnerColor;
    }

    /// <summary>
    /// Wrapper for GradientColorUiData
    /// </summary>
    public class GradientColorWrapper : ColorUiDataWrapper
    {
        public GradientColorWrapper(GradientColorUiData data)
        {
            GradientColorData = data;
            AssetId = data.AssetId;
            DisplayName = data.DisplayName;
            Material = data.Material;
            IsSimpleColor = false;
        }

        public override Color GetPrimaryColor() => GradientColorData.ColorBase;
    }

    public class ColorPresetCustomizationController : BaseCustomizationController, IItemPickerDataSource
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

        private const string _eyeColorCategory = "EyeColor";


        [SerializeField] private Sprite _eyeColorSprite;

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

        private InventoryUIDataProvider<ColoredInventoryAsset, SimpleColorUiData> _eyeColorProvider;
        private InventoryUIDataProvider<ColoredInventoryAsset, GradientColorUiData> _makeupColorProvider;


        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _eyeColorProvider = new(
                UIDataProviderConfigs.DefaultAvatarEyesConfig,
                ServiceManager.Get<IAssetsService>());

            _makeupColorProvider = new(
                UIDataProviderConfigs.MakeupColorPresetsConfig,
                ServiceManager.Get<IAssetsService>());

            _customizer = customizer;
            GetColorCategoryData(out _colorSlotId, out _colorAnalyticsEventName);
            _loadedData = new();
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStarted);

            //Aim the camera at the body area
            CurrentVirtualCameraController.ActivateVirtualCamera(_virtualCamera);

            _customizer.View.PrimaryItemPicker.Show(this).Forget();
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStopped);
            CurrentVirtualCameraController.ActivateVirtualCamera(GeniesVirtualCameraCatalog.FullBodyFocusCamera);
            _customizer.View.PrimaryItemPicker.Hide();
        }

        public override void OnUndoRedo()
        {
            _customizer.View.PrimaryItemPicker.RefreshSelection().Forget();
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return null;
        }

        /// <summary>
        /// Get which color preset index is currently selected for the current <see cref="_mainType"/>
        /// and its <see cref="_subcategory"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int GetCurrentSelectedIndex()
        {
            if (_ids != null)
            {
                for (int i = 0; i < _ids.Count; i++)
                {
                    if (CurrentCustomizableAvatar.IsAssetEquipped(_ids[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        // Pagination support
        public bool HasMoreItems
        {
            get
            {
                if (_subcategory == _eyeColorCategory)
                {
                    return _eyeColorProvider?.HasMoreData ?? false;
                }
                else
                {
                    return _makeupColorProvider?.HasMoreData ?? false;
                }
            }
        }

        public bool IsLoadingMore
        {
            get
            {
                if (_subcategory == _eyeColorCategory)
                {
                    return _eyeColorProvider?.IsLoadingMore ?? false;
                }
                else
                {
                    return _makeupColorProvider?.IsLoadingMore ?? false;
                }
            }
        }

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            if (IsLoadingMore || !HasMoreItems)
            {
                return false;
            }

            try
            {
                List<IAssetUiData> newItems;

                if (_subcategory == _eyeColorCategory)
                {
                    if (_eyeColorProvider == null)
                    {
                        return false;
                    }


                    newItems = (await _eyeColorProvider.LoadMoreAsync().AttachExternalCancellation(cancellationToken))
                        .Cast<IAssetUiData>().ToList();

                    _ids = await _eyeColorProvider.GetAllAssetIds(pageSize: InventoryConstants.DefaultPageSize);
                }
                else // Makeup
                {
                    if (_makeupColorProvider == null)
                    {
                        return false;
                    }

                    try
                    {
                        newItems = (await _makeupColorProvider.LoadMoreAsync()
                                .AttachExternalCancellation(cancellationToken))
                            .Cast<IAssetUiData>().ToList();

                        _ids = await _makeupColorProvider.GetAllAssetIds(pageSize: InventoryConstants.DefaultPageSize)
                            .AttachExternalCancellation(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _ids = new();
                        return false;
                    }
                }

                return newItems.Count > 0;
            }
            catch (Exception e)
            {
                CrashReporter.Log($"ColorPresetCustomizationController's LoadMoreItemsAsync failed: {e}", LogSeverity.Error);
                return false;
            }
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            if (_subcategory == _eyeColorCategory)
            {
                _ids = await _eyeColorProvider.GetAllAssetIds(pageSize: InventoryConstants.DefaultPageSize);

            }
            else // Makeup
            {
                _ids = await _makeupColorProvider.GetAllAssetIds(pageSize: InventoryConstants.DefaultPageSize);
            }

            return _ids.Count;
        }

        public override Vector2 GetCellSize(int index)
        {
            return new Vector2(56, 56);
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<ColorUiDataWrapper>> GetDataForIndexAsync(int index)
        {
            string id = string.Empty;

            try
            {
                if (TryGetLoadedData<ColorUiDataWrapper>(index, out var data))
                {
                    return data;
                }

                if (index < 0 || _ids == null || index >= _ids.Count)
                {
                    return default;
                }

                id = _ids[index];

                Ref<ColorUiDataWrapper> newDataRef;

                if (_subcategory == _eyeColorCategory)
                {
                    // Load SimpleColorUiData for eye colors
                    var simpleUiData = await _eyeColorProvider.GetDataForAssetId(id);
                    var wrapper = new SimpleColorWrapper(simpleUiData);
                    newDataRef = CreateRef.FromDependentResource((ColorUiDataWrapper)wrapper);
                }
                else
                {
                    // Load GradientColorUiData for makeup colors
                    var gradientUiData = await _makeupColorProvider.GetDataForAssetId(id);
                    var wrapper = new GradientColorWrapper(gradientUiData);
                    newDataRef = CreateRef.FromDependentResource((ColorUiDataWrapper)wrapper);
                }

                _loadedData ??= new();
                _loadedData[index] = newDataRef;
                return newDataRef;
            }
            catch (Exception e)
            {
                CrashReporter.Log($"ColorPresetCustomization's GetDataForIndexAsync with index {index} & id {id} can't be retrieved: {e}", LogSeverity.Error);
                return default;
            }
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
        public async UniTask<bool> OnItemClickedAsync(
            int index,
            ItemPickerCellView clickedCell,
            bool wasSelected,
            CancellationToken cancellationToken)
        {
            if (wasSelected)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            ICommand command = null;

            if (TryGetLoadedData<ColorUiDataWrapper>(index, out var data) is false)
            {
                return false;
            }

            if (_mainType == ColorMainTypes.Makeup)
            {
                var subcategory = (MakeupPresetCategory)Enum.Parse(typeof(MakeupPresetCategory), _subcategory);

                // For gradient color makeup, use all colors
                Color[] colors = data.Item.GradientColorData.GetColorsArray();
                command = new EquipMakeupColorCommand(subcategory, colors, CurrentCustomizableAvatar);
            }
            else if (_subcategory == "EyeColor")
            {
                // Handle eye colors using the asset ID
                command = new EquipNativeAvatarAssetCommand(data.Item.AssetId, CurrentCustomizableAvatar);
            }
            else
            {
                // Handle other color types using the asset ID
                command = new EquipNativeAvatarAssetCommand(data.Item.AssetId, CurrentCustomizableAvatar);
            }

            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var props = new AnalyticProperties();

            if (_loadedData.ContainsKey(index) && data.Item != null)
            {
                props.AddProperty("name", $"{data.Item.DisplayName}");
            }
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
        public async UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            var dataRef = await GetDataForIndexAsync(index);

            if (dataRef.Item == null)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var asGeneric = view as GenericItemPickerCellView;
            if (asGeneric == null)
            {
                return false;
            }

            if (_subcategory == _eyeColorCategory && dataRef.Item.IsSimpleColor)
            {
                // Handle eye color cell initialization with simple color display
                var primaryColor = dataRef.Item.GetPrimaryColor();
                asGeneric.thumbnail.sprite = _eyeColorSprite;
                asGeneric.thumbnail.color = primaryColor;
                asGeneric.thumbnail.material = null;
            }
            else
            {
                // Handle material-based display for gradient colors and other types
                asGeneric.thumbnail.material = dataRef.Item.Material;
            }

            asGeneric.SetDebuggingAssetLabel(dataRef.Item.DisplayName ?? dataRef.Item.AssetId);

            return true;
        }

        /// <summary>
        /// Dispose the controller
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            _eyeColorProvider?.Dispose();
            _eyeColorProvider = null;

            _makeupColorProvider?.Dispose();
            _makeupColorProvider = null;
        }
    }
}
