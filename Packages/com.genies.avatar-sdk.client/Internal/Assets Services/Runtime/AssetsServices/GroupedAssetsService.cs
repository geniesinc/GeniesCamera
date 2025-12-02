using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Merges the results from multiple <see cref="IAssetsService"/> instances together. It may not always work
    /// as expected. This should be mainly used in the editor to test content without the need for building Addressables.
    /// </summary>
    public sealed class GroupedAssetsService : BaseAssetsService
    {
        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();

        private IAssetsService[] _services;

        public GroupedAssetsService(IEnumerable<IAssetsService> services = null)
        {
            SetServices(services);
        }

        public void SetServices(IEnumerable<IAssetsService> services)
        {
            if (services is null)
            {
                _services = Array.Empty<IAssetsService>();
                return;
            }

            _services = services.ToArray();
        }

        public override async UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
        {
            if (_services.Length == 0)
            {
                return default;
            }

            Ref<T>[] assetRefs = await UniTask.WhenAll(_services.Select(service => service.LoadAssetAsync<T>(key, version)));

            // return the first alive reference and dispose the rest
            Ref<T> result = default;
            foreach (Ref<T> assetRef in assetRefs)
            {
                if (result.IsAlive)
                {
                    assetRef.Dispose();
                }
                else
                {
                    result = assetRef;
                }
            }

            return result;
        }

        public override async UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location)
        {
            if (_services.Length == 0)
            {
                return default;
            }

            Ref<T>[] assetRefs = await UniTask.WhenAll(_services.Select(service => service.LoadAssetAsync<T>(location)));

            // return the first alive reference and dispose the rest
            Ref<T> result = default;
            foreach (Ref<T> assetRef in assetRefs)
            {
                if (result.IsAlive)
                {
                    assetRef.Dispose();
                }
                else
                {
                    result = assetRef;
                }
            }

            return result;
        }

        public override async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type)
        {
            if (_services.Length == 0)
            {
                return EmptyLocations;
            }

            IList<IResourceLocation>[] results = await UniTask.WhenAll(_services.Select(service => service.LoadResourceLocationsAsync(key, type)));

            var locations = new List<IResourceLocation>();
            foreach (IList<IResourceLocation> result in results)
            {
                if (result is not null)
                {
                    locations.AddRange(result);
                }
            }

            return locations.AsReadOnly();
        }

        public override async UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type)
        {
            if (_services.Length == 0)
            {
                return EmptyLocations;
            }

            IList<IResourceLocation>[] results = await UniTask.WhenAll(_services.Select(service => service.LoadResourceLocationsAsync(keys, mergingMode, type)));

            var locations = new List<IResourceLocation>();
            foreach (IList<IResourceLocation> result in results)
            {
                if (result is not null)
                {
                    locations.AddRange(result);
                }
            }

            return locations.AsReadOnly();
        }
    }
}
