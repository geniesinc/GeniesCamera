using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Models.MaterialData;
using Genies.Refs;

namespace Genies.Avatars.Context
{
    public sealed class MakeupColorLoader : IAssetLoader<MakeupColorAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;

        public MakeupColorLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<MakeupColorAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            using Ref<MaterialDataMakeupColor> makeupColorRef = await _assetsService.LoadAssetAsync<MaterialDataMakeupColor>(assetId, lod:lod);
            if (!makeupColorRef.IsAlive)
            {
                return default;
            }

            var asset = new MakeupColorAsset(assetId, makeupColorRef.Item.IconColor, makeupColorRef.Item.IconColor2, makeupColorRef.Item.IconColor3);

            return CreateRef.FromAny(asset);
        }
    }
}
