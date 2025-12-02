using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars.Behaviors;
using Genies.Avatars.Services.Flair;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Models;
using Genies.Naf;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Handles customizing the avatar flairs (eyebrows, eyelashes, etc..)
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FlairCustomizationController", menuName = "Genies/Customizer/Controllers/Flair Customization Controller")]
#endif
    public class FlairCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        [SerializeField]
        private List<ExtraItemPickerSettings> _extraItemPickerSettings;

        [SerializeField]
        private string _presetGuidPriority;

        //save the guid to consider as a option that is not available for customize
        [SerializeField]
        private string _nonePresetId;

        [SerializeField]
        private FlairAssetType _flairCategory;

        [SerializeField]
        public FlairColorItemPickerDataSource flairColorDataSource;

        /// <summary>
        /// Connected Chaos customization node's controller
        /// Used to reset all custom vector values when equipped preset has changed
        /// </summary>
        [SerializeField]
        private FaceVectorCustomizationController _chaosCustomizer;
        private string _stringSubcategory;
        private CancellationTokenSource _cancellationTokenSource;

        private IFlairCustomColorPresetService _FlairCustomColorPresetService => this.GetService<IFlairCustomColorPresetService>();

        public static readonly Dictionary<FlairAssetType, Dictionary<AnalyticsActionType, string>> AnalyticsEventsPerFlairType =
        new Dictionary<FlairAssetType, Dictionary<AnalyticsActionType, string>>()
        {
            { FlairAssetType.Eyebrows, new Dictionary<AnalyticsActionType, string>()
            {
                {AnalyticsActionType.EnterCategory, CustomizationAnalyticsEvents.EyeBrowCategorySelected},
                {AnalyticsActionType.PresetSelected, CustomizationAnalyticsEvents.EyeBrowPresetClickEvent},
                {AnalyticsActionType.ColorPresetSelected, CustomizationAnalyticsEvents.EyeBrowColorPresetClickEvent},
                {AnalyticsActionType.ColorPickerSelected, CustomizationAnalyticsEvents.EyeBrowColorPickerClickEvent},
            }},
            { FlairAssetType.Eyelashes, new Dictionary<AnalyticsActionType, string>()
            {
                {AnalyticsActionType.EnterCategory, CustomizationAnalyticsEvents.EyelashCategorySelected},
                {AnalyticsActionType.PresetSelected, CustomizationAnalyticsEvents.EyeLashPresetClickEvent},
                {AnalyticsActionType.ColorPresetSelected, CustomizationAnalyticsEvents.EyeLashColorPresetClickEvent},
                {AnalyticsActionType.ColorPickerSelected, CustomizationAnalyticsEvents.EyeLashColorPickerClickEvent},
            }},
        };

        public enum AnalyticsActionType
        {
            EnterCategory = 1,
            PresetSelected = 2,
            ColorPresetSelected = 3,
            ColorPickerSelected = 4,
        };


        private string _lastSelectedBlendShape = "None";

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;

        public override async UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultAvatarFlairConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _stringSubcategory = _flairCategory.ToString().ToLower();
            _loadedData = new();

            flairColorDataSource.Initialize(_customizer);

            // Initialize _ids collection before StartCustomization is called
            _cancellationTokenSource = new CancellationTokenSource();
            await InitializeAndGetCountAsync(_cancellationTokenSource.Token);

            return true;
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName,
                nameof(FlairCustomizationController), $"open face - {_stringSubcategory} category");

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.FlairCustomizationStarted);
            //Aim the camera at the body area
            ActivateCamera();

            ShowPrimaryPicker(this);

            flairColorDataSource.SetCategory(_flairCategory);
            flairColorDataSource.StartCustomization();
            ShowSecondaryPicker(flairColorDataSource);


            AddListeners();

            var currentAssetEquipped = _ids.FirstOrDefault(CurrentCustomizableAvatar.IsAssetEquipped);
            var props = new AnalyticProperties();

            if(currentAssetEquipped != null)
            {
                props.AddProperty("flairAssetName", $"{currentAssetEquipped}");
            }

            AnalyticsReporter.LogEvent(AnalyticsEventsPerFlairType[_flairCategory][AnalyticsActionType.EnterCategory], props);

        }

        private void AddListeners()
        {
            _customizer.View.EditOrDeleteController.OnEditClicked += EditCustomColorData;
            _customizer.View.EditOrDeleteController.OnDeleteClicked += DeleteCustomColorData;
            _customizer.View.SecondaryItemPicker.OnScroll += CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
        }

        private void RemoveListeners()
        {
            _customizer.View.EditOrDeleteController.OnEditClicked -= EditCustomColorData;
            _customizer.View.EditOrDeleteController.OnDeleteClicked -= DeleteCustomColorData;
            _customizer.View.SecondaryItemPicker.OnScroll -= CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
        }

        private void EditCustomColorData()
        {
            CurrentCustomColorViewState = CustomColorViewState.Edit;
            _customizer.GoToCreateItemNode();
        }

        private async void DeleteCustomColorData()
        {
            var category = flairColorDataSource.FlairAssetType.ToString();
            var deletedDataId = flairColorDataSource.CurrentLongPressColorData?.AssetId;

            // Trigger the animation of closing the edit and delete button and forget.
            _customizer.View.EditOrDeleteController.DisableAndDeactivateButtons().Forget();

            var nextIndexToEquip = flairColorDataSource.CurrentLongPressIndex + 1;
            Ref<GradientColorUiData> nextUiDataRef = await flairColorDataSource.GetDataForIndexAsync(nextIndexToEquip); // this can be sync if the data exists in the cache

            flairColorDataSource.CurrentLongPressColorData = nextUiDataRef.Item;

            ICommand command = new SetNativeAvatarColorsCommand(GetColors(flairColorDataSource), CurrentCustomizableAvatar);

            await command.ExecuteAsync();

            // Delete the data in the backend
            if (deletedDataId != null)
            {
                await _FlairCustomColorPresetService.DeleteCustomFlairColor(deletedDataId, category);

                // Dispose current data source, reload data from backend, and reinitialize
                //refresh the data provider
                //await flairColorDataSource._uiProvider.ReloadAsync();
                flairColorDataSource.Dispose();
                await flairColorDataSource.InitializeAndGetCountAsync(new());

                // Call the picker show the updated view
                ShowSecondaryPicker(flairColorDataSource);
            }
        }

        private static GenieColorEntry[] GetColors(FlairColorItemPickerDataSource source)
        {
            var colors = FlairColorItemPickerDataSource.SafeGetColorsArray(source.CurrentLongPressColorData);
            return FlairColorItemPickerDataSource.MapToFlairColors(colors, source.FlairAssetType);
        }


        public override void StopCustomization()
        {

            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.FlairCustomizationStopped);
            //Aim the camera at the body area
            ResetCamera();

            RemoveListeners();

            _customizer.View.EditOrDeleteController.DeactivateButtonsImmediately();

            HidePrimaryPicker();
            HideSecondaryPicker();

            flairColorDataSource.StopCustomization();

        }

        private void CloseEditOrDeleteButtonsWhenCrossingLeftMargin()
        {
            var editOrDeleteController = _customizer.View.EditOrDeleteController;
            if (editOrDeleteController.IsActive && editOrDeleteController.transform.localPosition.x < -120)
            {
                editOrDeleteController.DeactivateButtonsImmediately();
            }
        }

        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            RefreshPrimaryPickerSelection();
            RefreshSecondaryPickerSelection();
        }

        public int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        public override bool ItemSelectedIsValidForProcessCTA()
        {
            var equippedId = _ids.FirstOrDefault(CurrentCustomizableAvatar.IsAssetEquipped);

            //valid to process a CTA if it's not the none preset option or an unselected item
            return !string.Equals(equippedId, _nonePresetId);
        }

        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            // Use base implementation with reorder logic
            return await LoadMoreItemsBaseAsync(
                cancellationToken,
                _stringSubcategory,
                null,
                ReorderIdsWithPriority
            );
        }

        private List<string> ReorderIdsWithPriority(List<string> ids)
        {
            var orderedList = new List<string>(ids);

            // Only reorder if _presetGuidPriority exists in the original list
            if (!string.IsNullOrEmpty(_presetGuidPriority) && ids.Contains(_presetGuidPriority))
            {
                orderedList.Remove(_presetGuidPriority);
                orderedList.Insert(0, _presetGuidPriority);
            }

            return orderedList;
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            // Use base implementation with reorder logic
            return await InitializeAndGetCountBaseAsync(
                cancellationToken,
                _stringSubcategory,
                null,
                ReorderIdsWithPriority
            );
        }

        public override ItemPickerCellView GetCellPrefab(int index)
        {
            if(_extraItemPickerSettings == null )
            {
                return _defaultCellView;
            }

            ExtraItemPickerSettings item = _extraItemPickerSettings.Find(e => e.StaticIndexOnList == index);
            var prefab = item != null ? item.ExtraItemPicker : _defaultCellView;
            return prefab;
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            // Special handling for extra item picker settings
            if (_extraItemPickerSettings != null && _extraItemPickerSettings.Exists(e=>e.StaticIndexOnList == index))
            {
                //Create a 'null' ref to return w/ 'using' for disposal
                Ref<BasicInventoryUiData> emptyTempRef = CreateRef.From<BasicInventoryUiData>(null);
                return emptyTempRef;
            }

            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "FlairCustomizationController");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            if (_flairCategory != FlairAssetType.Eyelashes)
            {
                return new ItemPickerCtaConfig(ctaType: CTAButtonType.CustomizeCTA, noneSelectedDelegate: CustomizeSelectedAsync);
            }

            return null;
        }

        private UniTask<bool> CustomizeSelectedAsync(CancellationToken cancellationToken)
        {
            switch (_flairCategory)
            {
                case FlairAssetType.Eyebrows:
                    CurrentDnaCustomizationViewState = AvatarBaseCategory.Brow;
                    break;
                default:
                    CrashReporter.LogError($"Invalid Customization Selection {_flairCategory.ToString()}");
                    break;
            }
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ChaosFaceCustomSelectEvent);
            _customizer.GoToEditItemNode();
            return UniTask.FromResult(true);
        }

        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            if (_extraItemPickerSettings != null && _extraItemPickerSettings.Exists(e => e.StaticIndexOnList == index))
            {
                throw new NotImplementedException();
                // clickedCell.ToggleSelected(true);
                // return true;
            }

            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return false;
            }


            // performance monitoring
            var currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,data.Item.AssetId, $"{_stringSubcategory} asset id");
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _previousSpan = currentPoseSpan;

            if (wasSelected && data.Item.IsEditable)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            if (!wasSelected && _chaosCustomizer != null)
            {
                _chaosCustomizer.ResetAllValues();
            }

            var command = new EquipNativeAvatarAssetCommand(data.Item.AssetId, CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);

            _lastSelectedBlendShape = data.Item?.DisplayName;

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var props = new AnalyticProperties();
            props.AddProperty("flairAssetName", $"{data.Item?.AssetId}");
            AnalyticsReporter.LogEvent(AnalyticsEventsPerFlairType[_flairCategory][AnalyticsActionType.PresetSelected], props);

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
            if (_extraItemPickerSettings != null && _extraItemPickerSettings.Exists(e => e.StaticIndexOnList == index))
            {
               view.SetState(ItemCellState.Initialized);
               view.SetDebuggingAssetLabel(_ids[index]);
               return true;
            }

            return await InitializeCellViewBaseAsync<BasicInventoryUiData>(view, index, isSelected, cancellationToken);
        }


        /// <summary>
        /// Dispose the controller.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            flairColorDataSource.Dispose();
            _categorySpan = null;
            _previousSpan = null;

        }
    }
}
