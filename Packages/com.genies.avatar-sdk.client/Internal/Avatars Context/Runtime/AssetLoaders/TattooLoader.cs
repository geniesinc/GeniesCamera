using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;

namespace Genies.Avatars.Context
{
    public sealed class TattooLoader : BaseAssetLoader<Texture2DAsset, TattooTemplate>
    {
        public TattooLoader(IAssetsService assetsService) : base(assetsService){}
        protected override UniTask<Texture2DAsset> FromContainer(string assetId, string lod, TattooTemplate container)
        {
            var asset = new Texture2DAsset(assetId, lod, container.Map);
            return UniTask.FromResult(asset);
        }
    }
}
