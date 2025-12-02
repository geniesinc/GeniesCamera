using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IOutfitAssetLoader"/> implementation capable of loading static assets only.
    /// </summary>
    public sealed class StaticOutfitAssetLoader : OutfitAssetLoaderBase
    {
        private static readonly IReadOnlyList<string> _supportedTypes
            = new List<string> { StaticWearableAsset.OutfitAssetType }.AsReadOnly();

        public override IReadOnlyList<string> SupportedTypes => _supportedTypes;

        // dependencies
        private readonly IAssetLoader<StaticWearableAsset> _staticWearableLoader;

        public StaticOutfitAssetLoader(IAssetLoader<StaticWearableAsset> staticWearableLoader)
        {
            _staticWearableLoader = staticWearableLoader;
        }

        public override bool IsOutfitAssetTypeSupported(string type)
        {
            return type == StaticWearableAsset.OutfitAssetType;
        }

        protected override bool ValidateKey(ref (OutfitAssetMetadata, string) key)
        {
            return key.Item1.IsValid && !string.IsNullOrEmpty(key.Item2);
        }

        protected override async UniTask<OutfitAsset> LoadOutfitAssetAsync(OutfitAssetMetadata metadata, string genieType = GenieTypeName.NonUma, string lod = AssetLod.Default)
        {
            Ref<StaticWearableAsset> assetRef = await _staticWearableLoader.LoadAsync(metadata.Id);
            if (!assetRef.IsAlive)
            {
                return default;
            }

            StaticWearableAsset asset = assetRef.Item;

            // create an outfit asset metadata that owns the asset ref
            var outfitAsset = new OutfitAsset(
                genieType,
                lod,
                metadata,
                asset.Recipe,
                asset.Slots,
                asset.Overlays,
                asset.ComponentCreators,
                assetRef
            );

            return outfitAsset;
        }
    }
}
