using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Assets provider for that can be initalised with a collection of resource locations.
    /// </summary>
    public class AssetsProviderByLocations<T> : IAssetsProvider<T>
    {
        public bool IsCached => Cache.IsAlive;
        
        public readonly IAssetsService AssetsService;

        protected readonly Dictionary<object, IResourceLocation> LocationsMap;
        protected readonly IList<IResourceLocation> Locations;
        protected Ref<IList<T>> Cache;

        public AssetsProviderByLocations(IAssetsService assetsService, IEnumerable<IResourceLocation> locations)
        {
            AssetsService = assetsService;
            Locations = locations.ToList().AsReadOnly();
            LocationsMap = new Dictionary<object, IResourceLocation>();

            foreach (IResourceLocation location in Locations)
            {
                LocationsMap[location.PrimaryKey] = location;
            }
        }
        
        public virtual async UniTask<Ref<T>> LoadAssetAsync(object key)
        {
            IResourceLocation location = await LoadResourceLocationAsync(key);

            if (location is null)
            {
                return default;
            }

            return await AssetsService.LoadAssetAsync<T>(location);
        }

        public virtual UniTask<IResourceLocation> LoadResourceLocationAsync(object key)
        {
            if (LocationsMap.TryGetValue(key, out IResourceLocation location))
            {
                return UniTask.FromResult(location);
            }

            Debug.LogWarning($"Could not find the resource location for key {key} in {this}");
            return UniTask.FromResult<IResourceLocation>(null);
        }

        public UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync()
            => UniTask.FromResult(Locations);

        public virtual UniTask<Ref<IList<T>>> LoadAllAssetsAsync()
            => AssetsService.LoadAssetsAsync<T>(Locations);
        
        public virtual UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback)
            => AssetsService.LoadAssetsAsync<T>(Locations, callback);
        
        public virtual UniTask<Ref<IList<T>>> LoadAllAssetsAsync(bool releaseDependenciesOnFailure)
            => AssetsService.LoadAssetsAsync<T>(Locations, releaseDependenciesOnFailure);
        
        public virtual UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback, bool releaseDependenciesOnFailure)
            => AssetsService.LoadAssetsAsync<T>(Locations, callback, releaseDependenciesOnFailure);
        
        public UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync()
            => AssetsService.LoadUnpackedAssetsAsync<T>(Locations);
        
        public UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync(Action<T> callback)
            => AssetsService.LoadUnpackedAssetsAsync<T>(Locations, callback);
        
        public virtual async UniTask CacheAllAssetsAsync()
        {
            Ref<IList<T>> newCache = await LoadAllAssetsAsync();
            Cache.Dispose();
            Cache = newCache;
        }
        
        public virtual UniTask ReleaseCacheAsync()
        {
            Cache.Dispose();
            return UniTask.CompletedTask;
        }
    }
}
