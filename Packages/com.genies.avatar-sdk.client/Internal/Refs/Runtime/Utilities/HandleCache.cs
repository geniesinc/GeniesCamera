using System.Collections.Generic;

namespace Genies.Refs
{
    /// <summary>
    /// Simple handle cache that only caches and returns alive handles.
    /// </summary>
    public sealed class HandleCache<TKey, TResource>
    {
        public int Count => _cache.Count;
        
        private readonly Dictionary<TKey, Handle<TResource>> _cache = new();
        
        /// <summary>
        /// Caches the given reference's handle with the given key. Nothing will be cached if the handle is not alive.
        /// </summary>
        public void CacheHandle(TKey key, Ref<TResource> reference)
        {
            CacheHandle(key, reference.Handle);
        }

        /// <summary>
        /// Caches the given handle with the given key. Nothing will be cached if the handle is not alive.
        /// </summary>
        public void CacheHandle(TKey key, Handle<TResource> handle)
        {
            if (!handle.IsAlive)
            {
                return;
            }

            _cache[key] = handle;
        }

        /// <summary>
        /// Releases the handle cached with the given key, if any.
        /// </summary>
        public void Release(TKey key)
        {
            if (key is not null)
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Tries to get a cached handle with the given key. Only returns alive handles.
        /// </summary>
        public bool TryGetHandle(TKey key, out Handle<TResource> handle)
        {
            if (key is null)
            {
                handle = default;
                return false;
            }
            
            if (!_cache.TryGetValue(key, out handle))
            {
                return false;
            }

            if (handle.IsAlive)
            {
                return true;
            }

            _cache.Remove(key);
            return false;
        }

        /// <summary>
        /// If there is a cached alive handle for the given key, it will return a new
        /// reference from it.
        /// </summary>
        public bool TryGetNewReference(TKey key, out Ref<TResource> reference)
        {
            if (TryGetHandle(key, out Handle<TResource> handle))
            {
                reference = CreateRef.FromHandle(handle);
                return true;
            }
            
            reference = default;
            return false;
        }

        /// <summary>
        /// Whether or not there is a cached alive handle with the given key.
        /// </summary>
        public bool IsHandleCached(TKey key)
        {
            return TryGetHandle(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Non-generic version of <see cref="HandleCache{TKey,TResource}"/>.
    /// </summary>
    public sealed class HandleCache<TKey>
    {
        public int Count => _cache.Count;
        
        private readonly Dictionary<TKey, Handle> _cache = new();
        
        /// <summary>
        /// Caches the given reference's handle with the given key. Nothing will be cached if the handle is not alive.
        /// </summary>
        public void CacheHandle(TKey key, Ref reference)
        {
            CacheHandle(key, reference.Handle);
        }

        /// <summary>
        /// Caches the given handle with the given key. Nothing will be cached if the handle is not alive.
        /// </summary>
        public void CacheHandle(TKey key, Handle handle)
        {
            if (!handle.IsAlive)
            {
                return;
            }

            _cache[key] = handle;
        }

        /// <summary>
        /// Releases the handle cached with the given key, if any.
        /// </summary>
        public void Release(TKey key)
        {
            if (key is not null)
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Tries to get a cached handle with the given key. Only returns alive handles.
        /// </summary>
        public bool TryGetHandle(TKey key, out Handle handle)
        {
            if (key is null)
            {
                handle = default;
                return false;
            }
            
            if (!_cache.TryGetValue(key, out handle))
            {
                return false;
            }

            if (handle.IsAlive)
            {
                return true;
            }

            _cache.Remove(key);
            return false;
        }

        /// <summary>
        /// If there is a cached alive handle for the given key, it will return a new
        /// reference from it.
        /// </summary>
        public bool TryGetNewReference(TKey key, out Ref reference)
        {
            if (TryGetHandle(key, out Handle handle))
            {
                reference = handle.NewReference();
                return true;
            }
            
            reference = default;
            return false;
        }

        /// <summary>
        /// Whether or not there is a cached alive handle with the given key.
        /// </summary>
        public bool IsHandleCached(TKey key)
        {
            return TryGetHandle(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}