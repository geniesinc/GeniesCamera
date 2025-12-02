using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Components.ShaderlessTools;
using Genies.Refs;
using Genies.ServiceManagement;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Genies.Assets.Services
{
    public sealed class AddressableAssetsService : BaseAssetsService
    {
        private IShaderlessAssetService _shaderlessAssetService;
        private IShaderlessAssetService ShaderlessAssetService => 
            _shaderlessAssetService ?? (_shaderlessAssetService = ServiceManager.Get<IShaderlessAssetService>() ?? new ShaderlessAssetService(this));

        public override async UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
        {
#if UNITY_EDITOR
            await RequestLoadOperationAsync();
#endif

            AsyncOperationHandle<T> handle;

            try
            {
                handle = await GeniesAddressables.LoadAssetVariantAsync<T>(key, version, lod);
                await handle;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load addressable asset with key: {key} version:{version} lod:{lod}\n{exception}");
                return default;
            }

            if (handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load addressable asset with key: {key} version:{version} lod:{lod}\n{handle.OperationException}");
                return default;
            }

            return await ResolveShaderlessMaterials(handle);
        }

        public override async UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location)
        {
            AsyncOperationHandle<T> handle = default;

            try
            {
                handle = GeniesAddressables.LoadAssetAsync<T>(location);
                await handle;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load addressable asset with location: {location}\n{exception}");
                return default;
            }

            if (handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load addressable asset with location: {location}\n{handle.OperationException}");
                return default;
            }

            return await ResolveShaderlessMaterials(handle);
        }

        public override async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle = default;

            try
            {
                handle = GeniesAddressables.LoadResourceLocationsAsync(key, type);
                await handle;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load resource locations: Key: {key}; Type: {type}\n{exception}");
                return null;
            }

            if (handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                Debug.LogWarning($"Failed to load resource locations: Key: {key}; Type: {type}\n{handle.OperationException}");
                return null;
            }

            IList<IResourceLocation> locations = handle.Result;
            UnityAddressables.Release(handle);
            return locations;
        }

        public override async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle = default;

            try
            {
                handle = GeniesAddressables.LoadResourceLocationsAsync(keys, (UnityAddressables.MergeMode)mergingMode, type);
                await handle;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load resource locations: Type: {type}\n{exception}");
                return null;
            }

            if (handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load resource locations: Type: {type}\n{handle.OperationException}");
                return null;
            }

            IList<IResourceLocation> locations = handle.Result;
            UnityAddressables.Release(handle);
            return locations;
        }

        public override async UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            Exception exception = null;
            AsyncOperationHandle<IList<T>> handle = default;

            try
            {
                handle = GeniesAddressables.LoadAssetsAsync(locations, callback, releaseDependenciesOnFailure);
                await handle;
            }
            catch (Exception e)
            {
                exception = e;
            }

            exception = handle.OperationException ?? exception;

            if (exception != null || handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                if (releaseDependenciesOnFailure)
                {
                    Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load addressable assets from locations list: Type: {typeof(T)}\n{exception}");
                    return default;
                }

                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load some or all addressable assets from locations list but releaseDependenciesOnFailure was set to false: Type: {typeof(T)}\n{exception}");
            }

            return await ResolveShaderlessMaterials(handle);
        }

        public override async UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, MergingMode mergingMode, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            Exception exception = null;
            AsyncOperationHandle<IList<T>> handle = default;

            try
            {
                handle = GeniesAddressables.LoadAssetsAsync(keys, callback, (UnityAddressables.MergeMode)mergingMode, releaseDependenciesOnFailure);
                await handle;
            }
            catch (Exception e)
            {
                exception = e;
            }

            exception = handle.OperationException ?? exception;

            if (exception != null || handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                if (releaseDependenciesOnFailure)
                {
                    Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load addressable assets from keys collection: Type: {typeof(T)}\n{exception}");
                    return default;
                }

                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load some or all addressable assets from keys collection but releaseDependenciesOnFailure was set to false: Type: {typeof(T)}\n{exception}");
            }

            return CreateRef.FromAddressable(handle);
        }

        public override async UniTask<Ref<IList<T>>> LoadAssetsAsync<T>(object key, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            Exception exception = null;
            AsyncOperationHandle<IList<T>> handle = default;

            try
            {
                handle = GeniesAddressables.LoadAssetsAsync(key, callback, releaseDependenciesOnFailure);
                await handle;
            }
            catch (Exception e)
            {
                exception = e;
            }

            exception = handle.OperationException ?? exception;

            if (exception != null || handle.Status == AsyncOperationStatus.Failed || !handle.IsValid())
            {
                if (releaseDependenciesOnFailure)
                {
                    Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load matching addressable assets. Key: {key}; Type: {typeof(T)}\n{exception}");
                    return default;
                }

                Debug.LogWarning($"[{nameof(AddressableAssetsService)}] Failed to load some or all matching addressable assets but releaseDependenciesOnFailure was set to false. Key: {key}; Type: {typeof(T)}\n{exception}");
            }

            return CreateRef.FromAddressable(handle);
        }

        private async UniTask<Ref<T>> ResolveShaderlessMaterials<T>(AsyncOperationHandle<T> handle)
        {
            Ref<T> assetRef = CreateRef.FromAddressable(handle);

            return await ShaderlessAssetService.LoadShadersAsync(assetRef);
        }

