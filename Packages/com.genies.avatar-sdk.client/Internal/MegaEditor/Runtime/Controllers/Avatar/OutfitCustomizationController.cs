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
using Genies.Models;
using Genies.Naf.Content;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Customization controller for avatar outfit assets
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "OutfitCustomizationController", menuName = "Genies/Customizer/Controllers/Outfit Customization Controller")]
#endif
    public class OutfitCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        [FormerlySerializedAs("_colorPresetDataSource")]
        [SerializeField]
        private CustomizationItemPickerDataSource _secondaryDataSource;

        /// <summary>
        /// The subcategory to load.
        /// </summary>
        [SerializeField]
        private WardrobeSubcategory _subcategory;

        /// <summary>
        /// Cache subcategory as a string for optimization.
        /// </summary>
        private string _subcategoryString;
        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;

        // TODO WHAT TO DO ABOUT PENDING ASSETS SERVICE
        //private IPendingAigcAssetUIService _PendingAigcAssetService => this.GetService<IPendingAigcAssetUIService>();
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;
        private readonly object _loadedDataLock = new();
        private bool _isRefreshing;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.AllWearablesConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _secondaryDataSource?.Initialize(_customizer);
            lock (_loadedDataLock)
            {
                _loadedData = new();
            }

            _subcategoryString = _subcategory == WardrobeSubcategory.none ? null : _subcategory.ToString();


            /*if (_PendingAigcAssetService != null)
            {
                _PendingAigcAssetService.RefreshScreen -= RefreshScreen;
                _PendingAigcAssetService.RefreshScreen += RefreshScreen;
            }*/


            return UniTask.FromResult(true);
        }

        private async UniTask RefreshScreen()
        {
            // prevent multiple simultaneous refreshes
            if (_isRefreshing)
            {
                return;
            }

            try
            {
                _isRefreshing = true;

                await InitializeAndGetCountAsync(new());

                lock (_loadedDataLock)
                {
                    if (_loadedData != null)
                    {
                        foreach (var data in _loadedData.Values)
                        {
                            if (data is Ref<BasicInventoryUiData> dataRef && dataRef.IsAlive)
                            {
                                dataRef.Dispose();
                            }
                        }
                        _loadedData.Clear();
                    }
                }

                StartCustomization();
                _customizer.RefreshItemPicker().Forget();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Error in RefreshScreen: {ex}");

                // fallback to full restart on error
                StopCustomization();
                StartCustomization();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName,
                nameof(OutfitCustomizationController), $"open outfit - {_subcategoryString} category");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.EnterOutfitEditingEvent);

            //Aim the camera
            ActivateCamera();

            // Check if this node will automatically navigate to a child node
            // If so, don't show the item picker here as the child will handle it
            bool willAutoNavigateToChild = _customizer.CurrentNode?.OpenFirstChildNodeAsDefault == true &&
                                         _customizer.CurrentNode?.Children?.Count > 0;

            if (!willAutoNavigateToChild)
            {
                //Show the item picker and set this controller as the data source.
                ShowPrimaryPicker(this);

                if (_secondaryDataSource != null)
                {
                    ShowSecondaryPicker(_secondaryDataSource);
                }
            }
        }

        public override void StopCustomization()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ExitOutfitEditingEvent);
            ResetCamera();
            //Hide the item picker
            HidePrimaryPicker();
            HideSecondaryPicker();
            _secondaryDataSource?.StopCustomization();
        }

        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            //When an undo/redo happens we want to refresh the current selection
            RefreshPrimaryPickerSelection();

            if (_secondaryDataSource != null)
            {
                RefreshSecondaryPickerSelection();
            }
        }


        /// <summary>
        /// Return which outfit asset index is currently selected
        /// </summary>
        /// <returns></returns>
        public int GetCurrentSelectedIndex()
        {
            // Check if there is a pending asset at the first position
            /*if (_PendingAigcAssetService != null && _PendingAigcAssetService.IsIdPendingAsset(_ids?.FirstOrDefault()))
            {
                return 0;
            }*/

            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        /// <summary>
        /// Get how many outfit assets we are going to show for <see cref="_subcategory"/>
        /// and initialize the list of outfit assets <see cref="_ids"/>
        /// </summary>
        /// <returns></returns>
        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            // Special handling for "all" subcategory
            if (_subcategory == WardrobeSubcategory.all)
            {
                if (_uiProvider == null || !HasMoreItems || IsLoadingMore)
                {
                    return false;
                }

                try
                {
                    var provider = GetUIProvider();
                    if (provider == null)
                    {
                        return false;
                    }

                    var newItemsList = await provider.LoadMoreAsync(category: null, subcategory: _subcategoryString);
                    var newItems = newItemsList?.Cast<BasicInventoryUiData>().ToList() ?? new List<BasicInventoryUiData>();

                    if (newItems.Count == 0)
                    {
                        return false;
                    }

                    List<WardrobeSubcategory> subCategories = new()
                    {
                        WardrobeSubcategory.shirt, WardrobeSubcategory.hoodie, WardrobeSubcategory.jacket,
                        WardrobeSubcategory.shorts, WardrobeSubcategory.dress, WardrobeSubcategory.pants,
                        WardrobeSubcategory.shoes, WardrobeSubcategory.bracelet, WardrobeSubcategory.earrings,
                        WardrobeSubcategory.glasses, WardrobeSubcategory.hat, WardrobeSubcategory.mask,
                        WardrobeSubcategory.watch
                    };
                    _ids = await provider.GetAllAssetIds(
                        subCategories.Select(s => s.ToString().ToLower()).ToList(),
                        pageSize: InventoryConstants.DefaultPageSize) ?? new List<string>();

                    return true;
                }
                catch (Exception e)
                {
                    CrashReporter.Log($"OutfitCustomizationController's LoadMoreItemsAsync failed: {e}", LogSeverity.Error);
                    return false;
                }
            }

            // Use base implementation for normal subcategories
            return await LoadMoreItemsBaseAsync(cancellationToken, _subcategory.ToString().ToLower(), _subcategoryString);
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            // Special handling for "all" subcategory
            if (_subcategory == WardrobeSubcategory.all)
            {
                var provider = GetUIProvider();
                if (provider == null)
                {
                    return 0;
                }

                // If all outfit categories are wanted, include all except for hair/facial hair
                List<WardrobeSubcategory> subCategories = new()
                {
                    WardrobeSubcategory.shirt,
                    WardrobeSubcategory.hoodie,
                    WardrobeSubcategory.jacket,
                    WardrobeSubcategory.shorts,
                    WardrobeSubcategory.dress,
                    WardrobeSubcategory.pants,
                    WardrobeSubcategory.shoes,
                    WardrobeSubcategory.bracelet,
                    WardrobeSubcategory.earrings,
                    WardrobeSubcategory.glasses,
                    WardrobeSubcategory.hat,
                    WardrobeSubcategory.mask,
                    WardrobeSubcategory.watch,
                };

                _ids = await provider.GetAllAssetIds(
                    subCategories.Select(s => s.ToString().ToLower()).ToList(),
                    InventoryConstants.DefaultPageSize) ?? new List<string>();
                return _ids.Count;
            }

            // Use base implementation for normal subcategories
            return await InitializeAndGetCountBaseAsync(cancellationToken, _subcategory.ToString().ToLower());
        }


        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
           if (_ids == null || _ids.Count == 0)
           {
               return default;
           }

           return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "OutfitCustomization");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            CTAButtonType ctaType = CTAButtonType.SingleNoneCTA;
            return new ItemPickerCtaConfig(ctaType: ctaType, noneSelectedDelegate: NoneSelectedAsync, createNewAction: OnCreateNewRequested);
        }

        private void OnCreateNewRequested()
        {
            CurrentCustomizableWearable.CurrentCategory = _subcategoryString;
            _customizer.GoToCreateItemNode();
        }

        private async UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            try
            {
                _InstrumentationManager.FinishChildSpan(_previousSpan);
                var currentRef = await GetDataForIndexAsync(GetCurrentSelectedIndex());

                if (currentRef.Item == null)
                {
                    return false;
                }
                IAssetIdConverter _iAssetIdConverter = ServiceManager.GetService<IAssetIdConverter>(null);
                var idToUnequip = await _iAssetIdConverter.ConvertToUniversalIdAsync(currentRef.Item.AssetId);

                var props = new AnalyticProperties();
                props.AddProperty("AssetId", idToUnequip);
                AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoOutfitSelected, props);
                if (!currentRef.IsAlive || cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var unequipCmd = new UnequipNativeAvatarAssetCommand(idToUnequip, CurrentCustomizableAvatar);
                await unequipCmd.ExecuteAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                _customizer.RegisterCommand(unequipCmd);
                return true;
            }
            catch (Exception exception)
            {
                CrashReporter.LogHandledException(exception);
                return false;
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
        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return false;
            }

            // performance monitoring
            string currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,data.Item.AssetId,$"{_subcategoryString} asset id");
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _previousSpan = currentPoseSpan;

            // if user clicks on a pending asset that has since finished, display a failure and or success view
            /*if (data.Item is PendingAigcAssetData pendingAsset)
            {

                if (pendingAsset.AssetIsFailure)
                {
                    _PendingAigcAssetService?.ShowAssetFailure(pendingAsset.TaskId);
                    return true;
                }

                if (pendingAsset.AssetIsSuccess)
                {
                    _PendingAigcAssetService?.ShowAssetSuccess(pendingAsset.TaskId);
                    return true;
                }
            }*/

            if (wasSelected && data.Item.IsEditable)
            {
                //If the item is selected and is editable we go to the editing node.
                _customizer.GoToEditItemNode();
                return true;
            }
            IAssetIdConverter _iAssetIdConverter = ServiceManager.GetService<IAssetIdConverter>(null);
            var idToEquip = await _iAssetIdConverter.ConvertToUniversalIdAsync(data.Item.AssetId);

            //Create command for equipping outfit asset
            var command = new EquipNativeAvatarAssetCommand(idToEquip, CurrentCustomizableAvatar);

            //Execute the command
