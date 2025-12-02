using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Refs
{
    /// <summary>
    /// Static utility to load assets from Resources using references.
    /// </summary>
    public static class ResourcesUtility
    {
        private static readonly HandleCache<(string, Type)> HandleCache = new();
        private static readonly Dictionary<(string, Type), UniTaskCompletionSource> LoadingOperations = new();
        
        public static Ref<T> LoadAsset<T>(string path)
        {
            // we avoid using a generic type constraint to UnityEngine.Object so we can use this method from an IAssetsService implementation
            Type assetType = typeof(T);
            var cacheKey = (path, assetType);
            
            // if the resource is already loaded then just create a new reference to it
            if (TryGetCachedAsset(cacheKey, out Ref<T> assetRef))
            {
                return assetRef;
            }

            try
            {
                // try to load the asset from the Resources API
                Object asset = Resources.Load(path, assetType);
                return ProcessLoadedAsset<T>(cacheKey, asset);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(ResourcesUtility)}] couldn't load {assetType} resource from: {path}\n{exception}");
                return default;
            }
        }

        public static async UniTask<Ref<T>> LoadAssetAsync<T>(string path)
        {
            // we avoid using a generic type constraint to UnityEngine.Object so we can use this method from an IAssetsService implementation
            Type assetType = typeof(T);
            var cacheKey = (path, assetType);
            
            // avoid executing multiple async loading operations for the same asset
            UniTaskCompletionSource loadingOperation;
            while(LoadingOperations.TryGetValue(cacheKey, out loadingOperation) && loadingOperation != null)
            {
                await loadingOperation.Task;
            }

            // if the resource is already loaded then just create a new reference to it
            if (TryGetCachedAsset(cacheKey, out Ref<T> assetRef))
            {
                return assetRef;
            }

            LoadingOperations[cacheKey] = loadingOperation = new UniTaskCompletionSource();

            try
            {
                ResourceRequest request = Resources.LoadAsync(path, assetType);
                await request;
                
                // after the async await we must check the cache again since a non async load call could have loaded the asset first
                if (TryGetCachedAsset(cacheKey, out assetRef))
                {
                    return assetRef;
                }

                // if the request was successful then process the asset and return a reference to it
                if (request.isDone)
                {
                    return ProcessLoadedAsset<T>(cacheKey, request.asset);
                }

                return default;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(ResourcesUtility)}] couldn't load {assetType} resource from: {path}\n{exception}");
                return default;
            }
            finally
            {
                LoadingOperations.Remove(cacheKey);
                loadingOperation.TrySetResult();
            }
        }

        private static bool TryGetCachedAsset<T>((string, Type) cacheKey, out Ref<T> assetRef)
        {
            if (HandleCache.TryGetNewReference(cacheKey, out Ref reference))
            {
                // this should never return false as we are never registering a non-matching type but just in case
                if (reference.TryCast(out assetRef))
                {
                    return true;
                }

                reference.Dispose();
            }
            
            assetRef = default;
            return false;
        }
        
        private static Ref<T> ProcessLoadedAsset<T>((string path, Type assetType) cacheKey, Object asset)
        {
            if (!asset)
            {
                Debug.LogError($"[{nameof(ResourcesUtility)}] couldn't load {cacheKey.assetType} resource from: {cacheKey.path}");
                return default;
            }

            // this should never happen as it doesn't make sense that the Resources.Load method would return a mismatching type, but just in case...
            if (asset is not T tAsset)
            {
                Debug.LogError($"[{nameof(ResourcesUtility)}] loaded resource from {cacheKey.path} but the asset is not of the expected type:\nType: {asset.GetType()}\nExpected: {cacheKey.assetType}");
                Resources.UnloadAsset(asset);
                return default;
            }
            
            // create a new ref to the asset, cache it and return it
            Ref<Object> unityObjectRef = CreateRef.FromUnityResource(asset);
            Ref<T> assetRef = CreateRef.FromDependentResource(tAsset, unityObjectRef);
            HandleCache.CacheHandle(cacheKey, assetRef);
            
            return assetRef;
        }
    }
}
