using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using Genies.Utilities;

namespace Genies.Avatars.Context
{
    public sealed class UgcTemplateLoader : IAssetLoader<UgcTemplateAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IUgcTemplateDataService _templateDataService;

        public UgcTemplateLoader(IAssetsService assetsService, IUgcTemplateDataService templateDataService)
        {
            _assetsService = assetsService;
            _templateDataService = templateDataService;
        }

        public async UniTask<Ref<UgcTemplateAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            // fetch template data first
            UgcTemplateData templateData = await _templateDataService.FetchTemplateDataAsync(assetId);
            if (templateData is null)
            {
                return default;
            }

            // fetch the UgcTemplate asset from the content build
            Ref<UgcTemplate> templateRef = await _assetsService.LoadAssetAsync<UgcTemplate>(templateData.TemplateId, lod:lod);
            if (!templateRef.IsAlive || !templateRef.Item)
            {
                return default;
            }

            UgcTemplate template = templateRef.Item;

            var asset = new UgcTemplateAsset(
                templateData,
                template.MeshHideAssets.ToArray(),
                template.extras.GetAssets<IGenieComponentCreator>().ToArray()
            );

            // create a new ref to the template asset that is linked to the UgcTemplate ref loaded from the assets service
            Ref<UgcTemplateAsset> assetRef = CreateRef.FromDependentResource(asset, templateRef);
            return assetRef;
        }
    }
}
