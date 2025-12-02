using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Implementation of <see cref="IOutfitAssetLoader"/> that must be initialized with a collection of other
    /// <see cref="IOutfitAssetLoader"/> implementations capable of loading different outfit asset types.
    /// </summary>
    public sealed class GroupedOutfitAssetLoader : IOutfitAssetLoader
    {
        public IReadOnlyList<string> SupportedTypes { get; }

        // dependencies
        private readonly Dictionary<string, IOutfitAssetLoader> _loadersByType;

        public GroupedOutfitAssetLoader(params IOutfitAssetLoader[] loaders)
            : this(loaders as IEnumerable<IOutfitAssetLoader>) { }

        public GroupedOutfitAssetLoader(IEnumerable<IOutfitAssetLoader> loaders)
        {
            _loadersByType = new Dictionary<string, IOutfitAssetLoader>();

            foreach (IOutfitAssetLoader loader in loaders)
            {
                if (loader is null)
                {
                    continue;
                }

                foreach (string type in loader.SupportedTypes)
                {
                    if (type != null)
                    {
                        _loadersByType[type] = loader;
                    }
                }
            }

            SupportedTypes = _loadersByType.Keys.ToList().AsReadOnly();
        }

        public UniTask<Ref<OutfitAsset>> LoadAsync(OutfitAssetMetadata metadata, string lod = AssetLod.Default)
        {
            if (!metadata.IsValid)
            {
                return UniTask.FromResult<Ref<OutfitAsset>>(default);
            }

            if (!string.IsNullOrEmpty(metadata.Type) && _loadersByType.TryGetValue(metadata.Type, out IOutfitAssetLoader loader))
            {
                return loader.LoadAsync(metadata, lod);
            }

            Debug.LogError($"[{nameof(GroupedOutfitAssetLoader)}] no outfit asset loader was found for outfit asset type: {metadata.Type}");
            return UniTask.FromResult<Ref<OutfitAsset>>(default);
        }

        public bool IsOutfitAssetTypeSupported(string type)
        {
            return _loadersByType.ContainsKey(type);
        }
    }
}
