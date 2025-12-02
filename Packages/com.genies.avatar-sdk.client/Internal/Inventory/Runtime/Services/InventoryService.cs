using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Addressables;
using Genies.Addressables.CustomResourceLocation;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.ServiceManagement;
using Genies.Services.Api;
using Genies.APIResolver;
using Genies.AssetLocations;
using Genies.FeatureFlags;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory
{
    /// <summary>
    /// Responsible to manage the metadata of Marketplace
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private IInventoryApi _inventoryApi;
        private UniTaskCompletionSource _apiInitializationSource;
        private CancellationTokenSource _cancellationTokenSource;
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        private string _userId;
        private readonly SemaphoreSlim _inventoryLock = new SemaphoreSlim(1, 1);
        private int? _cachedLimit;
        private UserInventoryData _cachedUserInventory;
        private readonly InventoryItemToCategory _inventoryItems;
        private readonly string _inventoryItemsFileName = "InventoryItemToCategory";

        /// <param name="inventoryItems">A scriptable object that maps an inventory item, i.e. "Shirt," to the category
        /// it belongs to, i.e. "Shirts." You can provide it as an argument override (an example is <see cref="InventoryServiceInstaller"/>)
        /// but if none is given, a default will be loaded from resources</param>
        /// <param name="partyId">An optional partyId override. If none is given, the <see cref="IAPIResolverService"/> is used</param>
        public InventoryService(InventoryItemToCategory inventoryItems = null, string partyId = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var config = new Configuration() { BasePath = GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment) };
            _inventoryApi = new InventoryApi(config);

            if (inventoryItems != null)
            {
                _inventoryItems = inventoryItems;
            }
            else
            {
                _inventoryItems = Resources.Load<InventoryItemToCategory>(_inventoryItemsFileName);
            }

            AwaitApiInitialization(partyId).Forget();
        }

        private async UniTask AwaitApiInitialization(string partyId = null)
        {
            try
            {
                // eventually pass trait service before chat service
                if (_apiInitializationSource != null)
                {
                    await _apiInitializationSource.Task;
                    return;
                }

                _apiInitializationSource = new UniTaskCompletionSource();

                // Await auth access token being set.
                await UniTask.WaitUntil(
                    () => GeniesLoginSdk.IsUserSignedIn(),
                    cancellationToken: _cancellationTokenSource.Token);

                _userId = await GeniesLoginSdk.GetUserIdAsync();
                _inventoryApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

                if (string.IsNullOrEmpty(partyId))
                {
                    IAPIResolverService apiResolverService = this.GetService<IAPIResolverService>();

                    if (apiResolverService != null)
                    {
                        partyId = await apiResolverService.GetPartyId(Application.identifier);
                    }
                }

                if (!string.IsNullOrEmpty(partyId))
                {
                    _inventoryApi.Configuration.AddApiKeyPrefix("party-id", partyId);
                }

                _apiInitializationSource.TrySetResult();
                _apiInitializationSource = null;

            }
            catch (OperationCanceledException ex)
            {
                CrashReporter.LogWarning($"Operation cancelled by the user: {ex}");
            }
            catch (Exception exception)
            {
                CrashReporter.Log(
                    $"Failed to Initialize API in {nameof(InventoryService)}: {exception}",
                    LogSeverity.Error);
            }
        }

        private string GetApiBaseUrl(BackendEnvironment environment)
        {
            switch (environment)
            {
                case BackendEnvironment.QA:
                    return "https://api.qa.genies.com";
                case BackendEnvironment.Prod:
                    return "https://api.genies.com";
                case BackendEnvironment.Dev:
                    return "https://api.dev.genies.com";
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }


        public async UniTask<UserInventoryData> GetUserInventory(int? limit = null)
        {
            // Return cached data if valid
            if (_cachedUserInventory.Items?.Count > 0 && _cachedLimit == limit)
            {
                return _cachedUserInventory;
            }

            await _inventoryLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cachedUserInventory.Items?.Count > 0 && _cachedLimit == limit)
                {
                    return _cachedUserInventory;
                }

                return await GetAndCacheUserInventoryInternal(limit);
            }
            finally
            {
                _inventoryLock.Release();
            }
        }

        private async UniTask<UserInventoryData> GetAndCacheUserInventoryInternal(int? limit = null)
        {
            await AwaitApiInitialization();

            try
            {
                var userInventory = await _inventoryApi.GetInventoryAsync(_userId, null, null, null, limit);
                if (userInventory is { Data: not null })
                {
                    var filteredAssets = userInventory.Data.Where(item => ValidateAssetType(item)).ToList();

                    var userInventoryData = new UserInventoryData(_userId, filteredAssets);

                    if (!(_FeatureFlagsManager?.IsFeatureEnabled(SharedFeatureFlags.AddressablesInventoryLocations) ?? false))
                    {
                        _cachedUserInventory = userInventoryData;
                        _cachedLimit = limit;
                        return userInventoryData;
                    }

                    CustomResourceLocationService.InitializeResourceProviders();

                    // todo add fileter by universal to inventory metadata fetch
                    IResourceLocationMetadataProvider<UserInventoryItem> provider = this.GetService<IResourceLocationMetadataProvider<UserInventoryItem>>();
                    var fetchTasks = userInventoryData.Items.Select(item => provider.Provide(item,
                        BaseAddressablesService.GetPlatformString(),
                        BaseAddressableProvider.DynBaseUrl,
                        AssetLocationDefaults.AssetLods,
                        AssetLocationDefaults.IconSizes)).ToList();

                    var locationsList = await UniTask.WhenAll(fetchTasks);
                    foreach (var locations in locationsList)
                    {
                        // skip adding locations when list is null or empty
                        // can happen when assets have not been built for universal or are internal, (will not happen once filter is implemented)
                        if (locations == null || locations.Count <= 0)
                        {
                            continue;
                        }

                        foreach (var location in locations)
                        {
                            CustomResourceLocationUtils.AddCustomLocator(location);
                        }
                    }

                    _cachedUserInventory = userInventoryData;
                    _cachedLimit = limit;
                    return userInventoryData;
                }

                CrashReporter.LogError($"Invalid user inventory: {userInventory}");
                return default;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to retrieve user inventory: {e}");
                return default;
            }
        }

        private bool ValidateAssetType(InventoryAssetInstance item)
        {
            // old, invalid assets will have AssetType of "shirt" etc.
            if (_inventoryItems.ItemToCategory.ContainsKey(item.Asset.AssetType))
            {
                return false;
            }

            return true;
        }


        public async UniTask<UserInventoryDecorData> GetUserInventoryDecor()
        {
            await AwaitApiInitialization();

            try
            {
                UserInventoryData response = await GetUserInventory();

                var decorList = new List<InventoryDecorData>();

                foreach (UserInventoryItem userInventoryItem in response.Items)
                {
                    if (userInventoryItem.Asset.AssetType == "ModelLibrary")
                    {
                        decorList.Add(new InventoryDecorData()
                        {
                            AssetId = userInventoryItem.AssetId,
                        });
                    }
                }

                var userInventoryDecorData = new UserInventoryDecorData()
                {
                    UserId = _userId,
                    DecorList = decorList,
                };

                return userInventoryDecorData;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to retrieve user inventory decor: {e}");
                return default;
            }
        }

        public void ClearCache()
        {
            // if the app reloads then new data is available in cms, so clear cache to pull new data
            _cachedUserInventory = default;
            _cachedLimit = null;
        }
    }
}
