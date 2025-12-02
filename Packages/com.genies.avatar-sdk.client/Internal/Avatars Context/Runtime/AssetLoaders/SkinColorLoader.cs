using System;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class SkinColorLoader : IAssetLoader<ColorAsset>
    {
        private readonly IAssetsService _assetsService;

        public SkinColorLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<ColorAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            // try to load as a custom skin color from embedded data
            Ref<ColorAsset> assetRef = LoadCustomSkinColor(assetId);
            if (assetRef.IsAlive)
            {
                return assetRef;
            }

            using Ref<MaterialDataSkinContainer> containerRef = await _assetsService.LoadAssetAsync<MaterialDataSkinContainer>(assetId, lod:lod);
            if (!containerRef.IsAlive || !containerRef.Item.targetMaterial)
            {
                return default;
            }

            var asset = new ColorAsset(assetId, containerRef.Item.IconColor);

            return CreateRef.FromAny(asset);
        }

        private Ref<ColorAsset> LoadCustomSkinColor(string assetId)
        {
            try
            {
                // if we cannot fetch the data from the repository then fallback to the AvatarEmbeddedData
                if (!AvatarEmbeddedData.TryGetData(assetId, out SkinColorData data))
                {
                    return default;
                }

                var colorAsset = new ColorAsset(assetId, data.BaseColor);
                return CreateRef.FromAny(colorAsset);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(SkinColorLoader)}] something went wrong while loading custom skin color: {assetId}\n{exception}");
                return default;
            }
        }
    }
}
