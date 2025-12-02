using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
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
using Genies.Ugc.CustomHair;
using Genies.UI.Widgets;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Data source for the hair editing view and controls. Uses GradientColorUiData from the inventory system.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "HairColorItemPickerDataSource", menuName = "Genies/Customizer/DataSource/HairColorItemPickerDataSource")]
#endif
    public class HairColorItemPickerDataSource : CustomizationItemPickerDataSource
    {
        [SerializeField] private NoneOrNewCTAController _Cta;

        // Analytics
        private readonly string _colorAnalyticsEventName = CustomizationAnalyticsEvents.HairColorPresetClickEvent;

        /// <summary> Hair service only used to know which ids are presets vs custom (for edit/delete gating) </summary>
        private HairColorService _HairColorService => this.GetService<HairColorService>();


        private LongPressCellView _currentLongPressCell; // contains the index
        private GradientColorUiData _currentLongPressColorData;
        public GradientColorUiData CurrentLongPressColorData => _currentLongPressColorData;

        /// <summary> Gets the previous hair color Id for customizing logic (discard). </summary>
        public string PreviousHairColorId;

        /// <summary> Gets the current hair color Id for customizing logic. </summary>
        public string CurrentHairColorId { get; set; }

        /// <summary> Index of the current UI hair icon being long pressed. </summary>
        public int CurrentLongPressIndex => _currentLongPressCell != null ? _currentLongPressCell.Index : -1;

        public List<string> CustomIds;
        private List<string> _presetIds;

        // Material/thumbnail shader (same style as skin)
        protected virtual Shader _ColorShader => Shader.Find("Genies/ColorPresetIcon");
        private const float Border = -1.81f;
        private static readonly int s_border     = Shader.PropertyToID("_Border");
        private static readonly int s_innerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int s_midColor   = Shader.PropertyToID("_MidColor");

        private CancellationTokenSource _cts;

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                var config = UIDataProviderConfigs.HairColorPresetsConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        protected override string GetAssetTypeString()
        {
            return ColorPresetType.Hair.ToString().ToLower();
        }

        protected override async UniTask<List<string>> GetCustomIdsAsync(CancellationToken token)
        {
            CustomIds = await _HairColorService.GetAllCustomHairIdsAsync();
            return CustomIds;
        }

        protected override async UniTask<List<string>> GetPresetIdsAsync(CancellationToken token)
        {
            if (_uiProvider == null)
            {
                _presetIds = new List<string>();
                return _presetIds;
            }
            _presetIds = await _uiProvider.GetAllAssetIds(category: GetAssetTypeString(), pageSize: InventoryConstants.DefaultPageSize) ?? new List<string>();
            return _presetIds;
        }

        public override void StartCustomization()
        {
            PreviousHairColorId = CurrentHairColorId;
            CurrentHairColorId = GetEquippedColorId();
            if (CurrentHairColorId == null)
            {
                CurrentHairColorId = PreviousHairColorId;
            }

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStarted);
        }

        private string GetEquippedColorId()
        {
            if (_ids == null)
            {
                return null;
            }

            // Match by equipped asset id (consistent with SkinColorItemPickerDataSource)
            for (var i = 0; i < _ids.Count; i++)
            {
                if (CurrentCustomizableAvatar.IsAssetEquipped(_ids[i]))
                {
                    return _ids[i];
                }
            }
            return null;
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
            Dispose();
            CurrentCustomColorViewState = CustomColorViewState.CreateNew;

            // Store previous color for discarding
            PreviousHairColorId = CurrentHairColorId;

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
        /// Gets which hair color is selected in the UI (by equipped asset id).
        /// </summary>
        public override int GetCurrentSelectedIndex()
        {
            if (_ids == null)
            {
                return -1;
            }

            var equippedId = GetEquippedColorId();
            if (string.IsNullOrEmpty(equippedId))
            {
                return -1;
            }

            for (var i = 0; i < _ids.Count; i++)
            {
                if (_ids[i] == equippedId)
                {
                    return i;
                }
            }
            return -1;
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

            if (index < 0 || _ids == null || index >= _ids.Count)
            {
                return default;
            }

            var id = _ids[index];
            GradientColorUiData uiData;

            if (CustomIds != null && CustomIds.Contains(id))
            {
                try
                {
                    // Try to load custom hair color data
                    var customColorData = await _HairColorService.CustomColorDataAsync(id);

                    if (customColorData != null)
                    {
                        uiData = new GradientColorUiData(
                            assetId: id,
                            displayName: null,
                            category: null,
                            subCategory: null,
                            order: 0,
                            isEditable: true,
                            colorBase: customColorData.ColorBase,
                            colorR: customColorData.ColorR,
                            colorG: customColorData.ColorG,
                            colorB: customColorData.ColorB
                        );
                    }
                    else
                    {
                        throw new Exception("Custom hair color data was null");
                    }
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"Failed to load custom hair color data for ID {id}: {ex.Message}");

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

            if (!dataRef.IsAlive || dataRef.Item == null)
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

            // Store current customizable hair color id (asset id from CMS)
            CurrentHairColorId = dataRef.Item.AssetId;

            // Map preset colors -> hair channels (Base, R, G, B)
            var colors = SafeGetColorsArray(dataRef.Item);
            var entries = MapToHairColors(colors);

            // Update avatar hair colors
            ICommand command = new SetNativeAvatarColorsCommand(entries, CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var props = new AnalyticProperties();
            props.AddProperty("name", $"{dataRef.Item.DisplayName}");
            AnalyticsReporter.LogEvent(_colorAnalyticsEventName, props);

            _customizer.RegisterCommand(command);
            return true;
        }

        /// <summary>
        /// Initialize the cell view when it's visible.
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
                if (longPressCellView.OnLongPress == null)
                {
                    longPressCellView.OnLongPress += OnLongPress;
                    if (longPressCellView.Index < 0)
                    {
                        longPressCellView.Index = index;
                    }
                }

                var uiData = dataRef.Item;
                if (uiData == null)
                {
                    return false;
                }

                // Use the same icon shader approach as skin; show base color as swatch
                var colors = SafeGetColorsArray(uiData);
                longPressCellView.thumbnail.material = GetSwatchMaterial(colors[0]);

                var isEquipped = EquippedHairColorIds.Contains(uiData.AssetId);
                // Since we use long press to edit, we hide the editable icon on the view
                longPressCellView.SetShowEditableIcon(uiData.IsEditable && !isEquipped);
                longPressCellView.SetDebuggingAssetLabel(uiData.AssetId);
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

            _currentLongPressCell = longPressCellView;

            // Get the current data of the cell that is being long pressed
            var longPressColorDataRef = await GetDataForIndexAsync(_currentLongPressCell.Index);
            _currentLongPressColorData = longPressColorDataRef.Item;

            if (_currentLongPressColorData == null)
            {
                return;
            }

            // Return if long pressed Hair Color is a preset (non-custom) — same behavior as before
            if (_presetIds != null && _presetIds.Contains(_currentLongPressColorData.AssetId))
            {
                return;
            }

            // Return if Hair Color is currently equipped somewhere (by asset id)
            if (EquippedHairColorIds.Contains(_currentLongPressColorData.AssetId))
            {
                return;
            }

            //// Return if long pressed Hair is not editable
            if (!_currentLongPressColorData.IsEditable)
            {
                return;
            }

            // Enable Edit
            // Track which asset id is subject to edit/delete
            CurrentHairColorId = _currentLongPressColorData.AssetId;

            await _editOrDeleteController.Enable(_currentLongPressCell.gameObject);
        }

        // ===== Helpers to map preset colors to hair materials =====

        public static Color[] SafeGetColorsArray(GradientColorUiData uiData)
        {
            // GradientColorUiData has 4 color properties: ColorBase, ColorR, ColorG, ColorB
            if (uiData == null)
            {
                return new[] { Color.black, Color.black, Color.black, Color.black };
            }

            return uiData.GetColorsArray();
        }

        public static GenieColorEntry[] MapToHairColors(Color[] colors)
        {
            // Expected order: [0]=Base, [1]=R, [2]=G, [3]=B
            return new[]
            {
                new GenieColorEntry(GenieColor.HairBase, colors[0]),
                new GenieColorEntry(GenieColor.HairR,    colors[1]),
                new GenieColorEntry(GenieColor.HairG,    colors[2]),
                new GenieColorEntry(GenieColor.HairB,    colors[3]),
            };
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

        public override void Dispose()
        {
            base.Dispose();

            CustomIds?.Clear();
            CustomIds = null;
            _presetIds?.Clear();
            _presetIds = null;
        }
    }
}
