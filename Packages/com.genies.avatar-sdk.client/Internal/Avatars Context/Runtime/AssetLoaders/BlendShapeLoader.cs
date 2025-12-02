using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;

namespace Genies.Avatars.Context
{
    public sealed class BlendShapeLoader : IAssetLoader<BlendShapeAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;

        // state
        private readonly Dictionary<string, UniTaskCompletionSource<BlendShapeAsset>> _cache;

        public BlendShapeLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
            _cache = new Dictionary<string, UniTaskCompletionSource<BlendShapeAsset>>();
        }

        public async UniTask<Ref<BlendShapeAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            BlendShapeAsset asset;

            if (_cache.TryGetValue(assetId, out UniTaskCompletionSource<BlendShapeAsset> assetLoadingTask))
            {
                asset = await assetLoadingTask.Task;
                return CreateRef.FromAny(asset);
            }

            _cache[assetId] = assetLoadingTask = new UniTaskCompletionSource<BlendShapeAsset>();

            // TODO we should build the content without the BlendShapeContainer_ prefix
            using Ref<BlendShapeDataContainer> containerRef = await _assetsService.LoadAssetAsync<BlendShapeDataContainer>($"BlendShapeContainer_{assetId}", lod:lod);
            if (!containerRef.IsAlive)
            {
                _cache.Remove(assetId);
                assetLoadingTask.TrySetResult(null);
                return default;
            }

            string slot = containerRef.Item.Type.ToString();
            DnaEntry[] dna = GetDnaArray(containerRef.Item.DNA);
            asset = new BlendShapeAsset(assetId, AssetLod.Default, slot, dna);

            // keep the result cached forever
            assetLoadingTask.TrySetResult(asset);
            return CreateRef.FromAny(asset);
        }

        private static DnaEntry[] GetDnaArray(List<DNAItem> dna)
        {
            var entries = new DnaEntry[dna.Count];

            for (int i = 0; i < entries.Length; ++i)
            {
                entries[i] = new DnaEntry() { Name = dna[i].Name, Value = dna[i].Value };
            }

            return entries;
        }
    }
}
