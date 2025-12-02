using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.AssetLocations;
using Genies.Models;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory.Providers
{

    public class UniversalContentLocationsFromInventory : IResourceLocationMetadataProvider<UserInventoryItem>
    {
        public async UniTask<List<ResourceLocationMetadata>> Provide(UserInventoryItem metadata, string platform, string baseUrl, IEnumerable<string> lods, IEnumerable<string> iconSizes)
        {
            var locations = new List<ResourceLocationMetadata>();
            // once all asset come from inventory remove this clause
            if (metadata.Creator == "internal")
            {
                return locations;
            }

            PipelineItem pItem = await InventoryPipelineProvider.ResolvePipelineItem(metadata.Asset);

            // only generate uris when universal is available
            if (!(pItem.UniversalAvailable ?? false))
            {
                return locations;
            }

            var assetType = metadata.AssetType;
            var version = pItem.UniversalBuildVersion.ToString();
            var parentGuid = string.IsNullOrEmpty(pItem.ParentId) ? metadata.AssetId : pItem.ParentId;

            foreach (var iconSize in iconSizes)
            {
                var primaryKey = $"{metadata.AssetId}{iconSize}";
                var internalId = $"{assetType}/{metadata.AssetId}{iconSize}";
                var bundleId = AssetLocationUtility.ToIconContainerUri(assetType, parentGuid, version, iconSize);
                var remoteUrl = AssetLocationUtility.ToIconFullUrl(baseUrl, assetType, parentGuid, version, iconSize);
                locations.Add(new ResourceLocationMetadata()
                {
                    Type = typeof(Sprite),
                    Address = primaryKey,
                    InternalId = internalId,
                    BundleKey = bundleId,
                    RemoteUrl = remoteUrl,
                });
                locations.Add(new ResourceLocationMetadata()
                {
                    Type = typeof(Sprite),
                    Address = internalId,
                    InternalId = internalId,
                    BundleKey = bundleId,
                    RemoteUrl = remoteUrl,
                });
            }
            return locations;
        }
    }
}
