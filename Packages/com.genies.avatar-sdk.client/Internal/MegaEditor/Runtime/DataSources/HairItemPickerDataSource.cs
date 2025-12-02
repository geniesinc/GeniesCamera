using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.CrashReporting;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "HairItemPickerDataSource", menuName = "Genies/Customizer/DataSource/HairItemPickerDataSource")]
#endif
    public class HairItemPickerDataSource : CustomizationItemPickerDataSource
    {
        [SerializeField]
        private HairColorItemPickerDataSource hairColorDataSource;

        /// <summary>
        /// Allows overriding layout config from <see cref="HairCustomizationController"/> with OverrideWithCustomLayoutConfig
        /// </summary>
        private ItemPickerLayoutConfig _overrideLayoutConfig;

        private const string _subcategoryString = "hair";
        private bool _isHairEquipped;
        public bool IsHairEquipped => _isHairEquipped;

        public event Action<string> OnItemClicked;
        public event Action OnNoneSelected;

        // Pagination support
        public override bool HasMoreItems => _uiProvider?.HasMoreData ?? false;
        public override bool IsLoadingMore => _uiProvider?.IsLoadingMore ?? false;

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                var config = UIDataProviderConfigs.DefaultWearablesConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        protected override string GetAssetTypeString()
        {
            return "hair";
        }

        protected override void OnAfterInitialized()
        {
            _overrideLayoutConfig = new ItemPickerLayoutConfig()
            {
                horizontalOrVerticalLayoutConfig = new HorizontalOrVerticalLayoutConfig() { padding = new RectOffset(16, 16, 16, 16), spacing = 8 },
                gridLayoutConfig = new GridLayoutConfig() { cellSize = new Vector2(88, 96), columnCount = 4, padding = new RectOffset(16, 16, 24, 8), spacing = new Vector2(16, 16) }
            };

            _isHairEquipped = IsHairItemEquipped();
            _defaultCellSize = new Vector2(88, 96);
        }

        public override void StartCustomization()
        {
        }

        public override void StopCustomization()
        {
        }

        public void OverrideWithCustomLayoutConfig(ItemPickerLayoutConfig layoutConfig)
        {
            _overrideLayoutConfig = layoutConfig;
        }

        public override ItemPickerLayoutConfig GetLayoutConfig()
        {
            return _overrideLayoutConfig;
        }

        /// <summary>
        /// Return which outfit asset index is currently selected
        /// </summary>
        /// <returns></returns>
        public override int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        /// <summary>
        /// Loads more items for pagination
        /// </summary>
        /// <returns>True if more items were loaded successfully</returns>
        public override async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            if (_uiProvider == null || !HasMoreItems || IsLoadingMore)
            {
                return false;
            }

            try
            {
                if (_uiProvider == null)
                {
                    return false;
                }

                // Load more data from the provider
                var newItemsList = await _uiProvider.LoadMoreAsync(category: null, subcategory: "hair");
                var newItems = newItemsList?.Cast<BasicInventoryUiData>().ToList() ?? new List<BasicInventoryUiData>();

                if (newItems == null || newItems.Count == 0)
                {
                    return false;
                }

                // Get updated IDs list
                _ids = await _uiProvider.GetAllAssetIds(category: "hair", pageSize: InventoryConstants.DefaultPageSize) ?? new List<string>();

                return true;
            }
            catch (Exception e)
            {
                CrashReporter.Log($"HairItemPicker's LoadMoreItemsAsync failed: {e}", LogSeverity.Error);
                return false;
            }
        }


        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<ColorTaggedInventoryAsset, BasicInventoryUiData>(index, "HairItemPicker");
        }

        public override ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.SingleNoneCTA, noneSelectedDelegate: NoneSelectedAsync);
        }

        private async UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            OnNoneSelected?.Invoke();
            var currentRef = await GetDataForIndexAsync(GetCurrentSelectedIndex());

            if (currentRef.Item == null)
            {
                return false;
            }

            var props = new AnalyticProperties();
            props.AddProperty("AssetId", currentRef.Item.AssetId);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoOutfitSelected, props);

            if (!currentRef.IsAlive || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var unequipCmd = new UnequipNativeAvatarAssetCommand(currentRef.Item.AssetId, CurrentCustomizableAvatar);
            await unequipCmd.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _customizer.RegisterCommand(unequipCmd);

            _isHairEquipped = false;
            _customizer.View.SecondaryItemPicker.Hide();
            hairColorDataSource.StopCustomization();

            return true;
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
            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return false;
            }

            OnItemClicked?.Invoke(data.Item.AssetId);

            //Create command for equipping outfit asset
            var command = new EquipNativeAvatarAssetCommand(data.Item.AssetId, CurrentCustomizableAvatar);

            //Execute the command
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Analytics
            var props = new AnalyticProperties();
            props.AddProperty("name", $"{data.Item.AssetId}");
            props.AddProperty("subcategory", $"{_subcategoryString}");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.AssetClickEvent, props);

            //Register the command for undo/redo
            _customizer.RegisterCommand(command);

            _isHairEquipped = true;
            hairColorDataSource.Initialize(_customizer);
            hairColorDataSource.StartCustomization();
            _customizer.View.SecondaryItemPicker.Show(hairColorDataSource).Forget();

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
            bool result = await InitializeCellViewBaseAsync<ColorTaggedInventoryAsset, BasicInventoryUiData>(view, index, cancellationToken);

            var asGeneric = view as GenericItemPickerCellView;
            asGeneric?.SetIsEditable(false);

            return result;
        }

        private bool IsHairItemEquipped()
        {
            if (_ids is null || _ids.Count == 0)
            {
                return false;
            }

            var equippedIds = CurrentCustomizableAvatar.GetEquippedAssetIds();
            foreach (var assetId in equippedIds)
            {
                if (_ids != null && _ids.Contains(assetId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
