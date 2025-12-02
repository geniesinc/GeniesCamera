using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;

namespace Genies.Avatars.Context
{
    public sealed class BlendShapePresetLoader : IAssetLoader<BlendShapePresetAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IAssetLoader<BlendShapeAsset> _blendShapeLoader;

        // state
        private readonly Dictionary<string, UniTaskCompletionSource<BlendShapePresetAsset>> _cache;

        public BlendShapePresetLoader(IAssetsService assetsService, IAssetLoader<BlendShapeAsset> blendShapeLoader = null)
        {
            _assetsService = assetsService;
            _blendShapeLoader = blendShapeLoader ?? new BlendShapeLoader(assetsService);
            _cache = new Dictionary<string, UniTaskCompletionSource<BlendShapePresetAsset>>();
        }

        public async UniTask<Ref<BlendShapePresetAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            BlendShapePresetAsset asset;

            if (_cache.TryGetValue(assetId, out UniTaskCompletionSource<BlendShapePresetAsset> assetLoadingTask))
            {
                asset = await assetLoadingTask.Task;
                return CreateRef.FromAny(asset);
            }

            _cache[assetId] = assetLoadingTask = new UniTaskCompletionSource<BlendShapePresetAsset>();
            using Ref<BlendshapeDataFacePresetContainer> presetRef = await _assetsService.LoadAssetAsync<BlendshapeDataFacePresetContainer>(assetId, lod:lod);
            if (!presetRef.IsAlive)
            {
                _cache.Remove(assetId);
                assetLoadingTask.TrySetResult(null);
                return default;
            }

            BlendshapeDataFacePresetContainer container = presetRef.Item;

            // load all the blend shape assets for the preset
            var tasks = new List<UniTask<Ref<BlendShapeAsset>>>(container.blendShapeIds.Count);
            foreach (string blendShapeId in container.blendShapeIds)
            {
                tasks.Add(_blendShapeLoader.LoadAsync(blendShapeId, lod));
            }

            Ref<BlendShapeAsset>[] assetRefs = await UniTask.WhenAll(tasks);
            BlendShapeAsset[] assets = assetRefs.Select(assetRef =>
            {
                BlendShapeAsset blendShapeAsset = assetRef.Item;
                // we don't need to keep this ref alive as the BlendShapeAsset doesn't really allocate releasable assets
                assetRef.Dispose();
                return blendShapeAsset;
            }).ToArray();

            asset = new BlendShapePresetAsset(assetId, assets);

            // keep the result cached forever
            assetLoadingTask.TrySetResult(asset);
            return CreateRef.FromAny(asset);
        }
    }
}
