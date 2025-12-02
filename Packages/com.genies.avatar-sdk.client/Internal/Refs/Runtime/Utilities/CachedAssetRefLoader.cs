using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Refs
{
    /// <summary>
    /// Utility class to easily implement asset ref loaders that properly cache already loaded assets and ongoing operations.
    /// </summary>
    public abstract class CachedAssetRefLoader<TKey, TResource>
    {
        // state
        private readonly Dictionary<TKey, UniTaskCompletionSource<Ref<TResource>>> _loadingOperations = new();
        private readonly HandleCache<TKey, TResource> _cache = new();
        
        protected abstract bool ValidateKey(ref TKey key);
        protected abstract UniTask<Ref<TResource>> LoadAssetAsync(TKey key);

        public async UniTask<Ref<TResource>> CachedLoadAssetAsync(TKey key)
        {
            if (!ValidateKey(ref key))
            {
                return default;
            }

            // if the asset was already loading then await for it and return a new reference
            if (_loadingOperations.TryGetValue(key, out UniTaskCompletionSource<Ref<TResource>> loadingOperation))
            {
                return (await loadingOperation.Task).New();
            }

            // if the asset was loaded before and is still loaded then return a new reference to it
            if (_cache.TryGetNewReference(key, out Ref<TResource> assetRef))
            {
                return assetRef;
            }

            // start a loading operation
            _loadingOperations[key] = loadingOperation = new UniTaskCompletionSource<Ref<TResource>>();

            try
            {
                assetRef = await LoadAssetAsync(key);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CachedAssetRefLoader] something went wrong while loading the asset with key {key}\n{exception}");
            }
            
            _cache.CacheHandle(key, assetRef);
            _loadingOperations.Remove(key);
            loadingOperation.TrySetResult(assetRef);

            return assetRef;
        }
    }
}
