using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.DataRepositoryFramework;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Special implementation for loading skin colors that can also load the ugc colors.
    /// </summary>
    public class SkinColorLoaderWithUgcColors : IAssetLoader<ColorAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IDataRepository<SkinColorData> _dataRepository;

        private readonly HandleCache<string, ColorAsset> _cache;
        private readonly HashSet<string> _customIds;
        private UniTaskCompletionSource _initializationOperation;

        public SkinColorLoaderWithUgcColors(IAssetsService assetsService, IDataRepository<SkinColorData> dataRepository)
        {
            _assetsService = assetsService;
            _dataRepository = dataRepository;

            _cache = new HandleCache<string, ColorAsset>();
            _customIds = new HashSet<string>();
        }

        public async UniTask<Ref<ColorAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            await InitializeAsync();

            // return a new reference if already loaded
            if (_cache.TryGetNewReference(assetId, out Ref<ColorAsset> assetRef))
            {
                return assetRef;
            }

            // try to load as a custom skin color first (this can come from the data repository or avatar embedded data)
            assetRef = await LoadCustomSkinColorAsync(assetId);
            if (assetRef.IsAlive)
            {
                _cache.CacheHandle(assetId, assetRef);
                return assetRef;
            }

            // default skin color loading from the assets service
            using Ref<MaterialDataSkinContainer> containerRef = await _assetsService.LoadAssetAsync<MaterialDataSkinContainer>(assetId, lod:lod);
            if (!containerRef.IsAlive || !containerRef.Item.targetMaterial)
            {
                return default;
            }

            var asset = new ColorAsset(assetId, containerRef.Item.IconColor);
            assetRef = CreateRef.FromAny(asset);

            _cache.CacheHandle(assetId, assetRef);
            return assetRef;
        }

        private async UniTask InitializeAsync()
        {
            if (_initializationOperation is not null)
            {
                await _initializationOperation.Task;
                return;
            }

            _initializationOperation = new UniTaskCompletionSource();

            try
            {
                List<string> customIds = await _dataRepository.GetIdsAsync();
                _customIds.UnionWith(customIds);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(SkinColorLoaderWithUgcColors)}] couldn't fetch custom skin color IDs from the data repository\n{exception}");
            }

            _initializationOperation.TrySetResult();
            _initializationOperation = null;
        }

        private async UniTask<Ref<ColorAsset>> LoadCustomSkinColorAsync(string assetId)
        {
            try
            {
                SkinColorData data = null;
                if (_customIds.Contains(assetId))
                {
                    data = await _dataRepository.GetByIdAsync(assetId);

                    // register to avatar embedded data every time we successfully load from data repository
                    if (data is not null)
                    {
                        AvatarEmbeddedData.SetData(assetId, data);
                    }
                }

                // if we cannot fetch the data from the repository then fallback to the AvatarEmbeddedData
                if (data is null && !AvatarEmbeddedData.TryGetData(assetId, out data))
                {
                    return default;
                }

                var colorAsset = new ColorAsset(assetId, data.BaseColor);
                return CreateRef.FromAny(colorAsset);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(SkinColorLoaderWithUgcColors)}] something went wrong while loading custom skin color: {assetId}\n{exception}");
                return default;
            }
        }
    }
}