#if !PRODUCTION_BUILD
            var executeTime = Time.time;
#endif
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Analytics
            var props = new AnalyticProperties();
            props.AddProperty("name", $"{idToEquip}");
            props.AddProperty("subcategory", $"{_subcategoryString}");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.AssetClickEvent, props);

            //Register the command for undo/redo
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

            if (view is InventoryPickerCellView inventoryItemView)
            {
                if (inventoryItemView == null)
                {
                    CrashReporter.LogError($"Cannot initialize cell view for this item: {view.GetType().Namespace} - {dataRef.Item.AssetId}");
                    return false;
                }

                // check if the inventory item is a pending AIGC asset
                /*if (dataRef.Item is PendingAigcAssetData pendingAsset)
                {
                    inventoryItemView.SetIsEditable(false);
                    inventoryItemView.SetDebuggingAssetLabel(pendingAsset.TaskId);
                    inventoryItemView.SetAssetName(pendingAsset.DisplayName);
                    inventoryItemView.SetLoadingData(pendingAsset.TimeStarted, pendingAsset.TaskId, pendingAsset.Thumbnail);

                    if (pendingAsset.AssetIsFailure)
                    {
                        inventoryItemView.SetFailureIcon();
                    }

                    if (pendingAsset.AssetIsSuccess)
                    {
                        inventoryItemView.SetSuccessIcon();
                    }

                    return true;
                }*/

                inventoryItemView.thumbnail.sprite = dataRef.Item.Thumbnail.Item;
                inventoryItemView.SetIsEditable(dataRef.Item.IsEditable && _customizer.HasEditingNode);
                inventoryItemView.SetDebuggingAssetLabel(dataRef.Item.AssetId);
                inventoryItemView.SetAssetName(dataRef.Item.DisplayName);
                inventoryItemView.RemovePendingAssetIcons();
            }
            else
            {
                // common item
                var asGeneric = view as GenericItemPickerCellView;
                asGeneric.thumbnail.sprite = dataRef.Item.Thumbnail.Item;
                asGeneric.SetIsEditable(dataRef.Item.IsEditable && _customizer.HasEditingNode);
                asGeneric.SetDebuggingAssetLabel(dataRef.Item.AssetId);
            }

            return true;
        }

        /// <summary>
        /// Dispose the controller. Loop through all loaded data and dispose the refs.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            _secondaryDataSource?.Dispose();

            /*if (_PendingAigcAssetService != null)
            {
                _PendingAigcAssetService.RefreshScreen -= RefreshScreen;
            }*/
        }
    }
}
