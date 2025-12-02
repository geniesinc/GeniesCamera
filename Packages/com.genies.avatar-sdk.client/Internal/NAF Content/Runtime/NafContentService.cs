using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Inventory;
using Genies.Inventory.Providers;
using Genies.ServiceManagement;
using Genies.Services.Model;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Uses Cms Source to translate Content between guids and uris
    /// </summary>
    public class NafContentService : NafContentServiceBase
    {
        private IInventoryService _InventoryService => ServiceManager.GetService<IInventoryService>(null);
        private IDefaultInventoryService _DefaultInventoryService => ServiceManager.GetService<IDefaultInventoryService>(null);

        private bool _includeInventoryV1;
        
        public NafContentService(bool includeInventoryV1)
        {
            _includeInventoryV1 = includeInventoryV1;
        }
        
        public override async UniTask Initialize()
        {
            try
            {
                if (_includeInventoryV1)
                {
                    var inventoryRecords = await FetchAigcMetadataFromInventory();
                    Merge(_assetsByAddress, inventoryRecords);
                }

                var defaultInventoryRecords = await FetchDefaultInventory();
                Merge(_assetsByAddress, defaultInventoryRecords);
                
                // get supported version for avatar Base from dynamic config
                _AvatarBaseVersionFromConfig = await AvatarBaseVersionService.GetAvatarBaseVersion();
                
            } catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"NafContentService Initialize Failed {ex}");
            }

            _initialized = true;
        }
        
        private async UniTask<Dictionary<string, NafContentMetadata>> FetchAigcMetadataFromInventory()
        {
            UserInventoryData data = await _InventoryService.GetUserInventory();
            Dictionary<string, NafContentMetadata> inventoryAssetsByGuid = new();

            foreach (UserInventoryItem userInventoryItem in data.Items)
            {
                PipelineItem pItem = await InventoryPipelineProvider.ResolvePipelineItem(userInventoryItem.Asset);

                // external does not support skins so parentId = assetId all the time
                var guid = string.IsNullOrEmpty(pItem.ParentId) ? userInventoryItem.AssetId : pItem.ParentId;

                var newEntry = new NafContentMetadata()
                {
                    AssetAddress = guid,
                    Guid = guid,
                    Owner = userInventoryItem.Creator,
                    UniversalBuildVersion = pItem.UniversalBuildVersion.ToString(),
                    UniversalAvailable = pItem.UniversalAvailable != null && (bool) pItem.UniversalAvailable,
                    PipelineId = userInventoryItem.AssetType,
                };

                inventoryAssetsByGuid.Add(guid, newEntry);
            }

            return inventoryAssetsByGuid;
        }

        private async UniTask<Dictionary<string, NafContentMetadata>> FetchDefaultInventory()
        {
            Dictionary<string, NafContentMetadata> inventoryAssetsByGuid = new();
            
            // Fetch all data concurrently
            var (defaultGear,
                defaultEyes,
                defaultFlair,
                defaultMakeup,
                defaultAvatarBase,
                defaultImageLibrary) = await UniTask.WhenAll(
                _DefaultInventoryService.GetAllWearables(),
                _DefaultInventoryService.GetDefaultAvatarEyes(),
                _DefaultInventoryService.GetDefaultAvatarFlair(),
                _DefaultInventoryService.GetDefaultAvatarMakeup(),
                _DefaultInventoryService.GetDefaultAvatarBaseData(),
                _DefaultInventoryService.GetDefaultImageLibrary());
            
            // Combine all default assets
            var allDefaultAssets = defaultGear.OfType<DefaultInventoryAsset>()
                .Concat(defaultEyes)
                .Concat(defaultFlair)
                .Concat(defaultMakeup)
                .Concat(defaultAvatarBase)
                .Concat(defaultImageLibrary)
                .ToList();
            
            
            foreach (DefaultInventoryAsset inventoryAsset in allDefaultAssets)
            {
                PipelineData pItem = inventoryAsset.PipelineData;
                if (pItem == null)
                {
                    continue;
                }

                var guid = inventoryAsset.AssetId;

                var newEntry = new NafContentMetadata()
                {
                    AssetAddress = string.IsNullOrEmpty(pItem.AssetAddress) ? guid : pItem.AssetAddress,
                    Guid = guid,
                    Owner = "internal",
                    UniversalBuildVersion = pItem.UniversalBuildVersion,
                    UniversalAvailable = pItem.UniversalAvailable,
                    PipelineId = inventoryAsset.AssetType.ToString()
                };

                // Add both the guid and the original AssetId as keys pointing to the same metadata
                // This ensures lookups work regardless of which ID format is used by the UI controllers
                inventoryAssetsByGuid[guid] = newEntry;
                
                // If the guid (parentId) is different from the original AssetId, also add the AssetId as a key
                if (!string.IsNullOrEmpty(inventoryAsset.AssetId) && inventoryAsset.AssetId != guid)
                {
                    inventoryAssetsByGuid[inventoryAsset.AssetId] = newEntry;
                }
            }

            return inventoryAssetsByGuid;
        }

        /// <summary>
        /// Updates NAF content metadata for a specific asset after it's been given to the user
        /// </summary>
        /// <param name="assetId">The ID of the asset to update metadata for</param>
        public async UniTask UpdateAssetMetadata(string assetId)
        {
            try
            {
                // Get the updated user inventory to find the new asset
                var userInventory = await _DefaultInventoryService.GetUserWearables();
                var userAsset = userInventory?.FirstOrDefault(item => item.AssetId == assetId);
                
                if (userAsset != null)
                {
                    // Process the user asset
                    PipelineData pItem = userAsset.PipelineData;
                    var guid = userAsset.AssetId;

                    var newEntry = new NafContentMetadata()
                    {
                        AssetAddress = guid,
                        Guid = guid,
                        Owner = "internal",
                        UniversalBuildVersion = pItem.UniversalBuildVersion,
                        UniversalAvailable = pItem.UniversalAvailable,
                        PipelineId = userAsset.AssetType.ToString(),
                    };

                    // Update the metadata cache
                    _assetsByAddress[guid] = newEntry;
                    
                    if (!string.IsNullOrEmpty(userAsset.AssetId) && userAsset.AssetId != guid)
                    {
                        _assetsByAddress[userAsset.AssetId] = newEntry;
                    }
                    return;
                }

                // If not found in user inventory, check if it's a default asset that needs refreshing
                var defaultAssets = await _DefaultInventoryService.GetAllWearables();
                var defaultAsset = defaultAssets.FirstOrDefault(asset => asset.AssetId == assetId);
                
                if (defaultAsset != null)
                {
                    var pItem = defaultAsset.PipelineData;
                    if (pItem != null)
                    {
                        var guid = defaultAsset.AssetId;
                        var newEntry = new NafContentMetadata()
                        {
                            AssetAddress = string.IsNullOrEmpty(pItem.AssetAddress) ? guid : pItem.AssetAddress,
                            Guid = guid,
                            Owner = "internal",
                            UniversalBuildVersion = pItem?.UniversalBuildVersion.ToString(),
                            UniversalAvailable = pItem?.UniversalAvailable != null && (bool) pItem?.UniversalAvailable,
                            PipelineId = defaultAsset.AssetType.ToString()
                        };

                        _assetsByAddress[guid] = newEntry;
                        if (!string.IsNullOrEmpty(defaultAsset.AssetId) && defaultAsset.AssetId != guid)
                        {
                            _assetsByAddress[defaultAsset.AssetId] = newEntry;
                        }
                        
                        UnityEngine.Debug.Log($"[NafContentService] Updated metadata for default asset {assetId}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NafContentService] Failed to update asset metadata for {assetId}: {ex}");
            }
        }

    }
    
    /// <summary>
    /// Boundary layer to convert from any metadata source to naf format
    /// Internal format do not move out of this class
    /// </summary>
    public class NafContentMetadata
    {
        public NafContentMetadata()
        {
        }

        public NafContentMetadata(string assetAddress, string guid, string owner, string universalBuildVersion, bool universalAvailable,
            string pipelineId)
        {
            AssetAddress = assetAddress;
            Guid = guid;
            Owner = owner;
            UniversalBuildVersion = universalBuildVersion;
            UniversalAvailable = universalAvailable;
            PipelineId = pipelineId;
        }

        public string AssetAddress { get; set; }
        public string Guid { get; set; }
        public string Owner { get; set; }
        public string UniversalBuildVersion { get; set; }
        public bool UniversalAvailable { get; set; }
        public string PipelineId { get; set; }
    }
}
