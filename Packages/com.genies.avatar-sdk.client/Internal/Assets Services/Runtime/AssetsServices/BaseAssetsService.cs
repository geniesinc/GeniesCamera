using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Provides some overridable generic implementations for most of <see cref="IAssetsService"/> so implementers can just implement
    /// the core loading methods.
    /// </summary>
    public abstract class BaseAssetsService : IAssetsService
    {
        public abstract UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default);
        public abstract UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location);
        public abstract UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type);
        public abstract UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type);

        public virtual async UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            if (locations is null)
            {
                return default;
            }

            var assets = new List<T>(locations.Count);
            var assetRefs = new List<Ref<T>>(locations.Count);

            async UniTask LoadLocationAsync(IResourceLocation location)
            {
                Ref<T> assetRef = await LoadAssetAsync<T>(location);
                if (!assetRef.IsAlive)
                {
                    return;
                }

                assets.Add(assetRef.Item);
                assetRefs.Add(assetRef);
                callback?.Invoke(assetRef.Item);
            }

            await UniTask.WhenAll(locations.Select(LoadLocationAsync));

            IList<T> result = assets.AsReadOnly();
            return CreateRef.FromDependentResource(result, assetRefs);
        }

        public virtual async UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            IList<IResourceLocation> locations = await LoadResourceLocationsAsync<T>(keys, mergingMode);
            return await LoadAssetsAsync(locations, callback, releaseDependenciesOnFailure);
        }

        public virtual async UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            IList<IResourceLocation> locations = await LoadResourceLocationsAsync<T>(key);
            return await LoadAssetsAsync(locations, callback, releaseDependenciesOnFailure);
        }

        public virtual async UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback)
        {
            async UniTask<Ref<T>> LoadWithCallbackAsync(IResourceLocation location, Action<T> callback)
            {
                Ref<T> assetRef = await LoadAssetAsync<T>(location);

                if (!assetRef.IsAlive)
                {
                    return default;
                }

                callback(assetRef.Item);
                return assetRef;
            }

            // prepare loading tasks for each location
            var tasks = new UniTask<Ref<T>>[locations.Count];

            if (callback is null)
            {
                for (int i = 0; i < tasks.Length; ++i)
                {
                    tasks[i] = LoadAssetAsync<T>(locations[i]);
                }
            }
            else
            {
                for (int i = 0; i < tasks.Length; ++i)
                {
                    tasks[i] = LoadWithCallbackAsync(locations[i], callback);
                }
            }

            // execute all loading tasks in parallel and return an IList result
            Ref<T>[] references = await UniTask.WhenAll(tasks);
            var result = new List<Ref<T>>(references.Length);
            result.AddRange(references);

            return result;
        }

        public virtual async UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback)
        {
            IList<IResourceLocation> locations = await LoadResourceLocationsAsync<T>(keys, mergingMode);
            return await LoadUnpackedAssetsAsync<T>(locations, callback);
        }

        public virtual async UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(object key, Action<T> callback)
        {
            IList<IResourceLocation> locations = await LoadResourceLocationsAsync<T>(key);
            return await LoadUnpackedAssetsAsync<T>(locations, callback);
        }

#region OVERLOADS
        public virtual UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key)
            => LoadResourceLocationsAsync(key, null);

        public virtual UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode)
            => LoadResourceLocationsAsync(keys, mergingMode, null);

        public virtual UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync<T>(object key)
            => LoadResourceLocationsAsync(key, typeof(T));

        public virtual UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync<T>(IEnumerable keys, MergingMode mergingMode)
            => LoadResourceLocationsAsync(keys, mergingMode, typeof(T));

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations)
            => LoadAssetsAsync<T>(locations, null, true);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback)
            => LoadAssetsAsync<T>(locations, callback, true);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, bool releaseDependenciesOnFailure)
            => LoadAssetsAsync<T>(locations, null, releaseDependenciesOnFailure);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode)
            => LoadAssetsAsync<T>(keys, mergingMode, null, true);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback)
            => LoadAssetsAsync<T>(keys, mergingMode, callback, true);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, bool releaseDependenciesOnFailure)
            => LoadAssetsAsync<T>(keys, mergingMode, null, releaseDependenciesOnFailure);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key)
            => LoadAssetsAsync<T>(key, null, true);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, Action<T> callback)
            => LoadAssetsAsync<T>(key, callback, true);

        public virtual UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, bool releaseDependenciesOnFailure)
            => LoadAssetsAsync<T>(key, null, releaseDependenciesOnFailure);

        public virtual UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IList<IResourceLocation> locations)
            => LoadUnpackedAssetsAsync<T>(locations, null);

        public virtual UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode)
            => LoadUnpackedAssetsAsync<T>(keys, mergingMode, null);

        public virtual UniTask<IList<Ref<T>>> LoadUnpackedAssetsAsync<T>(object key)
            => LoadUnpackedAssetsAsync<T>(key, null);
#endregion
    }
}
