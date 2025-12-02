using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;

namespace Genies.Animations
{
    /// <summary>
    /// Implementation of the IAnimationLoader that loads AnimationClips from Unity's Addressables system.
    /// This loader provides asynchronous loading of animation clips with proper reference management and error handling.
    /// </summary>
    public class AddressablesAnimationLoader : IAnimationLoader
    {
        private readonly IAssetsService _service;

        /// <summary>
        /// Initializes a new instance of the AddressablesAnimationLoader class.
        /// </summary>
        /// <param name="service">The assets service used for loading resources from the Addressables system.</param>
        public AddressablesAnimationLoader(IAssetsService service)
        {
            _service = service;
        }

        /// <inheritdoc cref='IAnimationLoader.LoadAnimationClips'/>
        public async UniTask<Ref<List<AnimationClip>>> LoadAnimationClips(List<string> assetAddresses)
        {
            var result = new List<AnimationClip>();

            var tasks = new List<UniTask<Ref<AnimationClip>>>();
            for (int i = 0; i < assetAddresses.Count; i++)
            {
                tasks.Add(_service.LoadAssetAsync<AnimationClip>(assetAddresses[i]));
            }

            var animationClips = await UniTask.WhenAll(tasks);

            for (int i = 0; i < animationClips.Length; i++)
            {
                if(animationClips[i] == default(Ref) || animationClips[i].Item == null)
                {
                    Debug.LogWarning("[AnimationLoader] Removed an empty animation ref that failed to load.");
                    animationClips[i].Dispose();
                    continue;
                }
                result.Add(animationClips[i].Item);
            }

            return CreateRef.FromDependentResource(result, animationClips);
        }

        /// <inheritdoc cref='IAnimationLoader.LoadAnimationClip'/>
        public async UniTask<Ref<AnimationClip>> LoadAnimationClip(string assetAddress)
        {
            UniTask<Ref<AnimationClip>> task = _service.LoadAssetAsync<AnimationClip>(assetAddress);
            Ref<AnimationClip> animationClipRef = await task;

            if (animationClipRef == default(Ref) || animationClipRef.Item == null)
            {
                Debug.LogWarning("[AnimationLoader] Removed an empty animation ref that failed to load.");
                animationClipRef.Dispose();
                return default;
            }

            return animationClipRef;
        }
    }
}
