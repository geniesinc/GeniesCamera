using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// <see cref="IAssetsService"/> implementation that loads from the Unity's Resources API.
    /// </summary>
    public sealed class ResourceAssetsService : BaseAssetsService
    {
        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();

        public override UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
        {
            if (IsKeyInvalid(key, out string path))
            {
                return UniTask.FromResult<Ref<T>>(default);
            }

            return ResourcesUtility.LoadAssetAsync<T>(path);
        }

        public override UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location)
        {
            return ResourcesUtility.LoadAssetAsync<T>(location.PrimaryKey);
        }

        public override UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type)
        {
            if (IsKeyInvalid(key, out string path))
            {
                return UniTask.FromResult(EmptyLocations);
            }

            var locations = new List<IResourceLocation>(1)
            {
                new Location(path, type)
            };

            return UniTask.FromResult<IList<IResourceLocation>>(locations.AsReadOnly());
        }

        public override UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type)
        {
            var locations = new List<IResourceLocation>();
            foreach (object key in keys)
            {
                if (key is string path)
                {
                    locations.Add(new Location(path, type));
                }
            }

            return UniTask.FromResult<IList<IResourceLocation>>(locations.AsReadOnly());
        }

        private bool IsKeyInvalid(object key, out string path)
        {
            if (key is string stringKey)
            {
                path = stringKey;
                return false;
            }

            Debug.LogError($"[{nameof(ResourceAssetsService)}] invalid key type {key.GetType()}. Key must be a string path to the resource");
            path = null;
            return true;
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
