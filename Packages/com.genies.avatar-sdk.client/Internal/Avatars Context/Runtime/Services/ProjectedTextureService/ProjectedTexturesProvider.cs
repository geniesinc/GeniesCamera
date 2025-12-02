using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// An <see cref="IAssetsLoader{T}"/> that just wraps <see cref="ProjectedTextureRemoteLoaderService"/>.
    /// </summary>
    public sealed class ProjectedTexturesProvider : IAssetsProvider<Texture2D>
    {
        public bool IsCached => _allAssetsRef.IsAlive;

        // dependencies
        public IProjectedTextureService ProjectedTextureService { get; private set; }

        // state
        private Ref _allAssetsRef;

        public ProjectedTexturesProvider(IProjectedTextureService projectedTextureService)
        {
            ProjectedTextureService = projectedTextureService;
        }

        public async UniTask<Ref<Texture2D>> LoadAssetAsync(object key)
        {
            if (key is ProjectedTexture projtex)
            {
                return await ProjectedTextureService.LoadProjectedTextureAsync(projtex);
            }

            return default;
        }


        public UniTask<Ref<IList<Texture2D>>> LoadAllAssetsAsync()
        {
            return LoadAllAssetsAsync(delegate { });
        }

        public UniTask<Ref<IList<Texture2D>>> LoadAllAssetsAsync(Action<Texture2D> callback)
        {
            throw new NotImplementedException();
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

        public UniTask<Ref<Texture2D>> LoadAsync(string assetId)
        {
            throw new NotImplementedException();
        }

        public UniTask<IResourceLocation> LoadResourceLocationAsync(object key)
        {
            throw new NotImplementedException();
        }

        public UniTask<IList<IResourceLocation>> LoadAllResourceLocationsAsync()
        {
            throw new NotImplementedException();
        }

        public UniTask<IList<Ref<Texture2D>>> LoadAllUnpackedAssetsAsync()
        {
            throw new NotImplementedException();
        }

        public UniTask<IList<Ref<Texture2D>>> LoadAllUnpackedAssetsAsync(Action<Texture2D> callback)
        {
            throw new NotImplementedException();
        }
    }
}
