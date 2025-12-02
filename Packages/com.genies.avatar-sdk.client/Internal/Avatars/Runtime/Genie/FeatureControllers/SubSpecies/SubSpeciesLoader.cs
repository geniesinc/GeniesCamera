using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using Genies.Models;

namespace Genies.Avatars
{
    public class SubSpeciesLoader : ISubSpeciesLoader
    {

        private readonly ISubSpeciesAssetService _assetsService;

        public SubSpeciesLoader(ISubSpeciesAssetService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<SubSpeciesAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            Ref<SubSpeciesContainer> containerRef = await _assetsService.LoadContainerAsync(assetId, lod);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            var asset = new SubSpeciesAsset(containerRef.Item, lod);
            containerRef.Dispose();
            return CreateRef.FromAny(asset);
        }
    }
}
