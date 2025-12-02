using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars.Behaviors;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory.UIData;
using Genies.Refs;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Intermediate base class for customization controllers that use InventoryUIDataProvider.
    /// Provides common functionality for inventory-based controllers
    /// </summary>
    public abstract class InventoryCustomizationController : BaseCustomizationController
    {

        [SerializeField]
        protected GeniesVirtualCameraCatalog _virtualCamera = GeniesVirtualCameraCatalog.FullBodyFocusCamera;

        #region Initialization, Pagination, Data Fetching

        /// <summary>
        /// Helper to initialize a UI provider with the specified configuration.
        /// Eliminates repeated pattern in TryToInitialize methods.
        /// </summary>
        protected void InitializeUIProvider<TAsset, TUI>(
            InventoryUIDataProviderConfig<TAsset, TUI> config,
            IAssetsService assetsService)
            where TUI : IAssetUiData
        {
            var provider = new InventoryUIDataProvider<TAsset, TUI>(config, assetsService);
            SetUIProvider(provider);
        }

        /// <summary>
        /// Indicates if more items are available for pagination.
        /// </summary>
        public virtual bool HasMoreItems => GetUIProvider()?.HasMoreData ?? false;

        /// <summary>
        /// Indicates if items are currently being loaded.
        /// </summary>
        public virtual bool IsLoadingMore => GetUIProvider()?.IsLoadingMore ?? false;

        /// <summary>
        /// Base implementation for loading more items with pagination support.
        /// Consolidates common pagination logic across controllers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="category">Category to filter by</param>
        /// <param name="subcategory">Subcategory to filter by</param>
        /// <param name="reorderLogic">Optional function to reorder the IDs list</param>
        /// <returns>True if more items were loaded successfully</returns>
        protected virtual async UniTask<bool> LoadMoreItemsBaseAsync(
            CancellationToken cancellationToken,
            string category = null,
            string subcategory = null,
            Func<List<string>, List<string>> reorderLogic = null)
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

                // Load more data from the provider
                var newItemsList = await provider.LoadMoreAsync(category: category, subcategory: subcategory).AttachExternalCancellation(cancellationToken);
                var newItems = newItemsList?.Cast<BasicInventoryUiData>().ToList() ?? new List<BasicInventoryUiData>();

                if (newItems.Count == 0)
                {
                    return false;
                }

                // Get updated IDs list
                _ids = await provider.GetAllAssetIds(category, subcategory).AttachExternalCancellation(cancellationToken) ?? new List<string>();

                // Apply reordering logic if provided
                if (reorderLogic != null)
                {
                    _ids = reorderLogic(_ids);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                _ids = null;
                return false;
            }
            catch (Exception e)
            {
                CrashReporter.Log($"{GetType().Name}'s LoadMoreItemsAsync failed: {e}", LogSeverity.Error);
                return false;
            }
        }

        /// <summary>
        /// Base implementation for initializing and getting count of items.
        /// Handles common pattern of fetching IDs and optionally reordering them.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="category">Category to filter by</param>
        /// <param name="subcategory">Subcategory to filter by</param>
        /// <param name="reorderLogic">Optional function to reorder the IDs list</param>
        /// <returns>Count of items loaded</returns>
        protected virtual async UniTask<int> InitializeAndGetCountBaseAsync(
            CancellationToken cancellationToken,
            string category = null,
            string subcategory = null,
            Func<List<string>, List<string>> reorderLogic = null)
        {
            try
            {
                var provider = GetUIProvider();
                if (provider == null)
                {
                    return 0;
                }

                _ids = await provider.GetAllAssetIds(category, subcategory).AttachExternalCancellation(cancellationToken) ?? new List<string>();

                // Apply reordering logic if provided
                if (reorderLogic != null)
                {
                    _ids = reorderLogic(_ids);
                }

                return _ids?.Count ?? 0;
            }
            catch (OperationCanceledException)
            {
                _ids = null;
                return 0;
            }
            catch (Exception e)
            {
                CrashReporter.Log($"{GetType().Name}'s InitializeAndGetCountAsync failed: {e}", LogSeverity.Error);
                return 0;
            }
        }

        /// <summary>
        /// Base implementation for getting cached or loading data for a specific index.
        /// Implements common caching pattern used across controllers.
        /// </summary>
        /// <typeparam name="TData">The type of UI data</typeparam>
        /// <param name="index">Index of the item</param>
        /// <param name="errorContext">Optional context string for error logging</param>
        /// <returns>Ref to the UI data, or default if failed</returns>
        protected virtual async UniTask<Ref<TData>> GetDataForIndexBaseAsync<TData>(
            int index,
            string errorContext = null)
            where TData : class, IAssetUiData
        {
            string id = string.Empty;

            try
            {
                // Check if we already have valid cached data
                if (TryGetLoadedData<TData>(index, out var data))
                {
                    return data;
                }

                // Validate index bounds
                if (index < 0 || _ids == null || index >= _ids.Count)
                {
                    return default;
                }

                // Get the asset ID and load data
                id = _ids[index];
                if (string.IsNullOrEmpty(id))
                {
                    return default;
                }

                var provider = GetUIProvider();
                if (provider == null)
                {
                    return default;
                }

                var uiDataInterface = await provider.GetDataForAssetId(id);
                if (uiDataInterface is not TData uiData)
                {
                    return default;
                }

                var newDataRef = CreateRef.FromDependentResource(uiData);

                // Cache the data
                _loadedData ??= new();
                _loadedData[index] = newDataRef;

                return newDataRef;
            }
            catch (Exception e)
            {
                var context = errorContext ?? GetType().Name;
                CrashReporter.Log($"{context}'s GetDataForIndexAsync with index {index} & id {id} can't be retrieved: {e}", LogSeverity.Error);

                // Return a null ref to keep call sites simple
                return CreateRef.From<TData>(null);
            }
        }

        /// <summary>
        /// Base implementation for getting the currently selected index.
        /// Consolidates repeated pattern of finding equipped items.
        /// </summary>
        /// <param name="isEquippedPredicate">Predicate to check if an ID is equipped</param>
        /// <returns>Index of equipped item, or -1 if none found</returns>
        protected virtual int GetCurrentSelectedIndexBase(Func<string, bool> isEquippedPredicate)
        {
            if (_ids == null || _ids.Count == 0)
            {
                return -1;
            }

            var equippedId = _ids.FirstOrDefault(isEquippedPredicate);
            return string.IsNullOrEmpty(equippedId) ? -1 : _ids.IndexOf(equippedId);
        }

         /// <summary>
        /// Helper to initialize a cell view with common data binding logic.
        /// </summary>
        /// <typeparam name="TData">The type of UI data</typeparam>
        /// <param name="view">The cell view to initialize</param>
        /// <param name="index">Index of the cell</param>
        /// <param name="isSelected">Whether the cell is selected</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if initialization succeeded</returns>
        protected virtual async UniTask<bool> InitializeCellViewBaseAsync<TData>(
            ItemPickerCellView view,
            int index,
            bool isSelected,
            CancellationToken cancellationToken)
            where TData : class, IAssetUiData
        {
            var dataRef = await GetDataForIndexBaseAsync<TData>(index);

            if (dataRef.Item == null)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            // Handle InventoryPickerCellView
            if (view is InventoryPickerCellView inventoryView)
            {
                if (dataRef is Ref<BasicInventoryUiData> basicData)
                {
                    inventoryView.thumbnail.sprite = basicData.Item.Thumbnail.Item;
                    inventoryView.SetIsEditable(basicData.Item.IsEditable && _customizer.HasEditingNode == true);
                }

                inventoryView.SetDebuggingAssetLabel(dataRef.Item.AssetId);
                inventoryView.SetAssetName(dataRef.Item.DisplayName);
                return true;
            }


            // Handle GenericItemPickerCellView
            if (view is GenericItemPickerCellView genericView)
            {
                if (dataRef is Ref<BasicInventoryUiData> basicData)
                {
                    genericView.thumbnail.sprite = basicData.Item.Thumbnail.Item;
                    genericView.SetIsEditable(basicData.Item.IsEditable && _customizer.HasEditingNode == true);
                }

                genericView.SetDebuggingAssetLabel(dataRef.Item.AssetId);
                return true;
            }

            return false;
        }

         #endregion

         #region Camera Management

        /// <summary>
        /// Activates the virtual camera specified in _virtualCamera field.
        /// Call this in StartCustomization to focus on the appropriate area.
        /// </summary>
        protected void ActivateCamera()
        {
            CustomizationContext.CurrentVirtualCameraController.ActivateVirtualCamera(_virtualCamera);
        }

        /// <summary>
        /// Activates a specific virtual camera.
        /// </summary>
        protected void ActivateCamera(GeniesVirtualCameraCatalog camera)
        {
            CustomizationContext.CurrentVirtualCameraController.ActivateVirtualCamera(camera);
        }

        /// <summary>
        /// Resets the camera to the default full body view.
        /// Call this in StopCustomization to return to the default view.
        /// </summary>
        protected void ResetCamera()
        {
            CustomizationContext.CurrentVirtualCameraController.ActivateVirtualCamera(GeniesVirtualCameraCatalog.FullBodyFocusCamera);
        }

        #endregion
    }
}

