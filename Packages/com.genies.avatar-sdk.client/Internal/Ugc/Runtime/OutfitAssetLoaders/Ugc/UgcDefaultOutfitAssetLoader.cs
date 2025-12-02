using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;

namespace Genies.Ugc
{
    /// <summary>
    /// <see cref="IOutfitAssetLoader"/> implementation capable of loading UGC default assets only.
    /// </summary>
    public sealed class UgcDefaultOutfitAssetLoader : OutfitAssetLoaderBase
    {
        private static readonly IReadOnlyList<string> _supportedTypes
            = new List<string> { UgcOutfitAssetType.UgcDefault }.AsReadOnly();

        public override IReadOnlyList<string> SupportedTypes => _supportedTypes;

        // dependencies
        private readonly IUgcOutfitAssetBuilder _ugcOutfitAssetBuilder;
        private readonly DefaultWearableCreator _defaultWearableCreator;

        public UgcDefaultOutfitAssetLoader(
            IUgcOutfitAssetBuilder ugcOutfitAssetBuilder,
            IAssetLoader<UgcTemplateAsset> ugcTemplateLoader)
        {
            _ugcOutfitAssetBuilder = ugcOutfitAssetBuilder;
            _defaultWearableCreator = new DefaultWearableCreator(ugcTemplateLoader);
        }

        public override bool IsOutfitAssetTypeSupported(string type)
        {
            return type == UgcOutfitAssetType.UgcDefault;
        }

        protected override async UniTask<OutfitAsset> LoadOutfitAssetAsync(OutfitAssetMetadata metadata, string genieType = GenieTypeName.NonUma, string lod = AssetLod.Default)
        {
            // create a default wearable that has no definition and build an outfit asset from it
            Wearable wearable = await _defaultWearableCreator.CreateAsync(metadata.Id);
            if (wearable is null)
            {
                return null;
            }

            OutfitAsset asset = await _ugcOutfitAssetBuilder.BuildOutfitAssetAsync(metadata.Id, wearable, metadata, lod);

            return asset;
        }
    }
}
