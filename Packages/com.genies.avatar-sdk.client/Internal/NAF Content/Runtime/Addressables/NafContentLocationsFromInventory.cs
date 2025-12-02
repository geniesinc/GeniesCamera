using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Addressables;
using Genies.Addressables.Universal;
using Genies.AssetLocations;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.Models;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Fetches metadata from the Inventory (AIGC assets) and converts them into ResourceLocationMetadata for Addressables.
    /// </summary>
    public class NafContentLocationsFromInventory : IInventoryNafLocationsProvider
    {
        private IInventoryService _InventoryService => this.GetService<IInventoryService>();
        private IDefaultInventoryService _DefaultInventoryService => this.GetService<IDefaultInventoryService>();

        public async UniTask AddCustomResourceLocationsFromInventory(bool includeV1Inventory = false)
        {
            // Fetch all data concurrently
            var (defaultWearables,
                defaultFlair,
                defaultMakeup,
                defaultBaseData,
                defaultImageLibrary) = await UniTask.WhenAll(
                _DefaultInventoryService.GetAllWearables(),
                _DefaultInventoryService.GetDefaultAvatarFlair(),
                _DefaultInventoryService.GetDefaultAvatarMakeup(),
                _DefaultInventoryService.GetDefaultAvatarBaseData(),
                _DefaultInventoryService.GetDefaultImageLibrary());

            UserInventoryData userInventory = new UserInventoryData();
            if (includeV1Inventory)
            {
                userInventory = await _InventoryService.GetUserInventory();
            }
            
            var conversionTasks = new List<UniTask<List<ResourceLocationMetadata>>>();

            if (userInventory.Items?.Count > 0)
            {
                conversionTasks.Add(ConvertUserInventoryItems(userInventory.Items.ToList()));
            }

            // Combine all default assets
            var allDefaultAssets = defaultWearables.OfType<DefaultInventoryAsset>()
                .Concat(defaultFlair)
                .Concat(defaultMakeup)
                .Concat(defaultBaseData)
                .Concat(defaultImageLibrary)
                .ToList();

            if (allDefaultAssets.Count > 0)
            {
                conversionTasks.Add(ConvertDefaultAssets(allDefaultAssets));
            }

            // Convert and apply all locations
            var results = await UniTask.WhenAll(conversionTasks);
            var allLocations = results.SelectMany(x => x).ToList();
            
            allLocations.ForEach(UniversalContentResourceLocationUtils.AddUniversalLocations);
        }

        private async UniTask<List<ResourceLocationMetadata>> ConvertUserInventoryItems(List<UserInventoryItem> items)
        {
            var provider = this.GetService<IResourceLocationMetadataProvider<UserInventoryItem>>();
            if (provider == null)
            {
                CrashReporter.LogWarning("[NafContent] No provider found for UserInventoryItem");
                return new List<ResourceLocationMetadata>();
            }

            return await ConvertWithProvider(items, provider);
        }

        private async UniTask<List<ResourceLocationMetadata>> ConvertDefaultAssets(List<DefaultInventoryAsset> assets)
        {
            var provider = this.GetService<IResourceLocationMetadataProvider<DefaultInventoryAsset>>();
            if (provider == null)
            {
                CrashReporter.LogWarning("[NafContent] No provider found for DefaultInventoryAsset");
                return new List<ResourceLocationMetadata>();
            }

            return await ConvertWithProvider(assets, provider);
        }

        private async UniTask<List<ResourceLocationMetadata>> ConvertWithProvider<T>(
            IEnumerable<T> metadataList,
            IResourceLocationMetadataProvider<T> provider)
        {
            var results = await UniTask.WhenAll(metadataList.Select(metadata =>
                provider.Provide(
                    metadata,
                    BaseAddressablesService.GetPlatformString(),
                    BaseAddressableProvider.DynBaseUrl,
                    AssetLocationDefaults.AssetLods,
                    AssetLocationDefaults.IconSizes)
            ));

            return results.Where(r => r != null).SelectMany(r => r).ToList();
        }

        /// <summary>
        /// Updates resource locations for a specific asset after it's been given to the user
        /// </summary>
        /// <param name="assetId">The ID of the asset to update locations for</param>
        public async UniTask UpdateAssetLocations(string assetId)
        {
            try
            {
                // First try to find it in user inventory
                var userInventory = await _DefaultInventoryService.GetUserWearables();
                var userAsset = userInventory?.FirstOrDefault(item => item.AssetId == assetId);
                
                if (userAsset != null)
                {
                    var locations = await ConvertDefaultAssets(new List<DefaultInventoryAsset> { userAsset });
                    locations.ForEach(UniversalContentResourceLocationUtils.AddUniversalLocations);
                }
                else
                {
                    CrashReporter.LogError($"[NafContentLocationsFromInventory] Could not find asset in user's inventory with id: {assetId}");
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[NafContentLocationsFromInventory] Failed to update asset locations for {assetId}: {ex}");
            }
        }
    }
}