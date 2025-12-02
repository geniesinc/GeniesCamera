using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Avatars.Services.Flair;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Models;
using Genies.Naf;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    public class FlairColorItemPickerDataSource : CustomizationItemPickerDataSource
    {
        [SerializeField] private NoneOrNewCTAController _Cta;
        private IFlairCustomColorPresetService _flairCustomColorPresetService => this.GetService<IFlairCustomColorPresetService>();

        private LongPressCellView _currentLongPressCell; // contains the index
        private GradientColorUiData _currentLongPressColorData;
        public GradientColorUiData CurrentLongPressColorData
        {
            get => _currentLongPressColorData;
            set => _currentLongPressColorData = value;
        }

        public GradientColorUiData PreviousPresetColor { get; private set; }

        /// <summary> Index of the current UI flair icon that is being long pressed. </summary>
        public int CurrentLongPressIndex => _currentLongPressCell != null ? _currentLongPressCell.Index : -1;

        // controls which preset group to fetch (Eyebrow/Eyelash)
        public ColorPresetType ColorPresetType { get; private set; }
        public FlairAssetType   FlairAssetType   { get; private set; }

        private List<string> _customIds = new List<string>();

        // Swatch shader (consistent with Skin/Hair refactor)
        protected virtual Shader _ColorShader => Shader.Find("Genies/ColorPresetIcon");
        private const float Border = -1.81f;
        private static readonly int s_border     = Shader.PropertyToID("_Border");
        private static readonly int s_innerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int s_midColor   = Shader.PropertyToID("_MidColor");

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                var config = UIDataProviderConfigs.FlairEyebrowColorPresetsConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        public override void StartCustomization()
        {
            // Optionally capture the current equipped preset as "previous"
            // We can resolve it lazily later when needed.
            PreviousPresetColor = null;
        }

        public void SetCategory(FlairAssetType flairCategory)
        {
            FlairAssetType = flairCategory;
            switch (FlairAssetType)
            {
                case FlairAssetType.Eyebrows:
                    ColorPresetType = ColorPresetType.FlairEyebrow;
                    _uiProvider = new InventoryUIDataProvider<ColoredInventoryAsset, GradientColorUiData>(
                        UIDataProviderConfigs.FlairEyebrowColorPresetsConfig,
                        ServiceManager.Get<IAssetsService>()
                    );
                    break;
                case FlairAssetType.Eyelashes:
                    ColorPresetType = ColorPresetType.FlairEyelash;
                    _uiProvider = new InventoryUIDataProvider<ColoredInventoryAsset, GradientColorUiData>(
                        UIDataProviderConfigs.FlairEyelashColorPresetsConfig,
                        ServiceManager.Get<IAssetsService>()
                    );
                    break;
                default:
                    CrashReporter.LogError($"Invalid Category {flairCategory}");
                    break;
            }
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStopped);
        }

        public override ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(
                ctaType: CTAButtonType.SingleCreateNewCTA,
                horizontalLayoutCtaOverride: _Cta,
                createNewAction: OnCreateNew);
        }

        private void OnCreateNew()
        {
            CurrentCustomColorViewState = CustomColorViewState.CreateNew;

            // Store previous color for discarding
            PreviousPresetColor = CurrentLongPressColorData;

            _customizer.GoToCreateItemNode();
        }

        public override ItemPickerLayoutConfig GetLayoutConfig()
        {
            return new ItemPickerLayoutConfig()
            {
                horizontalOrVerticalLayoutConfig =
                    new HorizontalOrVerticalLayoutConfig()
                    {
                        padding = new RectOffset(16, 16, 28, 28),
                        spacing = 12,
                    },
                gridLayoutConfig = new GridLayoutConfig()
                {
                    cellSize = new Vector2(56, 56),
                    columnCount = 5,
                    padding = new RectOffset(16, 16, 24, 8),
                    spacing = new Vector2(16, 16),
                },
            };
        }

        /// <summary>
        /// Selected index = currently equipped asset's index in _ids.
        /// </summary>
        public override int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        public override async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            var typeString = FlairAssetType switch
            {
                FlairAssetType.Eyebrows => Models.ColorPresetType.FlairEyebrow.ToString().ToLower(),
                FlairAssetType.Eyelashes => Models.ColorPresetType.FlairEyelash.ToString().ToLower(),
                _ => throw new ArgumentOutOfRangeException()
            };

            try
            {
                // IMPORTANT: fetch IDs by the configured ColorPresetType (Eyebrow/Eyelash)
                SetCategory(FlairAssetType); // Creates instance of _uiProvider
                _ids = await GetUIProvider<ColoredInventoryAsset, GradientColorUiData>().GetAllAssetIds(category: typeString, pageSize: InventoryConstants.DefaultPageSize)
                    .AttachExternalCancellation(cancellationToken);

                // Get custom colors - use the material slot names that the flair service expects
                var flairCategoryString = FlairAssetType switch
                {
                    FlairAssetType.Eyebrows => UnifiedMaterialSlot.Eyebrows,
                    FlairAssetType.Eyelashes => UnifiedMaterialSlot.Eyelashes,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var customColors = await _flairCustomColorPresetService
                    .TryGetAllCustomColorsByCategory(flairCategoryString)
                    .AttachExternalCancellation(cancellationToken);



                _customIds ??= new();
                _customIds.Clear();

                foreach (var color in customColors)
                {
                    _customIds.Add(color.Id);
                }
            }
            catch (OperationCanceledException)
            {
                _ids = new();
                _customIds = new();
            }

            // Make ordered list of custom colors followed by presets
            List<string> orderedList = _customIds.ToList();
            orderedList.AddRange(_ids);
            _ids = orderedList;

            return _ids.Count;
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        public async UniTask<Ref<GradientColorUiData>> GetDataForIndexAsync(int index)
        {
            if (TryGetLoadedData<GradientColorUiData>(index, out var data))
            {
                return data;
            }

            if (_ids is null)
            {
                return default;
            }

            string id = null;

            if (index < 0 || index >= _ids?.Count)
            {
                Ref<GradientColorUiData> emptyTempRef = CreateRef.From<GradientColorUiData>(null);
                return emptyTempRef;
            }

            id = _ids?[index];
            GradientColorUiData uiData;
            if (_customIds !=  null && _customIds.Contains(id))
            {
                try
                {
                    // Try to load custom flair color data
                    var customColorData = await _flairCustomColorPresetService.GetCustomColorById(id);

                    if (customColorData?.Colors is { Length: >= 4 })
                    {
                        uiData = new GradientColorUiData(
                            assetId: id,
                            displayName: null,
                            category: null,
                            subCategory: null,
                            order: 0,
                            isEditable: true,
                            colorBase: customColorData.Colors[0],
                            colorR: customColorData.Colors[1],
                            colorG: customColorData.Colors[2],
                            colorB: customColorData.Colors[3]
                        );
                    }
                    else
                    {
                        throw new Exception("Custom flair color not found or invalid");
                    }
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"Failed to load custom flair color data for ID {id}: {ex.Message}");

                    uiData = new GradientColorUiData(
                        assetId: id,
                        displayName: null,
                        category: null,
                        subCategory: null,
                        order: 0,
                        isEditable: false,
                        colorBase: Color.black,
                        colorR: Color.black,
                        colorG: Color.black,
                        colorB: Color.black
                    );
                }
            }
            else
            {
                uiData = await GetUIProvider<ColoredInventoryAsset, GradientColorUiData>().GetDataForAssetId(id);
            }

            var newDataRef = CreateRef.FromDependentResource(uiData);

            _loadedData ??= new();
            _loadedData[index] = newDataRef;

            return newDataRef;
        }

        /// <summary>
        /// Business logic for what happens when a cell is clicked.
        /// </summary>
        public override async UniTask<bool> OnItemClickedAsync(
            int index,
            ItemPickerCellView clickedCell,
            bool wasSelected,
            CancellationToken cancellationToken)
        {
            // Load the ui data.
            if (TryGetLoadedData<GradientColorUiData>(index, out var dataRef) is false)
            {
                return false;
            }

            if (!dataRef.IsAlive)
            {
                return false;
            }

            var longPressCellView = clickedCell as LongPressCellView;

            if (longPressCellView != null && _editOrDeleteController.IsActive)
            {
                // If the edit and delete buttons are present, disable them
                _editOrDeleteController.DisableAndDeactivateButtons().Forget();
            }

            if (wasSelected)
            {
                OnLongPress(longPressCellView);
                return true;
            }

            // Update selection and index for the clicked cell.
            clickedCell.ToggleSelected(true);
            clickedCell.Index = index;

            _currentLongPressColorData = dataRef.Item;

            // Map preset colors -> flair channels (Base, R, G, B)
            var colors  = SafeGetColorsArray(dataRef.Item);
            var entries = MapToFlairColors(colors, FlairAssetType);

            ICommand command = new SetNativeAvatarColorsCommand(entries, CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _customizer.RegisterCommand(command);

            var props = new AnalyticProperties();
            props.AddProperty("flairAssetName", dataRef.Item.AssetId);
            AnalyticsReporter.LogEvent(
                FlairCustomizationController.AnalyticsEventsPerFlairType[FlairAssetType][FlairCustomizationController.AnalyticsActionType.ColorPresetSelected],
                props);

            return true;
        }

        /// <summary>
        /// Initialize the cell view when its visible.
        /// </summary>
        public override async UniTask<bool> InitializeCellViewAsync(
            ItemPickerCellView view,
            int index,
            bool isSelected,
            CancellationToken cancellationToken)
        {
            var dataRef = await GetDataForIndexAsync(index);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var longPressCellView = view as LongPressCellView;
            if (longPressCellView != null && dataRef.IsAlive && dataRef.Item != null)
            {
                var colorUiData = dataRef.Item;

                // Only allow long-press for custom colors (editable)
                if (colorUiData.IsEditable)
                {
                    if (longPressCellView.OnLongPress == null)
                    {
                        longPressCellView.OnLongPress += OnLongPress;
                    }

                    if (longPressCellView.Index < 0)
                    {
                        longPressCellView.Index = index;
                    }
                }

                // Use the same swatch icon shader approach as skin/hair (show base color)
                var colors = SafeGetColorsArray(colorUiData);
                longPressCellView.thumbnail.material = GetSwatchMaterial(colors[0]);

                // Equipped check must be by asset id (not hair list)
                var isEquipped = CurrentCustomizableAvatar.IsAssetEquipped(colorUiData.AssetId);

                // Since we use long press to edit, hide the editable icon if equipped
                longPressCellView.SetShowEditableIcon(colorUiData.IsEditable && !isEquipped);
                longPressCellView.SetDebuggingAssetLabel(colorUiData.AssetId);

                if (isSelected)
                {
                    _currentLongPressColorData = colorUiData;
                }
            }

            return true;
        }

        private async void OnLongPress(LongPressCellView longPressCellView)
        {
            if (_currentLongPressCell == longPressCellView && _editOrDeleteController.IsActive)
            {
                return;
            }

            if (longPressCellView == null || longPressCellView.Index < 0)
            {
                return;
            }

            // Get the current data of the cell that is being long pressed
            var longPressColorDataRef = await GetDataForIndexAsync(longPressCellView.Index);

            if (longPressColorDataRef.Item == null)
            {
                return;
            }

            // Only editable for custom colors
            if (!longPressColorDataRef.Item.IsEditable)
            {
                return;
            }

            _currentLongPressCell  = longPressCellView;
            _currentLongPressColorData = longPressColorDataRef.Item;

            await _editOrDeleteController.Enable(_currentLongPressCell.gameObject);
        }

        /// <summary>
        /// Dispose the controller.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _currentLongPressColorData = null;
            _customIds?.Clear();
            _customIds = null;
        }

        // ===== Helpers =====

        public static Color[] SafeGetColorsArray(GradientColorUiData uiData)
        {
            // GradientColorUiData has 4 color properties: ColorBase, ColorR, ColorG, ColorB
            if (uiData == null)
            {
                return new[] { Color.black, Color.black, Color.black, Color.black };
            }

            return uiData.GetColorsArray();
        }

        public static GenieColorEntry[] MapToFlairColors(Color[] colors, FlairAssetType flairAssetType)
        {
            switch (flairAssetType)
            {
                case FlairAssetType.Eyebrows:
                    return new[]
                    {
                        new GenieColorEntry(GenieColor.EyebrowsBase, colors[0]),
                        new GenieColorEntry(GenieColor.EyebrowsR,    colors[1]),
                        new GenieColorEntry(GenieColor.EyebrowsG,    colors[2]),
                        new GenieColorEntry(GenieColor.EyebrowsB,    colors[3]),
                    };

                case FlairAssetType.Eyelashes:
                    return new[]
                    {
                        new GenieColorEntry(GenieColor.EyelashesBase, colors[0]),
                        new GenieColorEntry(GenieColor.EyelashesR,    colors[1]),
                        new GenieColorEntry(GenieColor.EyelashesG,    colors[2]),
                        new GenieColorEntry(GenieColor.EyelashesB,    colors[3]),
                    };

                case FlairAssetType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(flairAssetType), flairAssetType, null);
            }
        }

        private Material GetSwatchMaterial(Color color)
        {
            var iconMaterial = new Material(_ColorShader);

            var mainColor = color;
            mainColor.a = 1f;

            iconMaterial.SetFloat(s_border, Border);
            iconMaterial.SetColor(s_innerColor, mainColor);
            iconMaterial.SetColor(s_midColor,   Color.white);

            return iconMaterial;
        }
    }
}
