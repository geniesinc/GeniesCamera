using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// An assets provider implementation for a group of assets that are already loaded into the app.
    /// </summary>
    public class LoadedAssetsProvider<T> : IAssetsProvider<T>
    {
        private readonly Dictionary<object, T> _assets;
        private readonly IList<T> _readonlyAssets;
        private readonly IList<IResourceLocation> _locations;
        private readonly Dictionary<object, IResourceLocation> _locationsMap = new Dictionary<object, IResourceLocation>();
        
        public bool IsCached => true;

        public LoadedAssetsProvider(IDictionary<object, T> assets)
        {
            _assets = new Dictionary<object, T>(assets);
            
            // initialize fake resource locations and readonly assets
            var locations = new List<IResourceLocation>(_assets.Count);
            var assetsList = new List<T>(_assets.Count);
            
            foreach (var pair in _assets)
            {
                var location = new FakeResourceLocation(pair.Key, typeof(T));
                locations.Add(location);
                _locationsMap[pair.Key] = location;
                assetsList.Add(pair.Value);
            }
            
            _locations = locations.AsReadOnly();
            _readonlyAssets = assetsList.AsReadOnly();
        }

        public virtual UniTask<Ref<T>> LoadAssetAsync(object key)
        {
            if (!_assets.TryGetValue(key, out var asset))
            {
                return UniTask.FromResult<Ref<T>>(default);
            }

            var assetRef = CreateRef.FromAny(asset);
            return UniTask.FromResult(assetRef);
        }
        
        public UniTask<IResourceLocation> LoadResourceLocationAsync(object key)
        {
            if (!_locationsMap.TryGetValue(key, out var location))
            {
                return UniTask.FromResult<IResourceLocation>(null);
            }

            return UniTask.FromResult(location);
        }

        public UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync()
        {
            return UniTask.FromResult(_locations);
        }
        
        public UniTask<Ref<IList<T>>> LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(null);
        }
        
        public UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback)
        {
            if (callback != null)
            {
                foreach (T asset in _readonlyAssets)
                {
                    callback(asset);
                }
            }

            var listRef = CreateRef.FromAny(_readonlyAssets);
            return UniTask.FromResult(listRef);
        }
        
        public UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync()
        {
            return LoadAllUnpackedAssetsAsync(null);
        }
        
        public UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync(Action<T> callback)
        {
            var refs = new List<Ref<T>>(_readonlyAssets.Count);
            foreach (T asset in _readonlyAssets)
            {
                callback?.Invoke(asset);
                refs.Add(CreateRef.FromAny(asset));
            }
            
            IList<Ref<T>> result = refs.AsReadOnly();
            return UniTask.FromResult(result);
        }

        public UniTask CacheAllAssetsAsync()
        {
            return UniTask.CompletedTask;
        }
        
        public UniTask ReleaseCacheAsync()
        {
            return UniTask.CompletedTask;
        }
    }
    
    internal class FakeResourceLocation : IResourceLocation
    {
        private static readonly IList<IResourceLocation> FakeDependencies = new List<IResourceLocation>(0).AsReadOnly();
        
        public string InternalId => PrimaryKey;
        public string ProviderId => null;
        public IList<IResourceLocation> Dependencies => FakeDependencies;
        public int DependencyHashCode => 0;
        public bool HasDependencies => false;
        public string PrimaryKey => null;
        public object Data { get; }
        public Type ResourceType { get; }

        public FakeResourceLocation(object data, Type resourceType)
        {
            Data = data;
            ResourceType = resourceType;
        }
        
        public int Hash(Type resultType)
        {
            return (Data, resultType).GetHashCode();
        }
    }
}
