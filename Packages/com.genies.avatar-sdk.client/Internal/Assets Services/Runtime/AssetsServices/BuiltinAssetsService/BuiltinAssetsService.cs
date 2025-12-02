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
    /// <see cref="IAssetsService"/> implementation that access builtin assets (already loaded) from a given <see cref="IBuiltinAssets"/> instance.
    /// Since the assets are supposed to be always loaded (builtin), all the load operations are synchronous and returned references will not destroy
    /// the assets on disposal.
    /// </summary>
    public sealed class BuiltinAssetsService : BaseAssetsService
    {
        private static readonly IList<IResourceLocation> EmptyLocations = new List<IResourceLocation>(0).AsReadOnly();

        private readonly IBuiltinAssets _assets;

        public BuiltinAssetsService(IBuiltinAssets assets)
        {
            _assets = assets;
        }

        public override UniTask<Ref<T>> LoadAssetAsync<T>(object key, int? version = null, string lod = AssetLod.Default)
        {
            Ref<T> assetRef = default;
            if (IsKeyValid(key, out string assetKey) && _assets.TryGetAsset(assetKey, out T asset))
            {
                assetRef = CreateRef.FromAny(asset);
            }

            return UniTask.FromResult(assetRef);
        }

        public override UniTask<Ref<T>> LoadAssetAsync<T>(IResourceLocation location)
        {
            Ref<T> assetRef = default;
            if (_assets.TryGetAsset(location, out T asset))
            {
                assetRef = CreateRef.FromAny(asset);
            }

            return UniTask.FromResult(assetRef);
        }

        public override UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, Type type)
        {
            if (!IsKeyValid(key, out string assetKey))
            {
                return UniTask.FromResult(EmptyLocations);
            }

            IList<IResourceLocation> locations = _assets.GetResourceLocations(assetKey, type);
            return UniTask.FromResult(locations);
        }

        public override UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(IEnumerable keys, MergingMode mergingMode, Type type)
        {
            IList<IResourceLocation> locations;

            if (keys is IEnumerable<string> assetKeys)
            {
                locations = _assets.GetResourceLocations(assetKeys, mergingMode, type);
            }
            else
            {
                locations = _assets.GetResourceLocations(GetAssetKeys(keys), mergingMode, type);
            }

            return UniTask.FromResult(locations);
        }

        private bool IsKeyValid(object key, out string path)
        {
            if (key is string stringKey)
            {
                path = stringKey;
                return true;
            }

            Debug.LogError($"[{nameof(BuiltinAssets)}] invalid key type {key.GetType()}. Key must be a string key");
            path = null;
            return false;
        }

        private static IEnumerable<string> GetAssetKeys(IEnumerable keys)
        {
            foreach (object key in keys)
            {
                if (key is string assetKey)
                {
                    yield return assetKey;
                }
            }
        }
    }
}
