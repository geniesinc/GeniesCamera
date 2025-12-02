using System;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Handles inventory events and updates NAF content metadata and locations accordingly
    /// </summary>
    public class NafInventoryEventHandler
    {
        private bool _isSubscribed = false;

        public void Initialize()
        {
            if (!_isSubscribed)
            {
                ServiceManager.Get<IDefaultInventoryService>().AssetMinted += OnAssetMinted;
                _isSubscribed = true;
            }
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                ServiceManager.Get<IDefaultInventoryService>().AssetMinted -= OnAssetMinted;
                _isSubscribed = false;
            }
        }

        private async void OnAssetMinted(string assetId)
        {
            try
            {
                // Update NAF content metadata
                await UpdateAssetMetadata(assetId);
                
                // Update resource locations
                await UpdateAssetLocations(assetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[NafInventoryEventHandler] Failed to handle asset minted event for {assetId}: {ex}");
            }
        }

        /// <summary>
        /// Updates NAF content metadata for a specific asset
        /// </summary>
        /// <param name="assetId">The ID of the asset to update</param>
        private async UniTask UpdateAssetMetadata(string assetId)
        {
            try
            {
                var nafContentService = ServiceManager.Get<NafContentService>();
                if (nafContentService != null)
                {
                    await nafContentService.UpdateAssetMetadata(assetId);
                }
                else
                {
                    CrashReporter.LogWarning($"[NafInventoryEventHandler] NafContentService not found, skipping metadata update for asset {assetId}");
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[NafInventoryEventHandler] Failed to update asset metadata for {assetId}: {ex}");
            }
        }

        /// <summary>
        /// Updates resource locations for a specific asset
        /// </summary>
        /// <param name="assetId">The ID of the asset to update</param>
        private async UniTask UpdateAssetLocations(string assetId)
        {
            try
            {
                var locationService = ServiceManager.Get<IInventoryNafLocationsProvider>();
                await locationService.UpdateAssetLocations(assetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[NafInventoryEventHandler] Failed to update asset locations for {assetId}: {ex}");
            }
        }
    }
}
