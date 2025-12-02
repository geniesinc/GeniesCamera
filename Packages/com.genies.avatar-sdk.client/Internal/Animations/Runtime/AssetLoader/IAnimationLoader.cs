using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Animations
{
    /// <summary>
    /// Defines the contract for loading animation clips from various asset sources.
    /// Implementations of this interface provide different loading strategies such as Addressables or Resources.
    /// </summary>
    public interface IAnimationLoader
    {
        /// <summary>
        /// Loads an array of animation clips from the specified asset addresses.
        /// </summary>
        /// <param name="assetAddresses">List of asset addresses or paths to the animation clips.</param>
        /// <returns>A task that completes with a reference to a list of loaded animation clips.</returns>
        public UniTask<Ref<List<AnimationClip>>> LoadAnimationClips(List<string> assetAddresses);
        
        /// <summary>
        /// Loads a single animation clip from the specified asset address.
        /// </summary>
        /// <param name="assetAddress">Asset address or path to the animation clip.</param>
        /// <returns>A task that completes with a reference to the loaded animation clip.</returns>
        public UniTask<Ref<AnimationClip>> LoadAnimationClip(string assetAddress);
    }
}
