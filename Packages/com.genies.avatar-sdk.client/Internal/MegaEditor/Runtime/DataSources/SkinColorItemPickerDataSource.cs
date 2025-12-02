using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Refs;
using Genies.Ugc;
using Genies.UI.Widgets;
using Genies.Models;
using Genies.ServiceManagement;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SkinColorItemPickerDataSource", menuName = "Genies/Customizer/DataSource/SkinColorItemPickerDataSource")]
#endif
    public class SkinColorItemPickerDataSource : CustomizationItemPickerDataSource
    {
        [SerializeField]
        private NoneOrNewCTAController _Cta;

        /// <summary>
        /// The event name to dispatch to analytics
        /// </summary>
        private const string _colorAnalyticsEventName = CustomizationAnalyticsEvents.SkinColorPresetClickEvent;

        private LongPressCellView _currentLongPressCell; // contains the index
        private SimpleColorUiData _currentLongPressColorData;

        protected virtual Shader _ColorShader => Shader.Find("Genies/ColorPresetIcon");
        public SimpleColorUiData CurrentLongPressColorData => _currentLongPressColorData;

        /// <summary>
        /// Default Skin Color loaded from ColorAsset with the Id to be <see cref="UnifiedDefaults.DefaultSkinColor"/>
        /// </summary>
        private SkinColorData _defaultSkinColorData;
        public SkinColorData PreviousSkinColorData { get; private set; }
        public SkinColorData CurrentSkinColorData { get; set; }
        public int CurrentLongPressIndex => _currentLongPressCell != null ? _currentLongPressCell.Index : -1;

        // Material properties for the color shader
        private const float Border = -1.81f;
        private static readonly int s_border = Shader.PropertyToID("_Border");
        private static readonly int s_innerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int s_midColor = Shader.PropertyToID("_MidColor");

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                var config = UIDataProviderConfigs.SkinColorPresetsConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        public override void StartCustomization()
        {
            CurrentSkinColorData = ColorAssetToSkinColorData(CurrentCustomizableAvatar.GetColor(GenieColor.Skin) ?? Color.black);
            PreviousSkinColorData = CurrentSkinColorData;

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStarted);
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStopped);
        }

        public override ItemPickerCtaConfig GetCtaConfig()
        {
            return null;
        }

        public override ItemPickerLayoutConfig GetLayoutConfig()
        {
            return new ItemPickerLayoutConfig()
            {
                horizontalOrVerticalLayoutConfig = new HorizontalOrVerticalLayoutConfig() { padding = new RectOffset(16,                  16, 28, 28), spacing = 12, },
                gridLayoutConfig = new GridLayoutConfig() { cellSize = new Vector2(56, 56), columnCount = 5, padding = new RectOffset(16, 16, 24, 8), spacing = new Vector2(16, 16), },
            };
        }

        /// <summary>
        /// Gets which skin color UI is selected.
        /// </summary>
        /// <remarks>This override uses "Skin.CurrentColor.Id" to determine which skin color UI is selected.
        /// So if we want to "set selection" we will need to set the current equipped color on the avatar to
        /// match the id of UiColorData, then this method will return the index based on the matching.
        /// "CurrentCustomizableAvatar.Materials.IsAssetEquipped" used in other implementations didn't work
        /// (maybe because custom color is not assigned with an asset id during customization)</remarks>
        /// <returns>the index of the UI item. -1 if none is selected.</returns>
        public override int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        public async UniTask<Ref<SimpleColorUiData>> GetDataForIndexAsync(int index)
        {
            // Ensure _ids is populated (normally happens in InitializeAndGetCountAsync)
            _ids ??= await GetUIProvider<ColoredInventoryAsset, SimpleColorUiData>().GetAllAssetIds(
                category: ColorPresetType.Skin.ToString().ToLower(),
                pageSize: InventoryConstants.DefaultPageSize);

            return await GetDataForIndexBaseAsync<ColoredInventoryAsset, SimpleColorUiData>(index, "SkinColorItemPicker");
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
            if (TryGetLoadedData<SimpleColorUiData>(index, out var dataRef) is false)
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
                //If the edit and delete buttons are present, disable them
                _editOrDeleteController.DisableAndDeactivateButtons().Forget();
            }

            // Update selection and index for the clicked cell.
            clickedCell.ToggleSelected(true);
            clickedCell.Index = index;

            CurrentSkinColorData = new SkinColorData { BaseColor = dataRef.Item.InnerColor };

            // Update avatar skin color
            ICommand command = new EquipSkinColorCommand(dataRef.Item.InnerColor, CurrentCustomizableAvatar);
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
        /// Initialize the cell view when its visible.
        /// </summary>
        /// <param name="view"> The view to initialize </param>
        /// <param name="index"> Cell index </param>
        /// <param name="isSelected"> If its already selected </param>
        /// <param name="cancellationToken"> The cancellation token </param>
        /// <returns></returns>
        public override async UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            Ref<SimpleColorUiData> dataRef = await GetDataForIndexAsync(index);

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

                var colorPresetUiData = dataRef.Item;
                if (colorPresetUiData == null)
                {
                    // maybe add current equipped color data to the material
                    return false;
                }

                //longPressCellView.thumbnail.material = colorPresetUiData.GetIconMaterial();
                longPressCellView.thumbnail.material = GetMaterial(colorPresetUiData.InnerColor);

                var isEquipped = EquippedSkinColorIds.Contains(colorPresetUiData.AssetId);

                // since we use long press to edit, we hide the editable icon on the view
                longPressCellView.SetShowEditableIcon(colorPresetUiData.IsEditable && !isEquipped);
                longPressCellView.SetDebuggingAssetLabel(dataRef.Item.AssetId);
            }

            return true;
        }

        private async void OnLongPress(LongPressCellView longPressCellView)
        {
            if (_currentLongPressCell == longPressCellView && _editOrDeleteController.IsActive)
            {
                return;
            }

            if (longPressCellView.Index < 0)
            {
                return;
            }

            _currentLongPressCell = longPressCellView;

            // Get the current data of the cell that is being long pressed
            Ref<SimpleColorUiData> longPressColorDataRef = await GetDataForIndexAsync(_currentLongPressCell.Index);
            _currentLongPressColorData = longPressColorDataRef.Item;

            // Check if the current skin color is equipped
            if (EquippedSkinColorIds.Contains(_currentLongPressColorData.AssetId))
            {
                Debug.Log("Return because the skin color is equipped!");
                return;
            }

            if (!_currentLongPressColorData.IsEditable)
            {
                return;
            }

            // Enable Edit
            // Set the current customizable skin color in the global service
            if (_currentLongPressColorData?.InnerColor != null)
            {
                CurrentSkinColorData = new SkinColorData { BaseColor = _currentLongPressColorData.InnerColor };
            }

            await _editOrDeleteController.Enable(_currentLongPressCell.gameObject);
        }

        private static SkinColorData ColorAssetToSkinColorData(Color color)
        {
            return new SkinColorData(){ BaseColor = color,};
        }

        private Material GetMaterial(Color color)
        {
            Material iconMaterial = new Material(_ColorShader);

            var mainColor = color;
            mainColor.a = 1f;

            iconMaterial.SetFloat(s_border, Border);
            iconMaterial.SetColor(s_innerColor, mainColor);
            iconMaterial.SetColor(s_midColor,   Color.white);

            return iconMaterial;
        }
    }
}
