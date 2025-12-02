using System;
using System.Collections.Generic;
using Genies.Refs;
using UMA;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Our own wrapper on top of the <see cref="UMAAssetIndexer"/> that leverages our Refs framework to fix some issues
    /// present with the default UMA implementation and enforce better resource management practises.
    /// <br/><br/>
    /// The main issue solved by this implementation is the edge case where we have the same asset instance duplicated
    /// (because it comes from two different Addressable bundles but it originally was the same asset) but it can only
    /// be indexed once because UMA uses the instance name as key. This implementation ensures that both instances can
    /// be "indexed" at the same time, and releasing one will not affect the index state of the other. Of course this
    /// assumes that whenever you index two instances resolving to the same key they are both exact copies, since only
    /// one of them can be registered to the UMA index at the same time.
    /// </summary>
    public sealed class GeniesAssetIndexer
    {
        public static readonly GeniesAssetIndexer Instance = new(UMAAssetIndexer.Instance);
        
        private readonly UMAAssetIndexer _indexer;
        private readonly HandleCache<Object, Object> _handleCache;
        private readonly Dictionary<Type, Dictionary<string, IndexedResource>> _umaIndexedResourcesByType;

        public GeniesAssetIndexer(UMAAssetIndexer indexer)
        {
            _indexer = indexer;
            _handleCache = new HandleCache<Object, Object>();
            _umaIndexedResourcesByType = new Dictionary<Type, Dictionary<string, IndexedResource>>();
        }

        /// <summary>
        /// Adds the given asset to the index and returns a reference that must be disposed to release it from the index.
        /// </summary>
        public Ref<Object> Index(Object asset)
        {
            if (!asset)
            {
                return default;
            }

            // if this asset was already indexed then return a new reference
            if (_handleCache.TryGetNewReference(asset, out Ref<Object> indexRef))
            {
                return indexRef;
            }

            // if the asset type is not indexed by UMA then return a dummy ref
            Type type = asset.GetType();
            if (!_indexer.IsIndexedType(type))
            {
                return CreateRef.FromAny(asset);
            }

            // create an indexed resource for the asset
            IndexedResource resource;
            string key = AssetItem.GetEvilName(asset);
            Dictionary<string, IndexedResource> umaIndexedResources = GetUmaIndexedResourcesForType(type);
            
            if (umaIndexedResources.TryGetValue(key, out IndexedResource indexedResource))
            {
                resource = indexedResource.IndexCopy(asset); // if the key is already in use then index a copy
            }
            else
            {
                resource = IndexedResource.Index(key, asset, this);
            }

            // create a new reference from the indexed resource and cache its handle
            indexRef = CreateRef.From(resource);
            _handleCache.CacheHandle(asset, indexRef);
            
            return indexRef;
        }

        private void AddToUmaIndex(IndexedResource resource)
        {
            _indexer.ProcessNewItem(resource.Resource, isAddressable: false, keepLoaded: false);
            // we must track the current indexed resource in case we need to do indexed copies
            Dictionary<string, IndexedResource> umaIndexedResources = GetUmaIndexedResourcesForType(resource.Resource.GetType());
            umaIndexedResources[resource.Key] = resource;
        }

        private void RemoveFromUmaIndex(IndexedResource resource)
        {
            _indexer.ReleaseReference(resource.Resource);
            Dictionary<string, IndexedResource> umaIndexedResources = GetUmaIndexedResourcesForType(resource.Resource.GetType());
            umaIndexedResources.Remove(resource.Key);
        }

        private Dictionary<string, IndexedResource> GetUmaIndexedResourcesForType(Type type)
        {
            type = _indexer.GetRuntimeType(type);
            if (!_umaIndexedResourcesByType.TryGetValue(type, out Dictionary<string, IndexedResource> umaIndexedResources))
            {
                umaIndexedResources = _umaIndexedResourcesByType[type] = new Dictionary<string, IndexedResource>();
            }

            return umaIndexedResources;
        }
        
        private sealed class IndexedResource : IResource<Object>
        {
            [ThreadStatic]
            private static Stack<IndexedResource> _pool;
            
            public string Key { get; private set; }
            public Object Resource { get; private set; }
            
            private GeniesAssetIndexer _indexer;
            private IndexedResource _previous;
            private IndexedResource _next;

            private IndexedResource() { }
            public static IndexedResource Index(string key, Object resource, GeniesAssetIndexer indexer)
            {
                IndexedResource instance = New(key, resource, indexer);
                instance.AddToUmaIndex();
                
                return instance;
            }
            
            /// <summary>
            /// Assumes the given resource is a copy of the current Resource that must be indexed with the same key. We
            /// are really not indexing the copy now, but it will be automatically indexed when all previous copies are
            /// released from the index.
            /// </summary>
            public IndexedResource IndexCopy(Object resource)
            {
                IndexedResource last = this;
                while (last._next is not null)
                {
                    last = last._next;
                }

                IndexedResource instance = New(Key, resource, _indexer);
                instance._previous = last;
                last._next = instance;
                
                return instance;
            }
            
            public void Dispose()
            {
                // if previous is null it means we are indexed, else just unlink this instance
                if (_previous is null)
                {
                    RemoveFromUmaIndex();
                }
                else
                {
                    Unlink();
                }

                Release(this);
            }

            private void AddToUmaIndex()
            {
                _previous = null;
                _indexer.AddToUmaIndex(this);
            }

            private void RemoveFromUmaIndex()
            {
                _indexer.RemoveFromUmaIndex(this);
                _next?.AddToUmaIndex();
            }

            private void Unlink()
            {
                _previous._next = _next;
                if (_next is not null)
                {
                    _next._previous = _previous;
                }
            }

            // creates a new indexed resource instance from the pool
            private static IndexedResource New(string key, Object resource, GeniesAssetIndexer indexer)
            {
                _pool ??= new Stack<IndexedResource>();
                var instance = _pool.Count > 0 ? _pool.Pop() : new IndexedResource();
                instance.Key = key;
                instance.Resource = resource;
                instance._indexer = indexer;
                
                return instance;
            }

            // resets and puts the given instance back to the pool
            private static void Release(IndexedResource instance)
            {
                instance.Key = null;
                instance.Resource = null;
                instance._indexer = null;
                instance._previous = null;
                instance._next = null;
                _pool ??= new Stack<IndexedResource>();
                _pool.Push(instance);
            }
        }
    }
}