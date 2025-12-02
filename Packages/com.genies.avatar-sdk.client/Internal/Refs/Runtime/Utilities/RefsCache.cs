using System.Collections.Generic;

namespace Genies.Refs
{
    /// <summary>
    /// Cache of references.
    /// If the max count is set to 0 or any negative number, the cache will have no size limit.
    /// </summary>
    public sealed class RefsCache
    {
        public int Count => _cache.Count;

        public int MaxCount { get => _maxCount; set { _maxCount = value; CheckSizeLimit(); } }

        private readonly Queue<Ref> _cache = new Queue<Ref>();
        private readonly HashSet<object> _cachedItems = new HashSet<object>();

        private int _maxCount;

        public RefsCache(int maxCount = 0)
            => _maxCount = maxCount;
        
        public void CacheReferences<T>(bool createNew, params Ref<T>[] references)
        {
            foreach (Ref<T> reference in references)
            {
                CacheReference(createNew ? reference.New() : reference);
            }
        }
        
        public void CacheReferences<T>(IEnumerable<Ref<T>> references, bool createNew = false)
        {
            foreach(Ref<T> reference in references)
            {
                CacheReference(createNew ? reference.New() : reference);
            }
        }
        
        public void CacheReferences(bool createNew, params Ref[] references)
        {
            foreach(Ref reference in references)
            {
                CacheReference(createNew ? reference.New() : reference);
            }
        }
        
        public void CacheReferences(IEnumerable<Ref> references, bool createNew = false)
        {
            foreach(Ref reference in references)
            {
                CacheReference(createNew ? reference.New() : reference);
            }
        }

        /// <summary>
        /// Caches the given reference so the item it holds never gets disposed until released from the cache. Please note that no new reference
        /// will be created from the given one, so the reference that you pass will be owned by the RefsCache.
        /// </summary>
        public void CacheReference(Ref reference)
        {
            if (!reference.IsAlive)
            {
                return;
            }

            if (_cachedItems.Contains(reference.Item))
            {
                reference.Dispose();
                return;
            }

            _cache.Enqueue(reference);
            _cachedItems.Add(reference.Item);

            CheckSizeLimit();
        }

        public void ReleaseCache()
        {
            foreach (Ref reference in _cache)
            {
                reference.Dispose();
            }

            _cache.Clear();
            _cachedItems.Clear();
        }

        public bool IsItemCached(object item)
            => _cachedItems.Contains(item);

        public bool IsItemCached(Ref reference)
            => _cachedItems.Contains(reference.Item);

        private void CheckSizeLimit()
        {
            if (MaxCount <= 0)
            {
                return;
            }

            // check if we exceeded the cache max count, in that case release the oldest cached reference
            while (_cache.Count > MaxCount)
            {
                var firstInQueue = _cache.Dequeue();
                _cachedItems.Remove(firstInQueue.Item);
                firstInQueue.Dispose();
            }
        }
    }
}