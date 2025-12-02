using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.AssetLocations;
using Genies.Models;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory.Providers
{

    public class UniversalContentLocationsFromDefaultInventory : IResourceLocationMetadataProvider<DefaultInventoryAsset>
    {
        public UniTask<List<ResourceLocationMetadata>> Provide(DefaultInventoryAsset metadata, string platform, string baseUrl, IEnumerable<string> lods, IEnumerable<string> iconSizes)
        {
            var locations = new List<ResourceLocationMetadata>();
            
            if (metadata == null)
            {
                return UniTask.FromResult(locations);
            }

            PipelineData pItem = metadata.PipelineData;
            // only generate uris when universal is available
            if (!pItem.UniversalAvailable)
            {
                return UniTask.FromResult(locations);
            }

            var assetType = metadata.AssetType.ToString();
            var version = pItem.UniversalBuildVersion;
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
            return UniTask.FromResult(locations);
        }
    }
}