#if UNITY_EDITOR
        private const ushort MaxLoadOperationsPerFrame = 10; // I can reproduce the crash with 18 in my machine, so 10 this is conservative
        private static ushort _currentFrameAcceptedLoadOperations = 0;
        private static ushort _currentFramePendingLoadOperations = 0;

        /**
         * Awaiting this method before performing an Addressables load operation will limit the amount of operations
         * performed on each frame.
         *
         * This fixes a silent crash that only happens in the editor. This crash is very hard to replicate and debug
         * since it leaves no logs, and it is caused by the Addressables package. The crash does not happen on player
         * builds. This has been happening for months and I came to this solution after at least an entire day of
         * debugging and trying almost everything.
         *
         * If the crash happens again you will see an operation count report on the editor log files as the very last
         * lines of the log. You can adjust the MaxLoadOperationsPerFrame to avoid crashes.
         */
        private static async UniTask RequestLoadOperationAsync()
        {
            while (_currentFrameAcceptedLoadOperations >= MaxLoadOperationsPerFrame)
            {
                ProcessRequest().Forget(); // we have to process the request on each frame we try
                await UniTask.Yield();
            }

            ProcessRequest().Forget();
            return;

            async UniTaskVoid ProcessRequest()
            {
                // if max load operations per frame is exceeded then leave this request as pending
                if (_currentFrameAcceptedLoadOperations >= MaxLoadOperationsPerFrame)
                {
                    ++_currentFramePendingLoadOperations;
                    return;
                }

                // register this as an accepted request and only proceed to next frame reset if it is the first one
                if (_currentFrameAcceptedLoadOperations++ > 0)
                    return;

                // await for the start of the next frame
                await UniTask.Yield(PlayerLoopTiming.Initialization);

                // log load operation report for this frame. We use Console.WriteLine so we don't generate noise in the
                // editor console. This log will appear in the editor log files, so we can check in case of a crash
                Console.WriteLine($"[{nameof(AddressableAssetsService)}] asset load operations report for frame {Time.frameCount}:");
                Console.WriteLine($"    Accepted load operations: {_currentFrameAcceptedLoadOperations}");
                Console.WriteLine($"    Pending load operations:  {_currentFramePendingLoadOperations}\n");

                // reset load operations count
                _currentFramePendingLoadOperations = 0;
                _currentFrameAcceptedLoadOperations = 0;
            }
        }
#endif
    }
}
