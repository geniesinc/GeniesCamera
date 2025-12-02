using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Genies.Assets.Services
{
    public sealed class BuiltinAssets : IBuiltinAssets
    {
        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();
        
        // state
        private readonly Dictionary<string, List<Asset>> _assetsByKey;
        private readonly Dictionary<(string, Type), IList<IResourceLocation>> _locationsCache;
        private readonly Dictionary<Object, IResourceLocation> _locationsByAsset;

        public BuiltinAssets(IEnumerable<Asset> assets = null)
        {
            _assetsByKey = new Dictionary<string, List<Asset>>();
            _locationsCache = new Dictionary<(string, Type), IList<IResourceLocation>>();
            _locationsByAsset = new Dictionary<Object, IResourceLocation>();

            SetAssets(assets);
        }

        public void SetAssets(IEnumerable<Asset> assets)
        {
            _assetsByKey.Clear();
            _locationsCache.Clear();
            _locationsByAsset.Clear();
            
            if (assets is null)
            {
                return;
            }

            foreach (Asset asset in assets)
            {
                RegisterAsset(asset);
            }
        }

        public bool TryGetAsset<T>(string key, out T asset)
        {
            if (!string.IsNullOrEmpty(key) && _assetsByKey.TryGetValue(key, out List<Asset> assets) && assets.First().asset is T tAsset)
            {
                asset = tAsset;
                return true;
            }
            
            asset = default;
            return false;
        }

        public bool TryGetAsset<T>(IResourceLocation location, out T asset)
        {
            if (location is Location && location.Data is T tAsset)
            {
                asset = tAsset;
                return true;
            }
            
            asset = default;
            return false;
        }

        public IList<IResourceLocation> GetResourceLocations(string key, Type type)
        {
            if (string.IsNullOrEmpty(key) || !_assetsByKey.TryGetValue(key, out List<Asset> assets))
            {
                return EmptyLocations;
            }

            var cacheKey = (key, type);
            if (_locationsCache.TryGetValue(cacheKey, out IList<IResourceLocation> locations))
            {
                return locations;
            }

            var locationsList = new List<IResourceLocation>(assets.Count);
            foreach (Asset asset in assets)
            {
                if ((type is null || type.IsInstanceOfType(asset.asset)) && _locationsByAsset.TryGetValue(asset.asset, out IResourceLocation location))
                {
                    locationsList.Add(location);
                }
            }
            
            _locationsCache[cacheKey] = locations = locationsList.AsReadOnly();
            return locations;
        }

        public IList<IResourceLocation> GetResourceLocations(IEnumerable<string> keys, MergingMode mergingMode, Type type)
        {
            if (keys is null)
            {
                return EmptyLocations;
            }

            if (mergingMode is MergingMode.None or MergingMode.UseFirst)
            {
                return GetResourceLocations(keys.FirstOrDefault(), type);
            }

            var locations = new HashSet<IResourceLocation>();

            if (mergingMode is MergingMode.Union)
            {
                foreach (string key in keys)
                {
                    locations.UnionWith(GetResourceLocations(key, type));
                }
            }
            else if (mergingMode is MergingMode.Intersection)
            {
                using IEnumerator<string> keysEnumerator = keys.GetEnumerator();
                
                // we have to union with the first key always, then we can intersect with the rest
                if (keysEnumerator.MoveNext())
                {
                    locations.UnionWith(GetResourceLocations(keysEnumerator.Current, type));
                }

                while (keysEnumerator.MoveNext())
                {
                    locations.IntersectWith(GetResourceLocations(keysEnumerator.Current, type));
                }
            }
            
            var locationsList = new List<IResourceLocation>(locations.Count);
            locationsList.AddRange(locations);
            
            return locationsList.AsReadOnly();
        }
        
        private void RegisterAsset(Asset asset)
        {
            if (!asset.asset)
            {
                return;
            }

            if (_locationsByAsset.ContainsKey(asset.asset))
            {
                Debug.LogError($"[{nameof(BuiltinAssets)}] found duplicated asset: {asset.asset} (key: {asset.key}. This key and labels will be ignored");
                return;
            }
            
            if (string.IsNullOrEmpty(asset.key))
            {
                Debug.LogError($"[{nameof(BuiltinAssets)}] found asset with null or empty key: {asset.asset}. The asset will not be added");
                return;
            }

            _locationsByAsset.Add(asset.asset, new Location(asset));
            GetOrCreateAssetListForKey(asset.key).Add(asset);

            if (asset.labels is null)
            {
                return;
            }

            foreach (string label in asset.labels)
            {
                GetOrCreateAssetListForKey(label).Add(asset);
            }
        }
        
        private List<Asset> GetOrCreateAssetListForKey(string key)
        {
            if (!_assetsByKey.TryGetValue(key, out List<Asset> assets))
            {
                _assetsByKey[key] = assets = new List<Asset>();
            }

            return assets;
        }

        [Serializable]
        public struct Asset
        {
            public string key;
            public Object asset;
            public List<string> labels;
        }

        private sealed class Location : IResourceLocation
        {
            public string InternalId => PrimaryKey;
            public string ProviderId => null;
            public IList<IResourceLocation> Dependencies => EmptyLocations;
            public int DependencyHashCode => 0;
            public bool HasDependencies => false;
            public string PrimaryKey { get; }
            public object Data { get; }
            public Type ResourceType { get; }

            public Location(Asset asset)
            {
                PrimaryKey = asset.key;
                ResourceType = asset.asset.GetType();
                Data = asset.asset;
            }
        
            public int Hash(Type resultType)
            {
                return Data.GetHashCode();
            }
        }
    }
}
