using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Assets provider for every asset matching with the labels and merging provided in the initialization. It needs to fetch all the resource locations first
    /// to work properly. If ReloadResourceLocationsAsync is not called manually, it will be done on the first call to any of its public methods.
    /// </summary>
    public class LabeledAssetsProvider<T> : IAssetsProvider<T>
    {
        public bool IsCached => Cache.IsAlive;
        
        public readonly IAssetsService AssetsService;
        public readonly object[] Labels;
        public readonly MergingMode MergingMode;

        protected readonly Dictionary<object, IResourceLocation> LocationsMap;
        protected readonly string LabelsString;
        protected IList<IResourceLocation> Locations;
        protected Ref<IList<T>> Cache;

        public LabeledAssetsProvider(IAssetsService assetsService, IEnumerable labels, MergingMode mergingMode)
        {
            LocationsMap = new Dictionary<object, IResourceLocation>();
                
            // convert labels to array (also create a string representation for debugging purposes)
            var labelsBuilder = new StringBuilder();
            labelsBuilder.Append("{ ");
            var list = new List<object>();

            foreach (object key in labels)
            {
                labelsBuilder.Append($"{key}, ");
                list.Add(key);
            }
            
            labelsBuilder.Append(" }");
            LabelsString = labelsBuilder.ToString();
            
            // initialise fields
            AssetsService = assetsService;
            Labels = list.ToArray();
            MergingMode = mergingMode;
        }
        
        public virtual async UniTask<Ref<T>> LoadAssetAsync(object key)
        {
            await WaitForReloadAsync();
            IResourceLocation location = await LoadResourceLocationAsync(key);

            if (location is null)
            {
                return default;
            }

            return await AssetsService.LoadAssetAsync<T>(location);
        }

        public virtual async UniTask<IResourceLocation> LoadResourceLocationAsync(object key)
        {
            await WaitForReloadAsync();
            
            if (LocationsMap.TryGetValue(key, out IResourceLocation location))
            {
                return location;
            }

            Debug.LogWarning($"Could not find the resource location for key {key} in {this}");
            return null;
        }

        public async UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync()
        {
            await WaitForReloadAsync();
            return Locations;
        }
        
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
        
        #region RELOAD
        protected UniTaskCompletionSource ReloadCompletionSource;
        protected CancellationTokenSource ReloadCancellationSource;
        
        /// <summary>
        /// Reloads all the resource locations for the configured labels. This is called automatically on instance creation but you may
        /// want to call it manually if updating the source providers for the assets service.
        /// </summary>
        public virtual async UniTask ReloadResourceLocationsAsync()
        {
            // cancel previous reload operations (if any)
            ReloadCancellationSource?.Cancel();
            ReloadCompletionSource?.TrySetCanceled();
            
            // start a new cancellable reload operation
            ReloadCompletionSource = new UniTaskCompletionSource();
            ReloadCancellationSource = new CancellationTokenSource();
            CancellationToken cancellationToken = ReloadCancellationSource.Token;
            
            // load all the locations matching the labels from the assets service
            IList<IResourceLocation> locations = await AssetsService.LoadResourceLocationsAsync<T>(Labels, MergingMode);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // reset the locations map
            Locations = locations;
            LocationsMap.Clear();

            foreach (IResourceLocation location in Locations)
            {
                LocationsMap[location.PrimaryKey] = location;
            }

            ReloadCompletionSource.TrySetResult();
            ReloadCancellationSource = null;
        }
        #endregion
        
        #region LOAD_ALL
        public virtual async UniTask<Ref<IList<T>>> LoadAllAssetsAsync()
        {
            await WaitForReloadAsync();
            return await AssetsService.LoadAssetsAsync<T>(Locations);
        }
        
        public virtual async UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback)
        {
            await WaitForReloadAsync();
            return await AssetsService.LoadAssetsAsync<T>(Locations, callback);
        }
        
        public virtual async UniTask<Ref<IList<T>>> LoadAllAssetsAsync(bool releaseDependenciesOnFailure)
        {
            await WaitForReloadAsync();
            return await AssetsService.LoadAssetsAsync<T>(Locations, releaseDependenciesOnFailure);
        }
        
        public virtual async UniTask<Ref<IList<T>>> LoadAllAssetsAsync(Action<T> callback, bool releaseDependenciesOnFailure)
        {
            await WaitForReloadAsync();
            return await AssetsService.LoadAssetsAsync<T>(Locations, callback, releaseDependenciesOnFailure);
        }
        #endregion
        
        #region UNPACKED_LOAD_ALL
        public async UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync()
        {
            await WaitForReloadAsync();
            return await AssetsService.LoadUnpackedAssetsAsync<T>(Locations);
        }
        
        public async UniTask<IList<Ref<T>>> LoadAllUnpackedAssetsAsync(Action<T> callback)
        {
            await WaitForReloadAsync();
            return await AssetsService.LoadUnpackedAssetsAsync<T>(Locations, callback);
        }
        #endregion

        public override string ToString()
        {
            return $"{nameof(LabeledAssetsProvider<T>)} with labels: {LabelsString}";
        }
        
        // helper method to reload all locations if not done yet and wait for any pending reload operations
        protected async UniTask WaitForReloadAsync()
        {
            // if ReloadCompletionSource is null, it means that we never loaded any resource locations
            if (ReloadCompletionSource is null)
            {
                ReloadResourceLocationsAsync().Forget();
            }

            while (await TryWaitingForReloadAsync())
            {
                ;
            }
        }

        private async UniTask<bool> TryWaitingForReloadAsync()
        {
            // the reload operation only gets cancelled when calling it multiple times, so the ReloadCompletionSource instance will represent the very last call
            try
            {
                await ReloadCompletionSource.Task;
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }
    }
}
