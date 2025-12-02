using System;
using System.Collections.Generic;
using Genies.Refs;
using UnityEngine;

namespace Genies.Animations.Model
{
    /// <summary>
    /// Data structure that contains an array of strings representing asset addresses of animation clips
    /// and the actual animation clips, both ordered by their montage number.
    /// This is organized by animation type (genie, dolls, camera) to support different animation targets.
    /// </summary>
    public class AnimationMontage : IDisposable
    {
        /// <summary>
        /// List of asset addresses for genie (avatar) animation clips, ordered by montage sequence.
        /// </summary>
        public List<string> GenieAssetAddresses = new List<string>();
        
        /// <summary>
        /// List of asset addresses for doll animation clips, ordered by montage sequence.
        /// </summary>
        public List<string> DollAssetAddresses = new List<string>();
        
        /// <summary>
        /// List of asset addresses for camera animation clips, ordered by montage sequence.
        /// </summary>
        public List<string> CameraAssetAddresses = new List<string>();
        
        /// <summary>
        /// Reference to the loaded genie (avatar) animation clips corresponding to the GenieAssetAddresses.
        /// </summary>
        public Ref<List<AnimationClip>> GenieAnimations;
        
        /// <summary>
        /// Reference to the loaded doll animation clips corresponding to the DollAssetAddresses.
        /// </summary>
        public Ref<List<AnimationClip>> DollAnimations;
        
        /// <summary>
        /// Reference to the loaded camera animation clips corresponding to the CameraAssetAddresses.
        /// </summary>
        public Ref<List<AnimationClip>> CameraAnimations;

        /// <summary>
        /// Disposes of all animation clip references to free up memory and resources.
        /// This method should be called when the animation montage is no longer needed.
        /// </summary>
        public void Dispose()
        {
            GenieAnimations.Dispose();
            DollAnimations.Dispose();
            CameraAnimations.Dispose();
        }
    }
}
