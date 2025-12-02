using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
    public class SubSpeciesMetadataService
    {
        /// <summary>
        /// SubSpecies at this point should only be supported for the UnifiedGAP species.
        /// </summary>
        public readonly string Species;

        // state
        private readonly Dictionary<string, UniTaskCompletionSource<SubSpeciesMetadata>> _cachedMetadata;


        public SubSpeciesMetadataService(string species)
        {
            Species = species;

            _cachedMetadata = new Dictionary<string, UniTaskCompletionSource<SubSpeciesMetadata>>();
        }

        public async UniTask<SubSpeciesMetadata> FetchAsync(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            if (_cachedMetadata.TryGetValue(assetId, out UniTaskCompletionSource<SubSpeciesMetadata> fetchingTask))
            {
                return await fetchingTask.Task;
            }

            _cachedMetadata[assetId] = fetchingTask = new UniTaskCompletionSource<SubSpeciesMetadata>();

            SubSpeciesMetadata metadata = await FetchStaticMetadataAsync(assetId);
            if (metadata.IsValid)
            {
                fetchingTask.TrySetResult(metadata);
                return metadata;
            }

            Debug.LogError($"[{nameof(SubSpeciesMetadataService)}] failed to fetch outfit asset metadata with ID: {assetId}");
            _cachedMetadata.Remove(assetId);
            fetchingTask.TrySetResult(default);
            return default;
        }

        private UniTask<SubSpeciesMetadata> FetchStaticMetadataAsync(string assetId)
        {
            return UniTask.FromResult(new SubSpeciesMetadata(assetId));
        }
    }
}
