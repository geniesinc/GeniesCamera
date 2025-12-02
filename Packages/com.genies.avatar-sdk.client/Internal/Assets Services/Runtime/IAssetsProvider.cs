using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// An assets provider differs from an assets service in that the provider has a fixed type and refers to a specific collection of assets
    /// that could be also loaded from the assets service.
    /// </br>
    /// The assets provider has direct methods to load all the assets that it represents and also some caching utilities.
    /// </summary>
    public interface IAssetsProvider<T>
    {
        bool IsCached { get; }

        UniTask<Ref<T>> LoadAssetAsync(object key);
        UniTask<IResourceLocation> LoadResourceLocationAsync(object key);

        UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync();
        
        UniTask<Ref<IList<T>>> LoadAllAssetsAsync();
        UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback);

        UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync();
        UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync(Action<T> callback);

        UniTask CacheAllAssetsAsync();
        UniTask ReleaseCacheAsync();
    }
}
