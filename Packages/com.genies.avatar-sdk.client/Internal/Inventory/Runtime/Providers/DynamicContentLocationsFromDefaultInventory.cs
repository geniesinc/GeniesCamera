using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.AssetLocations;
using Genies.CrashReporting;
using Genies.Models;
using UnityEngine;

namespace Genies.Inventory.Providers
{
    /// <summary>
    /// Generates Resource Locations out of Metadata from Inventory.. Replaces Merged Catalogs for Animations..
    /// </summary>
    public class DynamicContentLocationsFromDefaultInventory :  IResourceLocationMetadataProvider<DefaultAnimationLibraryAsset>
    {
        public UniTask<List<ResourceLocationMetadata>> Provide(DefaultAnimationLibraryAsset metadata, string platform, string baseUrl, IEnumerable<string> lods, IEnumerable<string> iconSizes)
        {
            PipelineData pipelineItem = metadata.PipelineData;

            if (pipelineItem == null)
            {
                CrashReporter.LogWarning($"[DynamicContentLocationsFromDefaultInventory] No pipeline item found for asset {metadata.AssetId} of type {metadata.AssetType}.");
                return default;
            }

            var internalIdPath = "Assets/Genies_Content";
            var parentGuid = string.IsNullOrEmpty(pipelineItem.ParentId) ? metadata.AssetId : pipelineItem.ParentId;
            var internalIdPrefix = $"{internalIdPath}/{parentGuid}";



            // bundle key
            var assetType = metadata.AssetType.ToString().ToLower();
            var addressableGroupPrefix = $"dynamicgroup{assetType}";
            var bundleKeySuffix = $"_assets_internal_{metadata.AssetId.ToLower()}";
            var bundleKeyPrefix = $"{addressableGroupPrefix}{bundleKeySuffix}";

            var bundleKeyIconPrefix = $"{addressableGroupPrefix}icons{bundleKeySuffix}";
            var remoteUrlPrefix = $"{baseUrl}/internal/{metadata.AssetType}/{parentGuid}/v{pipelineItem.AssetVersion}";
            var locationData = new List<ResourceLocationMetadata>();

            foreach (var lod in lods)
            {
                var primaryKey = $"{metadata.AssetId}{lod}";
                var internalId = $"{internalIdPrefix}{lod}.asset";
                var bundleKey = $"{bundleKeyPrefix}_v{pipelineItem.AssetVersion}.bundle";
                var remoteUrl = $"{remoteUrlPrefix}/{platform}/{bundleKey}";

                locationData.Add(new ResourceLocationMetadata()
                {
                    Type = GetContainerForAnimation(metadata.Category),
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
                    Type = typeof(Texture2D),
                    Address = primaryKey,
                    InternalId = internalId,
                    BundleKey = bundleKey,
                    RemoteUrl = remoteUrl,
                });

                locationData.Add(new ResourceLocationMetadata()
                {
                    Type = typeof(Sprite),
                    Address = primaryKey,
                    InternalId = internalId,
                    BundleKey = bundleKey,
                    RemoteUrl = remoteUrl,
                });
            }

            return UniTask.FromResult(locationData);
        }
        
        private Type GetContainerForAnimation(string category)
        {
            return category.ToLower() switch
            {
                "geniescameraemote" => typeof(GeniesCameraEmoteContainer),
                "behavioranim" => typeof(BehaviorAnimContainer),
                "spacesidle" => typeof(SpacesIdleContainer),
                _ => typeof(AnimationContainer)
            };
        }
    }
}