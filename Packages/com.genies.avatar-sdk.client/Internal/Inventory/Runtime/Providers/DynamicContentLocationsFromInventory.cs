using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Models;
using Genies.Services.Model;
using UnityEngine;
using Genies.AssetLocations;

namespace Genies.Inventory.Providers
{
    public class DynamicContentLocationsFromInventory : IResourceLocationMetadataProvider<UserInventoryItem>
    {
        public async UniTask<List<ResourceLocationMetadata>> Provide(UserInventoryItem metadata, string platform, string baseUrl, IEnumerable<string> lods, IEnumerable<string> iconSizes)
        {
            if (metadata.Creator == "internal")
            {
                return default;
            }

            PipelineItem pipelineItem = await InventoryPipelineProvider.ResolvePipelineItem(metadata.Asset);

            if (pipelineItem == null)
            {
                CrashReporter.LogWarning($"[UserInventoryLocationProviderForAddressables.Provide] No pipeline item found for asset {metadata.AssetId} of type {metadata.AssetType}.");
                return default;
            }

            var internalIdPath = "Assets/Genies_Content";
            var parentGuid = string.IsNullOrEmpty(pipelineItem.ParentId) ? metadata.AssetId : pipelineItem.ParentId;
            var internalIdPrefix = $"{internalIdPath}/{parentGuid}";
            var bundleKeySuffix = $"_assets_{metadata.Creator}_{metadata.AssetId}";
            var addressableGroupPrefix = $"dynamicgroup{metadata.AssetType.ToLower()}";
            var bundleKeyPrefix = $"{addressableGroupPrefix}{bundleKeySuffix}";
            var bundleKeyIconPrefix = $"{addressableGroupPrefix}icons{bundleKeySuffix}";
            var remoteUrlPrefix = $"{baseUrl}/external/{metadata.Creator}/{metadata.AssetType}/{parentGuid}/v{pipelineItem.AssetVersion}";
            var locationData = new List<ResourceLocationMetadata>();

            foreach (var lod in lods)
            {
                var primaryKey = $"{metadata.AssetId}{lod}";
                var internalId = $"{internalIdPrefix}{lod}.asset";
                var bundleKey = $"{bundleKeyPrefix}{lod}_v{pipelineItem.AssetVersion}.bundle";
                var remoteUrl = $"{remoteUrlPrefix}/{platform}/{bundleKey}";

                locationData.Add(new ResourceLocationMetadata()
                {
                    Type = typeof(GearElementContainer),
                    Address = primaryKey,
                    InternalId = internalId,
                    BundleKey = bundleKey,
                    RemoteUrl = remoteUrl,
                });
            }

            foreach (var iconSize in iconSizes)
            {
                var primaryKey = $"{metadata.AssetId}{iconSize}";
                var internalId = $"{internalIdPrefix}{iconSize}.png";
                var bundleKey = $"{bundleKeyIconPrefix}{iconSize}_v{pipelineItem.AssetVersion}.bundle";
                var remoteUrl = $"{remoteUrlPrefix}/{platform}/{bundleKey}";

                locationData.Add(new ResourceLocationMetadata()
                {
                    Type = typeof(Sprite),
                    Address = primaryKey,
                    InternalId = internalId,
                    BundleKey = bundleKey,
                    RemoteUrl = remoteUrl,
                });

                locationData.Add(new ResourceLocationMetadata()
                {
                    Type = typeof(Texture2D),
                    Address = primaryKey,
                    InternalId = internalId,
                    BundleKey = bundleKey,
                    RemoteUrl = remoteUrl,
                });
            }

            return locationData;
        }
    }
}
