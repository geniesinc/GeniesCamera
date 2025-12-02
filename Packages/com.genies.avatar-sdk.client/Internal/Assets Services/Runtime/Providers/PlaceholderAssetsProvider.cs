using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// A no-op assets provider.
    /// </summary>
    public class PlaceholderAssetsProvider<T> : IAssetsProvider<T>
    {
        public static PlaceholderAssetsProvider<T> Instance => _instance ??= new PlaceholderAssetsProvider<T>();
        
        private static PlaceholderAssetsProvider<T> _instance;
        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();
        private static readonly IList<Ref<T>> EmptyRefs = new List<Ref<T>>(0).AsReadOnly();
        
        public bool IsCached => false;
        
        public UniTask<Ref<T>> LoadAssetAsync(object key)
            => UniTask.FromResult<Ref<T>>(default);

        public UniTask<IResourceLocation> LoadResourceLocationAsync(object key)
            => UniTask.FromResult<IResourceLocation>(null);

        public UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync()
            => UniTask.FromResult(EmptyLocations);

        public UniTask<Ref<IList<T>>> LoadAllAssetsAsync()
            => UniTask.FromResult<Ref<IList<T>>>(default);

        public UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback)
            => UniTask.FromResult<Ref<IList<T>>>(default);

        public UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync()
            => UniTask.FromResult(EmptyRefs);

        public UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync(Action<T> callback)
            => UniTask.FromResult(EmptyRefs);

        public UniTask CacheAllAssetsAsync()
            => UniTask.CompletedTask;

        public UniTask ReleaseCacheAsync()
            => UniTask.CompletedTask;
    }
}
