using System.Collections.Generic;
using System.Linq;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// A cache of <see cref="IAsset"/> references by their ID.
    /// </summary>
    public sealed class AssetReferencesCache<TAsset>
        where TAsset : IAsset
    {
        private readonly Dictionary<string, Ref<TAsset>> _cache;

        public AssetReferencesCache()
        {
            _cache = new Dictionary<string, Ref<TAsset>>();
        }

        /// <summary>
        /// Gets an array with all the currently cached asset IDs.
        /// </summary>
        public string[] GetCachedIds()
        {
            return _cache.Keys.ToArray();
        }

        /// <summary>
        /// Populates the given <see cref="assetIds"/> collection with the currently cached asset IDs.
        /// It does not clear the collection.
        /// </summary>
        public void GetCachedIds(ICollection<string> assetIds)
        {
            if (assetIds is null)
            {
                return;
            }

            foreach (string assetId in _cache.Keys)
            {
                assetIds.Add(assetId);
            }
        }

        /// <summary>
        /// If there is an asset reference cached for the given ID, it will return its referenced asset.
        /// If you want to get a new reference from the cached asset reference then use <see cref="TryGetNewReference"/> instead.
        /// </summary>
        public bool TryGetAsset(string assetId, out TAsset asset)
        {
            if (TryGetAliveReference(assetId, out Ref<TAsset> assetRef))
            {
                asset = assetRef.Item;
                return true;
            }
            
            asset = default;
            return false;
        }
        
        /// <summary>
        /// If there is an asset reference cached for the given ID, it will create and return a new reference from it
        /// that must be owned by the caller.
        /// </summary>
        public bool TryGetNewReference(string assetId, out Ref<TAsset> assetRef)
        {
            if (TryGetAliveReference(assetId, out assetRef))
            {
                assetRef = assetRef.New();
                return true;
            }
            
            assetRef = default;
            return false;
        }

        /// <summary>
        /// Caches the given asset reference.
        /// </summary>
        public void Cache(Ref<TAsset> assetRef)
        {
            if (!assetRef.IsAlive || assetRef.Item?.Id is null)
            {
                assetRef.Dispose();
                return;
            }
            
            // make sure we dispose any previously cached reference for the same asset ID
            if (_cache.TryGetValue(assetRef.Item.Id, out Ref<TAsset> previousAssetRef) && previousAssetRef != assetRef)
            {
                previousAssetRef.Dispose();
            }

            _cache[assetRef.Item.Id] = assetRef;
        }

        /// <summary>
        /// Disposes the cached reference for the given asset ID, if any.
        /// </summary>
        public void Release(string assetId)
        {
            if (!TryGetAliveReference(assetId, out Ref<TAsset> assetRef))
            {
                return;
            }

            assetRef.Dispose();
            _cache.Remove(assetId);
        }
        
        /// <summary>
        /// Disposes all cached asset references.
        /// </summary>
        public void ReleaseAllReferences()
        {
            foreach (Ref<TAsset> assetRef in _cache.Values)
            {
                assetRef.Dispose();
            }

            _cache.Clear();
        }
        
        /// <summary>
        /// Whether or not there is a cached asset reference for the given asset ID.
        /// </summary>
        public bool IsReferenceCached(string assetId)
        {
            return TryGetAliveReference(assetId, out _);
        }
        
        // convenient method to check if asset ID is null and also remove any dead references from the cache
        private bool TryGetAliveReference(string assetId, out Ref<TAsset> assetRef)
        {
            if (assetId is null || !_cache.TryGetValue(assetId, out assetRef))
            {
                assetRef = default;
                return false;
            }
            
            if (assetRef.IsAlive)
            {
                return true;
            }

            // there was a cached reference but it was dead, so just remove it from the cache
            _cache.Remove(assetId);
            return false;
        }
    }
}