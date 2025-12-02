using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Models;
using Genies.Refs;

namespace Genies.Avatars.Context
{
    public class FlairLoader : ISlottedAssetLoader<FlairAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;

        public FlairLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<FlairAsset>> LoadAsync(string assetId, string slotId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            Ref<FlairContainer> templateRef = await _assetsService.LoadAssetAsync<FlairContainer>(assetId, lod:lod);
            if (!templateRef.IsAlive)
            {
                return default;
            }

            var flairAsset = CreateFlairAsset(templateRef.Item, assetId, slotId, lod);

            return CreateRef.FromDependentResource(flairAsset, templateRef);
        }

        public FlairAsset CreateFlairAsset(FlairContainer container, string assetId, string slotId, string lod = AssetLod.Default)
        {
            return new FlairAsset(
                assetId,
                lod,
                slotId,
                container.GetTexture(TextureMapType.AlbedoTransparency),
                container.GetTexture(TextureMapType.Normal),
                container.GetTexture(TextureMapType.MetallicSmoothness),
                container.GetTexture(TextureMapType.RgbaMask));
        }
    }
}
