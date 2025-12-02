using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;
using Genies.Assets.Services;
using Genies.Customization.Framework;

namespace Genies.Inventory.UIData
{
    /// <summary>
    /// Config that handles how data for a particular asset type should be loaded,
    /// what should be compared to for categories and subcategories, how the data should be sorted,
    /// and how it should be converted into the UI type
    /// </summary>
    /// <typeparam name="TAsset">The type of asset to make into UI data</typeparam>
    /// <typeparam name="TUI">The UI data type</typeparam>
    public class InventoryUIDataProviderConfig<TAsset, TUI> : IInventoryUIProviderConfig
        where TUI : IAssetUiData
    {
        public Func<string, string, UniTask<PagedResult<TAsset>>>  DataGetter { get; set; }  // Accepts category, subcategory for server-side filtering
        public Func<UniTask<PagedResult<TAsset>>>  LoadMoreGetter { get; set; }  // For pagination
        public Func<TAsset, string> CategorySelector { get; set; }
        public Func<TAsset, string> SubcategorySelector { get; set; }
        public Func<TAsset, object> Sort { get; set; }
        public Func<TAsset, TUI> DataConverter { get; set; }
    }

    public record PagedResult<TAsset>
    {
        public IEnumerable<TAsset> Data { get; set; }
        public string NextCursor { get; set; }
    }

    public interface IInventoryUIProviderConfig
    {
        // Concrete interface to get UI Provider configs from without having to use generic types
    }

