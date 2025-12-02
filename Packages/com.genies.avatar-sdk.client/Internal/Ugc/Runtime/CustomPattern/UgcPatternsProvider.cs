using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Ugc.CustomPattern
{
    /// <summary>
    /// An <see cref="IAssetsProvider{T}"/> implementation that adds custom pattern loading on top of a given patterns provider.
    /// </summary>
    public sealed class UgcPatternsProvider : IAssetsProvider<Texture2D>
    {
        public bool IsCached => _allAssetsRef.IsAlive;

        // dependencies
        private readonly ICustomPatternService _customPatternService;
        private readonly IAssetsProvider<Texture2D> _nonCustomPatternsProvider;

        // state
        private readonly Dictionary<string, CustomPatternLocation> _customPatternLocationsCache;
        private Ref _allAssetsRef;

        public UgcPatternsProvider(ICustomPatternService customPatternService, IAssetsProvider<Texture2D> nonCustomPatternsProvider)
        {
            _customPatternService = customPatternService;
            _nonCustomPatternsProvider = nonCustomPatternsProvider;

            _customPatternLocationsCache = new Dictionary<string, CustomPatternLocation>();
        }

        public async UniTask<Ref<Texture2D>> LoadAssetAsync(object key)
        {
            if (key is string patternId)
            {
                //return the custom patterns that belongs to me
                var isMyCustomPattern =  await _customPatternService.DoesCustomPatternExistAsync(patternId);
                if (isMyCustomPattern)
                {
                    return await _customPatternService.LoadCustomPatternTextureAsync(patternId);
                }

                var isCustomPatternFromOtherUser = await _customPatternService.DoesCustomPatternFromOtherUser(patternId);
                if (!string.IsNullOrEmpty(isCustomPatternFromOtherUser))
                {
                    return await _customPatternService.LoadCustomPatternTextureAsync(isCustomPatternFromOtherUser, patternId);
                }
            }


            return await _nonCustomPatternsProvider.LoadAssetAsync(key);
        }

        public async UniTask<IResourceLocation> LoadResourceLocationAsync(object key)
        {
            if (key is string patternId && await _customPatternService.DoesCustomPatternExistAsync(patternId))
            {
                return GetOrCreateCustomPatternLocation(patternId);
            }

            return await _nonCustomPatternsProvider.LoadResourceLocationAsync(key);
        }

        public async UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync()
        {
            IList<IResourceLocation> nonCustomLocations = await _nonCustomPatternsProvider.LoadAllResourceLocationsAsync();
            List<string> customPatternIds = await _customPatternService.GetAllCustomPatternIdsAsync();

            var locations = new List<IResourceLocation>(nonCustomLocations.Count + customPatternIds.Count);
            locations.AddRange(nonCustomLocations);
            locations.AddRange(customPatternIds.Select(GetOrCreateCustomPatternLocation));

            return locations.AsReadOnly();
        }

        public UniTask<Ref<IList<Texture2D>>> LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(delegate { });
        }

        public async UniTask<Ref<IList<Texture2D>>> LoadAllAssetsAsync(Action<Texture2D> callback)
        {
            // fetch data from non custom patterns provider and custom pattern service
            Ref<IList<Texture2D>> nonCustomPatternsRef = await _nonCustomPatternsProvider.LoadAllAssetsAsync(callback);
            List<string> customPatternIds = await _customPatternService.GetAllCustomPatternIdsAsync();
            int totalCount = nonCustomPatternsRef.IsAlive ? nonCustomPatternsRef.Item.Count + customPatternIds.Count : customPatternIds.Count;

            // instantiate the array that will have all textures and the one that will have all dependency refs
            var patterns = new List<Texture2D>(totalCount);
            var dependencies = new List<Ref>(customPatternIds.Count + 1);

            // if non custom patterns were loaded, add them
            if (nonCustomPatternsRef.IsAlive)
            {
                patterns.AddRange(nonCustomPatternsRef.Item);
                dependencies.Add(nonCustomPatternsRef);
            }

            // load and add custom patterns
            async UniTask LoadAndAddCustomPatternAsync(string patternId)
            {
                Ref<Texture2D> patternRef = await _customPatternService.LoadCustomPatternTextureAsync(patternId);
                if (!patternRef.IsAlive)
                {
                    return;
                }

                patterns.Add(patternRef.Item);
                dependencies.Add(patternRef);
                callback?.Invoke(patternRef.Item);
            }

            await UniTask.WhenAll(customPatternIds.Select(LoadAndAddCustomPatternAsync));

            // generate the final ref encapsulating all the refs and return it
            IList<Texture2D> readOnlyPatterns = patterns.AsReadOnly();
            Ref<IList<Texture2D>> patternsRef = CreateRef.FromDependentResource(readOnlyPatterns, dependencies);

            return patternsRef;
        }

        public UniTask<IList<Ref<Texture2D>>> LoadAllUnpackedAssetsAsync()
        {
            return LoadAllUnpackedAssetsAsync(delegate { });
        }

        public async UniTask<IList<Ref<Texture2D>>> LoadAllUnpackedAssetsAsync(Action<Texture2D> callback)
        {
            // fetch data from non custom patterns provider and custom pattern service
            IList<Ref<Texture2D>> nonCustomPatterns = await _nonCustomPatternsProvider.LoadAllUnpackedAssetsAsync(callback);
            List<string> customPatternIds = await _customPatternService.GetAllCustomPatternIdsAsync();
            int totalCount = nonCustomPatterns?.Count ?? 0 + customPatternIds.Count;

            // instantiate the array that will have all texture refs
            var patterns = new List<Ref<Texture2D>>(totalCount);

            // if non custom patterns were loaded, add them
            if (nonCustomPatterns != null)
            {
                patterns.AddRange(nonCustomPatterns);
            }

            // load and add custom patterns
            async UniTask LoadAndAddCustomPatternAsync(string patternId)
            {
                Ref<Texture2D> patternRef = await _customPatternService.LoadCustomPatternTextureAsync(patternId);
                if (!patternRef.IsAlive)
                {
                    return;
                }

                patterns.Add(patternRef);
                callback?.Invoke(patternRef.Item);
            }

            await UniTask.WhenAll(customPatternIds.Select(LoadAndAddCustomPatternAsync));

            return patterns.AsReadOnly();
        }

        public async UniTask CacheAllAssetsAsync()
        {
            if (IsCached)
            {
                return;
            }

            _allAssetsRef = await LoadAllAssetsAsync();
        }

        public UniTask ReleaseCacheAsync()
        {
            _allAssetsRef.Dispose();
            return UniTask.CompletedTask;
        }

        private CustomPatternLocation GetOrCreateCustomPatternLocation(string patternId)
        {
            if (!_customPatternLocationsCache.TryGetValue(patternId, out CustomPatternLocation location))
            {
                _customPatternLocationsCache[patternId] = location = new CustomPatternLocation(patternId);
            }

            return location;
        }

        private sealed class CustomPatternLocation : IResourceLocation
        {
            private static readonly IList<IResourceLocation> FakeDependencies = new List<IResourceLocation>().AsReadOnly();

            public string InternalId => PrimaryKey;
            public string ProviderId => null;
            public IList<IResourceLocation> Dependencies => FakeDependencies;
            public int DependencyHashCode => 0;
            public bool HasDependencies => false;
            public string PrimaryKey { get; }
            public object Data => null;
            public Type ResourceType => typeof(Texture2D);

            public CustomPatternLocation(string primaryKey)
            {
                PrimaryKey = primaryKey;
            }

            public int Hash(Type resultType)
            {
                return PrimaryKey.GetHashCode();
            }
        }
    }
}
