using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Abstract base class for asset loaders that provides common functionality for loading assets
    /// from containers through the assets service.
    /// </summary>
    /// <typeparam name="T">The type of asset to load (e.g., Texture2D, AudioClip, etc.)</typeparam>
    /// <typeparam name="TContainer">The type of container that holds the asset data (e.g. ScriptableObject, etc.)</typeparam>
    /// <remarks>
    /// This class implements a two-step loading process:
    /// 1. Load the container using the assets service
    /// 2. Extract the specific asset from the container using the abstract FromContainer method
    /// 
    /// Subclasses must implement the FromContainer method to define how to extract the asset
    /// from the loaded container.
    /// </remarks>
    public abstract class BaseAssetLoader<T, TContainer> : IAssetLoader<T>
    {
        protected readonly IAssetsService _assetsService;
        
        public BaseAssetLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }
        
        public virtual async UniTask<Ref<T>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return default;
            }

            Ref<TContainer> container  = await _assetsService.LoadAssetAsync<TContainer>(assetId);

            if (!container.IsAlive || container.Item == null)
            {
                return default;
            }

            var asset = await FromContainer(assetId, lod, container.Item);
            return CreateRef.FromDependentResource(asset, container);
        }
        
        protected abstract UniTask<T> FromContainer(string assetId, string lod, TContainer container);
        
    }
}
