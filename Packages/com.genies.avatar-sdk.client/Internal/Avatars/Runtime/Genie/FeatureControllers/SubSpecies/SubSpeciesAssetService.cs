using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Components.ShaderlessTools;
using Genies.Models;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// Service for loading sub-species assets, which are collections of species and their associated data.
    /// </summary>
    public class SubSpeciesAssetService : ISubSpeciesAssetService
    {
        // dependencies
        private readonly IAssetsService _assetsService;

        public SubSpeciesAssetService(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<SubSpeciesContainer>> LoadContainerAsync(string id, string lod = AssetLod.Default)
        {
            Ref<SubSpeciesContainer> containerRef = await _assetsService.LoadAssetAsync<SubSpeciesContainer>(id, lod:lod);

            return containerRef;
        }
    }
}
