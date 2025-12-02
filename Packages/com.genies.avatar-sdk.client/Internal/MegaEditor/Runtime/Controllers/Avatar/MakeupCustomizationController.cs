using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars.Behaviors;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.MakeupPresets;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using Genies.Utilities;
using Genies.Utilities.Internal;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Handles customizing the avatar makeup (combined controller + item picker data source).
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MakeupCustomizationController", menuName = "Genies/Customizer/Controllers/Makeup Customization Controller")]
#endif
    public class MakeupCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        [SerializeField] private MakeupPresetCategory _subcategory;

        private string _subcategoryString;

        private Dictionary<int, Ref<GradientColorUiData>> _loadedColorData;
        private InventoryUIDataProvider<ColoredInventoryAsset, GradientColorUiData> _colorUIProvider;

        private string _lastSelectedMakeup = "None";

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultAvatarMakeupConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _loadedData = new();
            _subcategoryString = _subcategory.ToString().ToLower();
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(
                _RootTransactionName, nameof(MakeupCustomizationController), $"open makeup - {_subcategoryString} category");

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.MakeupCustomizationStarted);

            ActivateCamera();
            ShowPrimaryPicker(this);
        }

        public override void StopCustomization()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.MakeupCustomizationStopped);

            ResetCamera();
            HidePrimaryPicker();
        }

        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            RefreshPrimaryPickerSelection();
        }

        // ---------- IItemPickerDataSource ----------

        /// <summary> Index of the currently equipped id in this subcategory. </summary>
        public int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        /// <summary> Load IDs for this subcategory. </summary>
        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return await LoadMoreItemsBaseAsync(cancellationToken, _subcategoryString, null);
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return await InitializeAndGetCountBaseAsync(cancellationToken, _subcategoryString);
        }

        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "MakeupCustomizationController");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(
                ctaType: CTAButtonType.SingleNoneCTA,
                noneSelectedDelegate: NoneSelectedAsync);
        }

        private async UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);

            var props = new AnalyticProperties();
            props.AddProperty("LastSelectedMakeup", _lastSelectedMakeup);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoMakeupSelected, props);

            if (_ids == null || _ids.Count == 0)
            {
                return false;
            }

            string equippedId = _ids.FirstOrDefault(CurrentCustomizableAvatar.IsAssetEquipped);
            if (string.IsNullOrEmpty(equippedId))
            {
                return false;
            }

            var unequipCmd = new UnequipNativeAvatarAssetCommand(equippedId, CurrentCustomizableAvatar);
            await unequipCmd.ExecuteAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _customizer.RegisterCommand(unequipCmd);
            return true;
            }

        /// <summary>
        /// Equip when clicked; if already selected and editable, go to edit.
        /// </summary>
        public async UniTask<bool> OnItemClickedAsync(
            int index,
            ItemPickerCellView clickedCell,
            bool wasSelected,
            CancellationToken cancellationToken)
        {
            var dataRef = await GetDataForIndexAsync(index);
            var data = dataRef.Item;
            if (data == null)
            {
                return false;
            }

            // Performance span per selected item
            string currentSpan = _InstrumentationManager.StartChildSpanUnderSpan(
                _categorySpan, data.AssetId, $"{_subcategoryString} asset id");
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _previousSpan = currentSpan;

            if (wasSelected && data.IsEditable)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            var command = new EquipNativeAvatarAssetCommand(data.AssetId, CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);
            _lastSelectedMakeup = data.DisplayName;

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            // Analytics
            var props = new AnalyticProperties();
            props.AddProperty("name",   data.AssetId);
            props.AddProperty("action", "EquipItem");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.MakeupPresetClickEvent, props);

            _customizer.RegisterCommand(command);
            return true;
        }

        /// <summary>
        /// Initialize the cell view when it becomes visible.
        /// </summary>
        public async UniTask<bool> InitializeCellViewAsync(
            ItemPickerCellView view,
            int index,
            bool isSelected,
            CancellationToken cancellationToken)
        {

            return await InitializeCellViewBaseAsync<BasicInventoryUiData>(view, index, isSelected, cancellationToken);
        }

        /// <summary>
        /// Dispose: clear sprite cache and release refs.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _categorySpan = null;
            _previousSpan = null;
        }
    }
}
