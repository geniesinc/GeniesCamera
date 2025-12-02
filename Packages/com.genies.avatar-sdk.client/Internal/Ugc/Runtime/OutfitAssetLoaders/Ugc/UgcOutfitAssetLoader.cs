using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;

namespace Genies.Ugc
{
    /// <summary>
    /// <see cref="IOutfitAssetLoader"/> implementation capable of loading UGC assets only.
    /// </summary>
    public sealed class UgcOutfitAssetLoader : OutfitAssetLoaderBase
    {
        private static readonly IReadOnlyList<string> _supportedTypes
            = new List<string> { UgcOutfitAssetType.Ugc }.AsReadOnly();

        public override IReadOnlyList<string> SupportedTypes => _supportedTypes;

        // dependencies
        private readonly IUgcOutfitAssetBuilder _ugcOutfitAssetBuilder;
        private readonly IUgcWearableDefinitionService _ugcWearableDefinitionService;

        public UgcOutfitAssetLoader(
            IUgcOutfitAssetBuilder ugcOutfitAssetBuilder,
            IUgcWearableDefinitionService ugcWearableDefinitionService)
        {
            _ugcOutfitAssetBuilder = ugcOutfitAssetBuilder;
            _ugcWearableDefinitionService = ugcWearableDefinitionService;
        }

        public override bool IsOutfitAssetTypeSupported(string type)
        {
            return type == UgcOutfitAssetType.Ugc;
        }

        protected override async UniTask<OutfitAsset> LoadOutfitAssetAsync(OutfitAssetMetadata metadata, string genieType = GenieTypeName.NonUma, string lod = AssetLod.Default)
        {
            /**
             * Try to get the wearable definition from the metadata first, this is not a good practise
             * but we do this because we are already fetching it when getting the metadata. We should
             * refactor it in the future. If there is no definition in the metadata, try to fetch from
             * the service.
             */
            Wearable wearable = metadata.GetUgcwWearable() as Wearable ?? await _ugcWearableDefinitionService.FetchAsync(metadata.Id);

            // build the outfit asset from the wearable and metadata
            OutfitAsset asset = await _ugcOutfitAssetBuilder.BuildOutfitAssetAsync(metadata.Id, wearable, metadata, lod);

            return asset;
        }
    }
}
