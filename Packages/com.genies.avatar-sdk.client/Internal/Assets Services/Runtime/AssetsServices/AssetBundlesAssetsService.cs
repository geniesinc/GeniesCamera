using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Genies.Assets.Services
{
    public static class AssetBundlesAssetsService
    {
        /// <summary>
        /// The only <see cref="IAssetsService"/> instance you can use to access the assets in the bundles contained
        /// in <see cref="AssetBundles"/>.
        /// </summary>
        public static readonly IAssetsService Service = new AssetsService();

        /// <summary>
        /// Add here any AssetBundles you need to be included for the <see cref="Service"/> instance.
        /// </summary>
        public static readonly HashSet<AssetBundle> AssetBundles = new();

        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();

        private static bool TryGetAssetNameAndBundle(object key, out string assetName, out AssetBundle bundle)
        {
            assetName = null;
            bundle = null;

            if (key is not string stringKey)
            {
                return false;
            }

            foreach (AssetBundle assetBundle in AssetBundles)
            {
                if (!assetBundle.Contains(stringKey))
                {
                    continue;
                }

                assetName = stringKey;
                bundle = assetBundle;
                return true;
            }

            return false;
        }

        private sealed class AssetsService : BaseAssetsService
        {
            public override async UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
            {
                if (!TryGetAssetNameAndBundle(key, out string assetName, out AssetBundle bundle))
                {
                    return default;
                }

                // try load the asset from the bundle
                Object asset;

                try
                {
                    asset = await bundle.LoadAssetAsync<T>(assetName);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[{nameof(AssetBundlesAssetsService)}] exception thrown while loading asset from AssetBundle:\nAsset Name: {assetName}\nBundle: {bundle.name}\n{exception}");
                    return default;
                }

                if (!asset || asset is not T tAsset)
                {
                    return default;
                }

                // create a dummy ref as we cannot destroy assets loaded directly from asset bundles (the asset bundle must be unloaded instead)
                // this means that this service is not very efficient, as any registered bundles must be always loaded in memory
                return CreateRef.FromAny(tAsset);
            }

            public override UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location)
            {
                return LoadAssetAsync<T>(location?.PrimaryKey);
            }

            public override async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type)
            {
                IResourceLocation location = await GetResourceLocationAsync(key, type);
                if (location is null)
                {
                    return EmptyLocations;
                }

                var locations = new List<IResourceLocation>(1) { location };
                return locations.AsReadOnly();
            }

            public override async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type)
            {
                // merging mode is ignored here since each key can only map to one asset
                var locations = new List<IResourceLocation>();

                foreach (object key in keys)
                {
                    IResourceLocation location = await GetResourceLocationAsync(key, type);
                    if (location is not null)
                    {
                        locations.Add(location);
                    }
                }

                return locations.AsReadOnly();
            }

            private static async UniTask<IResourceLocation> GetResourceLocationAsync(object key, Type type)
            {
                if (!TryGetAssetNameAndBundle(key, out string assetName, out AssetBundle bundle))
                {
                    return null;
                }

                Object asset;

                try
                {
                    asset = await bundle.LoadAssetAsync(assetName, type ?? typeof(Object));
                }
                catch (Exception)
                {
                    return null;
                }

                if (!asset)
                {
                    return null;
                }

                return new Location(assetName, type ?? asset.GetType());
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
