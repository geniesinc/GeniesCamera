using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Capable of fetching <see cref="OutfitAssetMetadata"/> from a given ID for the Static, UGC and UGC-default outfit asset types.
    /// Ideally we should implement this to directly use CMS instead of actually needing to load assets to check for the asset type.
    /// For now this is the only way that we have to do it.
    /// </summary>
    public sealed class OutfitAssetMetadataService : IOutfitAssetMetadataService
    {
        private static readonly string[] _allOutfitAssetTypes =
        {
            UgcOutfitAssetType.Gear,
            StaticWearableAsset.OutfitAssetType,
            UgcOutfitAssetType.Ugc,
            UgcOutfitAssetType.UgcDefault,
        };

        /// <summary>
        /// The species that this info service will return assets for. This is an ad-hoc solution because right now we don't have any other
        /// way of knowing what species an asset is targeted for, we should probably update our content build models to contain metadata about
        /// the target species.
        /// </summary>
        public readonly string Species;

        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IUgcTemplateDataService _ugcTemplateDataService;
        private readonly IUgcWearableDefinitionService _wearableService;

        // state
        private readonly Dictionary<string, UniTaskCompletionSource<OutfitAssetMetadata>> _cachedMetadata;

        public OutfitAssetMetadataService(
            string species,
            IAssetsService assetsService,
            IUgcTemplateDataService ugcTemplateDataService,
            IUgcWearableDefinitionService wearableService = null)
        {
            Species = species;
            _assetsService = assetsService;
            _ugcTemplateDataService = ugcTemplateDataService;
            _wearableService = wearableService;

            _cachedMetadata = new Dictionary<string, UniTaskCompletionSource<OutfitAssetMetadata>>();
        }

        public async UniTask<OutfitAssetMetadata> FetchAsync(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            if (_cachedMetadata.TryGetValue(assetId, out UniTaskCompletionSource<OutfitAssetMetadata> fetchingTask))
            {
                return await fetchingTask.Task;
            }

            _cachedMetadata[assetId] = fetchingTask = new UniTaskCompletionSource<OutfitAssetMetadata>();

            OutfitAssetMetadata metadata = await NonCachedFetchAsync(assetId);
            if (metadata.IsValid)
            {
                fetchingTask.TrySetResult(metadata);
                return metadata;
            }

            // this is the old way of fetching the metadata, we don't need it anymore but I will leave it commented
            // metadata = await NonCachedBruteForceFetchAsync(assetId);
            // if (metadata.IsValid)
            // {
            //     fetchingTask.TrySetResult(metadata);
            //     return metadata;
            // }

            Debug.LogError($"[{nameof(OutfitAssetMetadataService)}] failed to fetch outfit asset metadata with ID: {assetId}");
            _cachedMetadata.Remove(assetId);
            fetchingTask.TrySetResult(default);
            return default;
        }

        public async UniTask FetchAsync(IEnumerable<string> assetIds, ICollection<OutfitAssetMetadata> assets)
        {
            OutfitAssetMetadata[] results = await UniTask.WhenAll(assetIds.Select(FetchAsync));

            foreach (OutfitAssetMetadata asset in results)
            {
                if (asset.IsValid)
                {
                    assets.Add(asset);
                }
            }
        }

        private async UniTask<OutfitAssetMetadata> NonCachedFetchAsync(string assetId)
        {
            IList<IResourceLocation> locations = await _assetsService.LoadResourceLocationsAsync(assetId);
            IList<IResourceLocation> templateLocations = await _assetsService.LoadResourceLocationsAsync($"{assetId}_template");

            foreach (IResourceLocation location in locations)
            {
                if (location.ResourceType == typeof(GearElementContainer))
                {
                    return await FetchGearMetadataAsync(assetId);
                }

                if (location.ResourceType == typeof(AssetContainer))
                {
                    return await FetchStaticMetadataAsync(assetId);
                }

                if (location.ResourceType == typeof(UgcTemplate))
                {
                    return await FetchUgcDefaultMetadataAsync(assetId);
                }
            }

            foreach (IResourceLocation location in templateLocations)
            {
                if (location.ResourceType == typeof(UgcTemplate))
                {
                    return await FetchUgcDefaultMetadataAsync(assetId);
                }
            }

            return await FetchUgcMetadataAsync(assetId);
        }

        private async UniTask<OutfitAssetMetadata> NonCachedBruteForceFetchAsync(string assetId)
        {
            // we don't know the outfit asset type so we have to try all of them and return the first successful one
            foreach (string type in _allOutfitAssetTypes)
            {
                OutfitAssetMetadata metadata = await FetchMetadataAsync(assetId, type);
                if (metadata.IsValid)
                {
                    return metadata;
                }
            }

            return default;
        }

        private async UniTask<OutfitAssetMetadata> FetchMetadataAsync(string assetId, string type)
        {
            try
            {
                return type switch
                {
                    UgcOutfitAssetType.Gear => await FetchGearMetadataAsync(assetId),
                    StaticWearableAsset.OutfitAssetType => await FetchStaticMetadataAsync(assetId),
                    UgcOutfitAssetType.UgcDefault => await FetchUgcDefaultMetadataAsync(assetId),
                    UgcOutfitAssetType.Ugc => await FetchUgcMetadataAsync(assetId),
                    _ => default
                };
            }
            catch (Exception)
            {
                // ignore exceptions
                return default;
            }
        }

        private async UniTask<OutfitAssetMetadata> FetchGearMetadataAsync(string assetId, string lod = AssetLod.Default)
        {
            using Ref<GearElementContainer> assetRef = await _assetsService.LoadAssetAsync<GearElementContainer>(assetId, lod:lod);

            if (!assetRef.IsAlive)
            {
                return default;
            }

            // build the asset metadata and return a succeeded result
            GearElementContainer container = assetRef.Item;
            return new OutfitAssetMetadata(assetId)
            {
                Id = assetId,
                Slot = container.slot,
                Subcategory = container.subcategory,
                Species = Species,
                Type = UgcOutfitAssetType.Gear,
                CollisionData = container.collisionData.ToOutfitCollisionData(),
            };
        }

        private async UniTask<OutfitAssetMetadata> FetchStaticMetadataAsync(string assetId, string lod = AssetLod.Default)
        {
            using Ref<AssetContainer> assetRef = await _assetsService.LoadAssetAsync<AssetContainer>(assetId, lod:lod);

            if (!assetRef.IsAlive)
            {
                return default;
            }

            // build the asset metadata and return a succeeded result
            AssetContainer container = assetRef.Item;
            return new OutfitAssetMetadata(assetId)
            {
                Id = assetId,
                Slot = container.Slot,
                Subcategory = container.Subcategory,
                Species = Species,
                Type = StaticWearableAsset.OutfitAssetType,
                CollisionData = container.CollisionData.ToOutfitCollisionData(),
            };
        }

        private async UniTask<OutfitAssetMetadata> FetchUgcMetadataAsync(string assetId)
        {
            if (_wearableService is null)
            {
                return default;
            }

            // get the wearable from the API
            Ugc.Wearable wearable = await _wearableService.FetchAsync(assetId);
            if (wearable is null)
            {
                return default;
            }

            // try to load the UGC template data
            UgcTemplateData templateData = await _ugcTemplateDataService.FetchTemplateDataAsync(wearable.TemplateId);
            if (templateData is null)
            {
                return default;
            }

            // create a unique asset index key that includes the hash of the definition so we avoid reusing caching an outdated definition
            string assetIndexKey = $"{assetId}-{wearable.ComputeHash()}";

            // build the asset and return a succeeded result
            return new OutfitAssetMetadata(assetIndexKey, wearable)
            {
                Id = assetId,
                Slot = templateData.Slot,
                Subcategory = templateData.Subcategory,
                Species = Species,
                Type = UgcOutfitAssetType.Ugc,
                CollisionData = templateData.CollisionData,
            };
        }

        private async UniTask<OutfitAssetMetadata> FetchUgcDefaultMetadataAsync(string assetId)
        {
            // try to load the UGC template data
            UgcTemplateData templateData = await _ugcTemplateDataService.FetchTemplateDataAsync(assetId);
            if (templateData is null)
            {
                return default;
            }

            // build the asset and return a succeeded result
            return new OutfitAssetMetadata(assetId)
            {
                Id = assetId,
                Slot = templateData.Slot,
                Subcategory = templateData.Subcategory,
                Species = Species,
                Type = UgcOutfitAssetType.UgcDefault,
                CollisionData = templateData.CollisionData,
            };
        }
    }
}
