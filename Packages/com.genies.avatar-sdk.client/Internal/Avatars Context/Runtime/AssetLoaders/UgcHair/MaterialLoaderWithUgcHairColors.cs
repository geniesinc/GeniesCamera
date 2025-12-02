using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.DataRepositoryFramework;
using Genies.Shaders;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Special implementation for loading avatar materials that can also load the ugc hair colors.
    /// </summary>
    public class MaterialLoaderWithUgcHairColors : ISlottedAssetLoader<MaterialAsset>
    {
        private static readonly int _colorBaseId = Shader.PropertyToID("_ColorBase");
        private static readonly int _colorRId = Shader.PropertyToID("_ColorR");
        private static readonly int _colorGId = Shader.PropertyToID("_ColorG");
        private static readonly int _colorBId = Shader.PropertyToID("_ColorB");

        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IDataRepository<CustomHairColorData> _dataRepository;

        private readonly HandleCache<string, MaterialAsset> _cache;
        private readonly HashSet<string> _customIds;
        private UniTaskCompletionSource _initializationOperation;

        public MaterialLoaderWithUgcHairColors(IAssetsService assetsService, IDataRepository<CustomHairColorData> dataRepository)
        {
            _assetsService = assetsService;
            _dataRepository = dataRepository;

            _cache = new HandleCache<string, MaterialAsset>();
            _customIds = new HashSet<string>();
        }

        public async UniTask<Ref<MaterialAsset>> LoadAsync(string assetId, string slotId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            await InitializeAsync();

            // return a new reference if already loaded
            if (_cache.TryGetNewReference(assetId, out Ref<MaterialAsset> assetRef))
            {
                return assetRef;
            }

            if (slotId == UnifiedMaterialSlot.Hair)
            {
                // try to load as a custom hair color first (this can come from the data repository or avatar embedded data)
                assetRef = await LoadCustomHairAsync(assetId);
                if (assetRef.IsAlive)
                {
                    _cache.CacheHandle(assetId, assetRef);
                    return assetRef;
                }
            }

            // default material loading from the assets service
            Ref<MaterialDataContainer> containerRef = await _assetsService.LoadAssetAsync<MaterialDataContainer>(assetId, lod:lod);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            var materialAsset = new MaterialAsset(assetId, AssetLod.Default, containerRef.Item.targetMaterial);
            assetRef = CreateRef.FromDependentResource(materialAsset, containerRef);

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
                Debug.LogError($"[{nameof(MaterialLoaderWithUgcHairColors)}] couldn't fetch custom hair color IDs from the data repository\n{exception}");
            }

            _initializationOperation.TrySetResult();
            _initializationOperation = null;
        }

        private async UniTask<Ref<MaterialAsset>> LoadCustomHairAsync(string assetId)
        {
            try
            {
                CustomHairColorData data = null;
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

                var material = new Material(GeniesShaders.MegaHair.Shader);
                material.SetColor(_colorBaseId, data.ColorBase);
                material.SetColor(_colorRId,    data.ColorR);
                material.SetColor(_colorGId,    data.ColorG);
                material.SetColor(_colorBId,    data.ColorB);

                var materialAsset = new MaterialAsset(assetId, AssetLod.Default, material);
                Ref<Material> materialRef = CreateRef.FromUnityObject(material);
                return CreateRef.FromDependentResource(materialAsset, materialRef);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(MaterialLoaderWithUgcHairColors)}] something went wrong while loading custom hair color: {assetId}\n{exception}");
                return default;
            }
        }
    }
}
