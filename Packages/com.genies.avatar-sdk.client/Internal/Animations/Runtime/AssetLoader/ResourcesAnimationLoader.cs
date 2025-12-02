using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Animations
{
    /// <summary>
    /// Implementation of the IAnimationLoader that loads AnimationClips from Unity's Resources folder.
    /// This loader provides synchronous-style loading wrapped in async tasks for animation clips stored in Resources.
    /// </summary>
    public class ResourcesAnimationLoader : IAnimationLoader
    {
        private const string AnimationLibraryPath = "Animations";

        /// <inheritdoc cref='IAnimationLoader.LoadAnimationClips'/>
        public async UniTask<Ref<List<AnimationClip>>> LoadAnimationClips(List<string> assetAddresses)
        {
            var animationClips = new List<AnimationClip>();

            var tasks = new List<UniTask<ResourceRequest>>();
            for (int i = 0; i < assetAddresses.Count; i++)
            {
                tasks.Add(new UniTask<ResourceRequest>(Resources.LoadAsync<AnimationClip>($"{AnimationLibraryPath}/{assetAddresses[i]}")));
            }

            var loadOperations = await UniTask.WhenAll(tasks);
            for (int i = 0; i < loadOperations.Length; i++)
            {
                animationClips.Add(loadOperations[i].asset as AnimationClip);
            }

            var result = CreateRef.FromAny(animationClips);
            return result;
        }

        /// <inheritdoc cref='IAnimationLoader.LoadAnimationClip'/>
        public async UniTask<Ref<AnimationClip>> LoadAnimationClip(string assetAddress)
        {
            var task = new UniTask<ResourceRequest>(Resources.LoadAsync<AnimationClip>($"{AnimationLibraryPath}/{assetAddress}"));
            ResourceRequest loadOperation = await task;
            var animationClip = loadOperation.asset as AnimationClip;
            Ref<AnimationClip> result = CreateRef.FromAny(animationClip);
            return result;
        }
    }
}
