using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Base implementation of <see cref="IOutfitAssetLoader"/>. The implementer only needs to care about the logic for loading
    /// an <see cref="OutfitAsset"/> instance from <see cref="OutfitAssetMetadata"/> and <see cref="string"/>.
    /// </summary>
    public abstract class OutfitAssetLoaderBase : CachedAssetRefLoader<(OutfitAssetMetadata, string), OutfitAsset>, IOutfitAssetLoader
    {
        public abstract IReadOnlyList<string> SupportedTypes { get; }
        public abstract bool IsOutfitAssetTypeSupported(string type);
        protected abstract UniTask<OutfitAsset> LoadOutfitAssetAsync(OutfitAssetMetadata metadata, string genieType = GenieTypeName.NonUma, string lod = AssetLod.Default);

        public UniTask<Ref<OutfitAsset>> LoadAsync(OutfitAssetMetadata metadata, string lod = AssetLod.Default)
        {
            return CachedLoadAssetAsync((metadata, lod));
        }

        protected override bool ValidateKey(ref (OutfitAssetMetadata, string) key)
        {
            return key.Item1.IsValid;
        }

        protected override async UniTask<Ref<OutfitAsset>> LoadAssetAsync((OutfitAssetMetadata, string) key)
        {
            OutfitAssetMetadata metadata = key.Item1;
            string lod = key.Item2;

            try
            {
                OutfitAsset asset = await LoadOutfitAssetAsync(metadata, lod);
                if (asset is not null && !asset.IsDisposed)
                {
                    return CreateRef.FromDisposable(asset);
                }

                Debug.LogWarning($"[{nameof(OutfitAssetLoaderBase)}] failed to load the {metadata.Type} outfit asset with ID: {metadata.Id}");
                return default;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(OutfitAssetLoaderBase)}] exception thrown while loading the {metadata.Type} outfit asset with ID: {metadata.Id}\n{exception}");
                return default;
            }
        }
    }
}
