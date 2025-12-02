using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Animations
{
    /// <summary>
    /// Asset loader specialized for loading animation containers and their associated thumbnails.
    /// This loader extends BaseAssetLoader to provide animation container-specific functionality including thumbnail caching.
    /// </summary>
    public class AnimationContainerLoader : BaseAssetLoader<AnimationContainer, AnimationContainer>
    {
        private Dictionary<string, IResourceLocation> _thumbnailLocationCache;

        /// <summary>
        /// Initializes a new instance of the AnimationContainerLoader class.
        /// </summary>
        /// <param name="assetsService">The assets service used for loading resources from the asset system.</param>
        public AnimationContainerLoader(IAssetsService assetsService) : base(assetsService)
        {
            _thumbnailLocationCache = new Dictionary<string, IResourceLocation>();
        }

        /// <summary>
        /// Creates an AnimationContainer instance from the loaded container data.
        /// This method simply returns the container as-is since no transformation is needed.
        /// </summary>
        /// <param name="assetId">The asset identifier.</param>
        /// <param name="lod">The level of detail (unused for animation containers).</param>
        /// <param name="container">The loaded animation container.</param>
        /// <returns>A task that completes with the animation container.</returns>
        protected override UniTask<AnimationContainer> FromContainer(string assetId, string lod, AnimationContainer container)
        {
            return UniTask.FromResult(container);
        }

        /// <summary>
        /// Loads a thumbnail texture for the specified animation asset.
        /// Uses caching to improve performance for repeated thumbnail requests.
        /// </summary>
        /// <param name="assetId">The asset identifier for which to load the thumbnail.</param>
        /// <returns>A task that completes with a reference to the thumbnail texture, or default if not found.</returns>
        // TODO: Reviewers, should this be moved to a metadata provider?
        public async UniTask<Ref<Texture2D>> LoadThumbnailAsync(string assetId)
        {
            Ref<Texture2D> result;
            if (_thumbnailLocationCache.ContainsKey(assetId))
            {
                result = await _assetsService.LoadAssetAsync<Texture2D>(_thumbnailLocationCache[assetId]);
                return result;
            }
            
            // Sprites can be loaded by added the suffix _x1024
            var locations = await _assetsService.LoadResourceLocationsAsync<Texture2D>($"{assetId}_x1024");

            if (locations.Count == 0)
            {
                return default;
            }

            var location  = locations[0];
            _thumbnailLocationCache.Add(assetId, location);

            result = await _assetsService.LoadAssetAsync<Texture2D>(location);
            return result;
        }
    }
}