    /// <summary>
    /// Base provider class that handles loading and organizing data from the inventory for UI display
    /// </summary>
    /// <typeparam name="TAsset">The asset type being used</typeparam>
    /// <typeparam name="TUI">The UI type being used</typeparam>
    public class InventoryUIDataProvider<TAsset, TUI> : IUIProvider
        where TUI : IAssetUiData
    {
        private readonly InventoryUIDataProviderConfig<TAsset, TUI> _dataConfig;
        private readonly Dictionary<string, TUI> _cache;
        private readonly IAssetsService _assetsService;

        public string NextCursor => _nextCursor;
        private string _nextCursor;
        private bool _initialized;
        private UniTaskCompletionSource<bool> _initializationCompletionSource;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        // Pagination support
        private bool _isLoadingMore;
        public bool HasMoreData => !string.IsNullOrEmpty(_nextCursor) || _uiPaginationIndex < _allFetchedData.Count;
        public bool IsLoadingMore => _isLoadingMore;

        // UI-level pagination (independent of service caching)
        private readonly List<TUI> _allFetchedData = new();
        private int _uiPaginationIndex = 0;

        public InventoryUIDataProvider(InventoryUIDataProviderConfig<TAsset, TUI> dataConfig, IAssetsService assetsService)
        {
            _dataConfig = dataConfig;
            _assetsService = assetsService;
            _cache = new();
        }

        public async UniTask ReloadAsync()
        {
            if (_initializationCompletionSource != null)
            {
                await _initializationCompletionSource.Task;
            }

            _nextCursor = null;
            _initialized = false;
            _uiPaginationIndex = 0;
            _allFetchedData.Clear();
            await Initialize();
        }

        private async UniTask<bool> Initialize(string category = null, string subCategory = null, int? pageSize = null)
        {
            if (_initialized)
            {
                return true;
            }

            if (_initializationCompletionSource != null)
            {
                return await _initializationCompletionSource.Task;
            }

            _initializationCompletionSource = new UniTaskCompletionSource<bool>();
            _initialized = false;
            _nextCursor = null;
            _uiPaginationIndex = 0;
            _allFetchedData.Clear();

            try
            {
                await LoadUIData(category, subCategory, pageSize);
            }
            catch (OperationCanceledException)
            {
                // operation was cancelled, do nothing
                _initializationCompletionSource.TrySetResult(false);
                return false;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Exception in initializing and getting UI data: {ex.Message}");
                _initializationCompletionSource.TrySetResult(false);
                return false;
            }

            _initialized = true;
            _initializationCompletionSource.TrySetResult(true);

            return true;
        }

        public async UniTask<List<TUI>> LoadUIData(string category = null, string subcategory = null, int? pageSize = null)
        {
            var pagedResult = await _dataConfig.DataGetter(category, subcategory);
            _nextCursor = pagedResult.NextCursor;

            List<TUI> allResults;

            await _cacheLock.WaitAsync();
            try
            {
                // Process ALL items from service into staging area
                allResults = pagedResult.Data
                    .Where(asset => asset != null)
                    .Where(asset => string.IsNullOrEmpty(category) || _dataConfig.CategorySelector?.Invoke(asset) == category || _dataConfig.CategorySelector?.Invoke(asset) == null)
                    .Where(asset => string.IsNullOrEmpty(subcategory) || _dataConfig.SubcategorySelector?.Invoke(asset) == subcategory || _dataConfig.SubcategorySelector?.Invoke(asset) == null)
                    .OrderBy(asset => _dataConfig.Sort?.Invoke(asset) ?? 0)
                    .Select(asset => _dataConfig.DataConverter(asset))
                    .ToList();

                // Store in staging area
                _allFetchedData.AddRange(allResults);

                List<TUI> pageToCache;

                if (pageSize.HasValue)
                {
                    // Only add most recent page to cache for UI consumption
                    pageToCache = _allFetchedData
                        .Skip(_uiPaginationIndex)
                        .Take(pageSize.Value)
                        .ToList();
                }
                else
                {
                    // If no page size, cache all data
                    pageToCache = _allFetchedData;
                }

                foreach (var uiData in pageToCache)
                {
                    if (!string.IsNullOrEmpty(uiData.AssetId))
                    {
                        _cache[uiData.AssetId] = uiData;
                    }
                }

                _uiPaginationIndex += pageToCache.Count;
            }
            finally
            {
                _cacheLock.Release();
            }

            // Load thumbnails only for the items we added to cache
            if (_cache.Count > 0 && _cache.Values.First() is BasicInventoryUiData)
            {
                await LoadThumbnailsAsync(_assetsService);
            }

            return _cache.Values.ToList();
        }

        public async UniTask<List<TUI>> LoadUIData(List<string> categories, string subcategory = null)
        {
            // For multi-category queries, pass first category or null - backend will need to support multi-category in future
            var pagedResult = await _dataConfig.DataGetter(categories?.FirstOrDefault(), subcategory);
            _nextCursor = pagedResult.NextCursor;

            List<TUI> allResults;

            await _cacheLock.WaitAsync();
            try
            {
                // Process ALL items from service into staging area
                allResults = pagedResult.Data
                    .Where(asset => asset != null)
                    .Where(asset => categories == null || categories.Contains(_dataConfig.CategorySelector?.Invoke(asset)))
                    .Where(asset => string.IsNullOrEmpty(subcategory) || _dataConfig.SubcategorySelector?.Invoke(asset) == subcategory)
                    .OrderBy(asset => _dataConfig.Sort?.Invoke(asset))
                    .Select(asset => _dataConfig.DataConverter(asset))
                    .ToList();

                // Store in staging area
                _allFetchedData.AddRange(allResults);

                // Only add first page to cache for UI consumption
                var pageToCache = _allFetchedData
                    .Skip(_uiPaginationIndex)
                    .Take(InventoryConstants.DefaultPageSize)
                    .ToList();

                foreach (var uiData in pageToCache)
                {
                    if (!string.IsNullOrEmpty(uiData.AssetId))
                    {
                        _cache[uiData.AssetId] = uiData;
                    }
                }

                _uiPaginationIndex += pageToCache.Count;
            }
            finally
            {
                _cacheLock.Release();
            }

            // Load thumbnails only for the items we added to cache
            if (_cache.Count > 0 && _cache.Values.First() is BasicInventoryUiData)
            {
                await LoadThumbnailsAsync(_assetsService);
            }

            return _cache.Values.ToList();
        }

        /// <summary>
        /// Load more data incrementally for pagination (UI-level pagination)
        /// </summary>
        public async UniTask<List<TUI>> LoadMoreAsync(string category = null, string subcategory = null)
        {
            if (_isLoadingMore)
            {
                return new List<TUI>();
            }

            _isLoadingMore = true;
            try
            {
                List<TUI> pageToCache;

                await _cacheLock.WaitAsync();
                try
                {
                    // First, check if we have more items in staging area (already fetched from service)
                    if (_uiPaginationIndex < _allFetchedData.Count)
                    {
                        // Load next page from staging area
                        pageToCache = _allFetchedData
                            .Skip(_uiPaginationIndex)
                            .Take(InventoryConstants.DefaultPageSize)
                            .ToList();

                        foreach (var uiData in pageToCache)
                        {
                            if (!string.IsNullOrEmpty(uiData.AssetId))
                            {
                                _cache[uiData.AssetId] = uiData;
                            }
                        }

                        _uiPaginationIndex += pageToCache.Count;
                    }
                    // If staging area is exhausted, fetch more from service
                    else if (!string.IsNullOrEmpty(_nextCursor) && _dataConfig.LoadMoreGetter != null)
                    {
                        _cacheLock.Release(); // Release lock before async service call

                        var pagedResult = await _dataConfig.LoadMoreGetter();
                        _nextCursor = pagedResult.NextCursor;

                        await _cacheLock.WaitAsync(); // Re-acquire lock

                        // Process new items from service into staging area
                        var newResults = pagedResult.Data
                            .Where(asset => asset != null)
                            .Where(asset => string.IsNullOrEmpty(category) || _dataConfig.CategorySelector?.Invoke(asset) == category || _dataConfig.CategorySelector?.Invoke(asset) == null)
                            .Where(asset => string.IsNullOrEmpty(subcategory) || _dataConfig.SubcategorySelector?.Invoke(asset) == subcategory || _dataConfig.SubcategorySelector?.Invoke(asset) == null)
                            .OrderBy(asset => _dataConfig.Sort?.Invoke(asset) ?? 0)
                            .Select(asset => _dataConfig.DataConverter(asset))
                            .ToList();

                        _allFetchedData.AddRange(newResults);

                        // Load next page from newly fetched data
                        pageToCache = _allFetchedData
                            .Skip(_uiPaginationIndex)
                            .Take(InventoryConstants.DefaultPageSize)
                            .ToList();

                        foreach (var uiData in pageToCache)
                        {
                            if (!string.IsNullOrEmpty(uiData.AssetId))
                            {
                                _cache[uiData.AssetId] = uiData;
                            }
                        }

                        _uiPaginationIndex += pageToCache.Count;
                    }
                    else
                    {
                        // No more data available
                        return new List<TUI>();
                    }
                }
                finally
                {
                    if (_cacheLock.CurrentCount == 0)
                    {
                        _cacheLock.Release();
                    }
                }

                // Load thumbnails only for newly added items
                if (_cache.Count > 0 && _cache.Values.First() is BasicInventoryUiData)
                {
                    await LoadThumbnailsAsync(_assetsService);
                }

                return pageToCache.ToList();
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        public async UniTask<bool> HasDataForAssetId(string assetId)
        {
            await Initialize();

            await _cacheLock.WaitAsync();
            try
            {
                return _cache.ContainsKey(assetId);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async UniTask<List<string>> GetAllAssetIds(string category = null, string subCategory = null, int? pageSize = null)
        {
            await Initialize(category, subCategory, pageSize);

            await _cacheLock.WaitAsync();

            try
            {
                // If no category given, return all assets
                if (string.IsNullOrEmpty(category) && string.IsNullOrEmpty(subCategory))
                {
                    return _cache.Keys.ToList();
                }

                var assetIds = new List<string>();


                foreach (var data in _cache.Values.ToList())
                {
                    if (data == null)
                    {
                        CrashReporter.LogWarning("Found null data when getting UI asset ids, skipping");
                        continue;
                    }

                    if ((category == null || data.Category == category) &&
                        (subCategory == null || data.SubCategory == subCategory))
                    {
                        assetIds.Add(data.AssetId);
                    }
                }

                return assetIds;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async UniTask<List<string>> GetAllAssetIds(List<string> categories, int? pageSize = null)
        {
            await Initialize(pageSize: pageSize);

            await _cacheLock.WaitAsync();

            try
            {
                var assetIds = new List<string>();
                foreach (var data in _cache.Values.ToList())
                {
                    if (data == null)
                    {
                        CrashReporter.LogWarning("Found null data when getting UI asset ids, skipping");
                        continue;
                    }

                    if (categories.Contains(data.Category))
                    {
                        assetIds.Add(data.AssetId);
                    }
                }

                return assetIds;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async UniTask<TUI> GetDataForAssetId(string assetId)
        {
            await Initialize();

            await _cacheLock.WaitAsync();
            try
            {
                if (!_cache.TryGetValue(assetId, out var uiData))
                {
                    throw new KeyNotFoundException($"Asset with id {assetId} not found.");
                }

                return uiData;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async UniTask LoadThumbnailsAsync(IAssetsService assetsService)
        {
            List<BasicInventoryUiData> itemsNeedingThumbnails;

            await _cacheLock.WaitAsync();
            try
            {
                itemsNeedingThumbnails = _cache.Values
                    .OfType<BasicInventoryUiData>()
                    .Where(uiData => !uiData.Thumbnail.IsAlive)
                    .ToList();
            }
            finally
            {
                _cacheLock.Release();
            }

            // Load thumbnails without holding the lock
            var tasks = itemsNeedingThumbnails
                .Select(uiData => LoadThumbnailForUiDataAsync(uiData, assetsService))
                .ToList();

            await UniTask.WhenAll(tasks);
        }

        private async UniTask LoadThumbnailForUiDataAsync(BasicInventoryUiData uiData, IAssetsService assetsService)
        {
            var locations = await assetsService.LoadResourceLocationsAsync<Sprite>(uiData.AssetId);
            if (locations.Count == 0)
            {
                uiData.Thumbnail = default;
                return;
            }
            uiData.Thumbnail = await assetsService.LoadAssetAsync<Sprite>(locations[0]);
        }

        #region IInventoryUIDataProvider Explicit Implementation

        // Explicit interface implementations to avoid dynamic types (IL2CPP/iOS compatible)
        bool IUIProvider.HasMoreData => HasMoreData;
        bool IUIProvider.IsLoadingMore => IsLoadingMore;

        async UniTask<List<IAssetUiData>> IUIProvider.LoadMoreAsync(string category, string subcategory)
        {
            var result = await LoadMoreAsync(category, subcategory);
            return result.Cast<IAssetUiData>().ToList();
        }

        UniTask<List<string>> IUIProvider.GetAllAssetIds(string category, string subcategory, int? pageSize)
        {
            return GetAllAssetIds(category, subcategory, pageSize);
        }

        UniTask<List<string>> IUIProvider.GetAllAssetIds(List<string> categories, int? pageSize)
        {
            return GetAllAssetIds(categories, pageSize);
        }

        async UniTask<IAssetUiData> IUIProvider.GetDataForAssetId(string assetId)
        {
            var result = await GetDataForAssetId(assetId);
            return result;
        }

        #endregion

        public void Dispose()
        {
            if (_cache?.Count > 0)
            {
                foreach (var data in _cache.Values)
                {
                    if (data is BasicInventoryUiData basicUiData)
                    {
                        basicUiData.Dispose();
                    }
                }

                _cache.Clear();
            }

            if (_allFetchedData?.Count > 0)
            {
                foreach (var data in _allFetchedData)
                {
                    if (data is BasicInventoryUiData basicUiData)
                    {
                        basicUiData.Dispose();
                    }
                }

                _allFetchedData.Clear();
            }
        }
    }
}
