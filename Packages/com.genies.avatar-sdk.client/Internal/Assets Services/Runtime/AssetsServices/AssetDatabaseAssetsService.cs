#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Editor-only <see cref="IAssetsService"/> implementation that access assets in the project by path or GUID.
    /// </summary>
    public sealed class AssetDatabaseAssetsService : BaseAssetsService
    {
        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();

        public override UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
        {
            if (!typeof(Object).IsAssignableFrom(typeof(T)) || !IsKeyValid(key, out string assetKey))
                return UniTask.FromResult(default(Ref<T>));

            if (TryLoadAsset(assetKey, typeof(T), out Object asset) && asset is T tAsset)
                return UniTask.FromResult(CreateRef.FromAny(tAsset));

            return UniTask.FromResult(default(Ref<T>));
        }

        public override UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location)
        {
            return LoadAssetAsync<T>(location?.PrimaryKey);
        }

        public override UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type)
        {
            if (!IsKeyValid(key, out string assetKey))
                return UniTask.FromResult(EmptyLocations);

            if (!TryLoadAsset(assetKey, type, out Object asset))
                return UniTask.FromResult(EmptyLocations);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                return UniTask.FromResult(EmptyLocations);

            var locations = new List<IResourceLocation>(1)
            {
                new Location(guid, asset.GetType())
            };

            return UniTask.FromResult<IList<IResourceLocation>>(locations.AsReadOnly());
        }

        public override UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type)
        {
            // merging mode is ignored here since each key can only map to one asset
            var locations = new List<IResourceLocation>();

            foreach (object key in keys)
            {
                if (!IsKeyValid(key, out string assetKey))
                    continue;
                if (!TryLoadAsset(assetKey, type, out Object asset))
                    continue;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                    continue;

                locations.Add(new Location(guid, asset.GetType()));
            }

            return UniTask.FromResult<IList<IResourceLocation>>(locations.AsReadOnly());
        }

        private bool IsKeyValid(object key, out string path)
        {
            if (key is string stringKey && !string.IsNullOrEmpty(stringKey))
            {
                path = stringKey;
                return true;
            }

            Debug.LogError($"[{nameof(BuiltinAssets)}] invalid key type {key.GetType()}. Key must be a string key");
            path = null;
            return false;
        }

        private static bool TryLoadAsset(string assetKey, Type type, out Object asset)
        {
            type ??= typeof(Object);

            try
            {
                // try to load the asset as if the key is a path
                asset = AssetDatabase.LoadAssetAtPath(assetKey, type);
                if (asset)
                    return true;

                // if we failed to load the asset then see if the asset key is actually a GUID
                assetKey = AssetDatabase.GUIDToAssetPath(assetKey);
                if (string.IsNullOrEmpty(assetKey))
                    return false;

                asset = AssetDatabase.LoadAssetAtPath(assetKey, type);
                if (asset)
                    return true;

                return false;
            }
            catch (Exception)
            {
                asset = null;
                return false;
            }
        }

        private sealed class Location : IResourceLocation
        {
            public string InternalId => PrimaryKey;
            public string ProviderId => null;
            public IList<IResourceLocation> Dependencies => EmptyLocations;
            public int DependencyHashCode => 0;
            public bool HasDependencies => false;
            public string PrimaryKey { get; }
            public object Data => null;
            public Type ResourceType { get; }

            public Location(string path, Type resourceType)
            {
                PrimaryKey = path;
                ResourceType = resourceType;
            }

            public int Hash(Type resultType)
            {
                return (PrimaryKey, resultType).GetHashCode();
            }
        }
    }
}
#endif
