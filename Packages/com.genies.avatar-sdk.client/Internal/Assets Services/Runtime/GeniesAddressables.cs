using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Wraps part of the <see cref="Addressables"/> API with some convenient extra functionality like disabled error
    /// logging and internal ID interpolated versioning.
    /// </summary>
    public static class GeniesAddressables
    {
        private const string _patternFileVersion = "_v\\d+.";
        private const string _fileVersionReplace = "_v{0}.";
        private const string _patternFolderVersion = "/v\\d+/";
        private const string _folderVersionReplace = "/v{0}/";
        private static readonly ConcurrentDictionary<object, int?> _interpolatedPrimaryKeyVersionDict;

        static GeniesAddressables()
        {
            _interpolatedPrimaryKeyVersionDict = new ConcurrentDictionary<object, int?>();

            AddressableTransformFuncUtility.RegisterTransformer(TryTransformFunc);
        }

        /// <summary>
        /// Wrapper static method to load addressables with interpolated versioning/lods. The returned handle must be awaited.
        /// </summary>
        /// <param name="key">Addressable Key</param>
        /// <param name="version">Version of the addressable to request (null gets latest)</param>
        /// <param name="lod">The level of detail for the asset</param>
        public static async UniTask<AsyncOperationHandle<T>> LoadAssetVariantAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
        {
            key = await ResolveLodAssetAddress<T>(key, lod);

            if (version is null)
            {
                return LoadAssetAsync<T>(key);
            }

            // try to get the resource locations
            IList<IResourceLocation> locations;
            try
            {
                AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = LoadResourceLocationsAsync(key, typeof(T));
                await locationsHandle;

                if (locationsHandle.Status == AsyncOperationStatus.Failed || !locationsHandle.IsValid())
                {
                    return LoadAssetAsync<T>(key);
                }

                locations = locationsHandle.Result;
                UnityAddressables.Release(locationsHandle);
            }
            catch (Exception)
            {
                return LoadAssetAsync<T>(key);
            }

            // locations were fetched successfully
            _interpolatedPrimaryKeyVersionDict.TryAdd(InterpolatedDictKey(key, typeof(T)), version);
            foreach (IResourceLocation resultLocation in locations)
            {
                foreach (IResourceLocation depLocation in resultLocation.Dependencies)
                {
                    _interpolatedPrimaryKeyVersionDict.TryAdd(InterpolatedDictKey(depLocation.PrimaryKey, depLocation.ResourceType), version);
                }
            }

            // try to get interpolated version addressable
            AsyncOperationHandle<T> handle = LoadAssetAsync<T>(key);
            handle.Completed += _ => _interpolatedPrimaryKeyVersionDict.TryRemove(InterpolatedDictKey(key, typeof(T)), out int? _);

            return handle;
        }

        public static AsyncOperationHandle<T> LoadAssetAsync<T>(object key)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadAssetAsync<T>(key);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        public static AsyncOperationHandle<T> LoadAssetAsync<T>(IResourceLocation location)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadAssetAsync<T>(location);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        public static AsyncOperationHandle<IList<T>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadAssetsAsync<T>(locations, callback, releaseDependenciesOnFailure);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        public static AsyncOperationHandle<IList<T>> LoadAssetsAsync<T>(IEnumerable keys, Action<T> callback, UnityAddressables.MergeMode mode, bool releaseDependenciesOnFailure)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadAssetsAsync<T>(keys, callback, mode, releaseDependenciesOnFailure);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        public static AsyncOperationHandle<IList<T>> LoadAssetsAsync<T>(object key, Action<T> callback, bool releaseDependenciesOnFailure)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadAssetsAsync<T>(key, callback, releaseDependenciesOnFailure);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type = null)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadResourceLocationsAsync(key, type);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, UnityAddressables.MergeMode mode, Type type = null)
        {
            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;

            try
            {
                Debug.unityLogger.logEnabled = false;
                return UnityAddressables.LoadResourceLocationsAsync(keys, mode, type);
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }
        }

        private static async UniTask<object> ResolveLodAssetAddress<T>(object key, string lod)
        {
            if (string.IsNullOrEmpty(lod))
            {
                return key;
            }

            bool wasLoggingEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;

            try
            {
                var lodAddress = AssetLod.InterpolatedAddress(key, lod);
                AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = LoadResourceLocationsAsync(lodAddress, typeof(T));
                await locationsHandle;

                if (locationsHandle.Status == AsyncOperationStatus.Failed || !locationsHandle.IsValid())
                {
                    return key;
                }

                if (locationsHandle.Result.Count > 0)
                {
                    return lodAddress;
                }
            }
            finally
            {
                Debug.unityLogger.logEnabled = wasLoggingEnabled;
            }

            return key;
        }

        /// <summary>
        /// Transform unity function to intercept the addressable 'InternalId'
        /// Uses interpolation to change the internal ID to have the version replaced
        /// See Genies.Components.Addressables.Editor.Utilities.CatalogUtility.TransformInternalIdToInterpolatedVersion()
        /// </summary>
        /// <param name="location"></param>
        /// <param name="transformedId"></param>
        private static bool TryTransformFunc(IResourceLocation location, out string transformedId)
        {
            transformedId = location.InternalId;
            var interpolatedDictKey = InterpolatedDictKey(location.PrimaryKey, location.ResourceType);

            if (!_interpolatedPrimaryKeyVersionDict.TryGetValue(interpolatedDictKey, out var version) || version == null)
            {
                return false;
            }

            transformedId = Regex.Replace(transformedId, _patternFileVersion, string.Format(_fileVersionReplace, version));
            transformedId = Regex.Replace(transformedId, _patternFolderVersion, string.Format(_folderVersionReplace, version));

            _interpolatedPrimaryKeyVersionDict.TryRemove(interpolatedDictKey, out var versionRemove);

            return true;
        }

        private static string InterpolatedDictKey(object key, object type) => $"{key}_{type}";
    }
}
