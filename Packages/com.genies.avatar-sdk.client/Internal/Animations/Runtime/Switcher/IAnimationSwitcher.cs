using System.Collections.Generic;
using System.Threading;
using Genies.Animations.Model;
using Genies.Refs;
using UnityEngine;

namespace Genies.Animations
{
    /// <summary>
    /// Defines the contract for switching between animation clips in a montage sequence.
    /// Implementations of this interface manage the playback of animation montages with timing control.
    /// </summary>
    public interface IAnimationSwitcher
    {
        /// <summary>
        /// Gets the total duration of the animation montage in seconds.
        /// </summary>
        float MontageTime { get; }
        
        /// <summary>
        /// Gets the frame rate of the animation clips in the montage.
        /// </summary>
        float FrameRate { get; }
        
        /// <summary>
        /// Initializes the IAnimationSwitcher implementation.
        /// </summary>
        /// <param name="clips">List of Animation Clips Ref</param>
        void Init(Ref<AnimationMontage> clips);

        /// <summary>
        /// Play the list of animations in sequence.
        /// </summary>
        void Play();

        /// <summary>
        /// Stops the looping of animation clips.
        /// </summary>
        void Stop();

        /// <summary>
        /// Plays the sequence of animations from the beginning.
        /// </summary>
        void Reset();
    }
}
