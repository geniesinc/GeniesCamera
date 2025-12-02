using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Singleton service that the app should use to load any external assets into memory.
    /// </summary>
    public interface IAssetsService
    {
        // load a single asset with its key/location
        UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default);
        UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location);

        // load resource locations from keys (non generic)
        UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key);
        UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type);
        UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode);
        UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type);

        // load resource locations from keys (generic)
        UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync<T>(object key);
        UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync<T>(IEnumerable keys, MergingMode mergingMode);

        #region PACKED_LOAD
        // load multiple assets with from a list of locations
        // the reason why we don't use IEnumerable<IResourceLocation> instead of IList is because Addressables only support the IList call
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, bool releaseDependenciesOnFailure);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback, bool releaseDependenciesOnFailure);

        // load multiple assets mathing a collection of keys with an specific merge mode
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, bool releaseDependenciesOnFailure);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback, bool releaseDependenciesOnFailure);

        // load multiple assets mathing a single key
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, Action<T> callback);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, bool releaseDependenciesOnFailure);
        UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, Action<T> callback, bool releaseDependenciesOnFailure);
        #endregion

        #region UNPACKED_LOAD
        // load multiple unpacked assets with from a list of locations
        UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IList<IResourceLocation> locations);
        UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback);

        // load multiple unpacked assets mathing a collection of keys with an specific merge mode
        UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode);
        UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback);

        // load multiple unpacked assets mathing a single key
        UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(object key);
        UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(object key, Action<T> callback);
        #endregion
    }
}
