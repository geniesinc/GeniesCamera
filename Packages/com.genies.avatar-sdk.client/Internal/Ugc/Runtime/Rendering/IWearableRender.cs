using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Defines the contract for rendering complete wearable items that may contain multiple elements.
    /// This interface provides methods for managing wearable rendering, element visibility, animations,
    /// and styling operations across all elements within a wearable.
    /// </summary>
    public interface IWearableRender : IDisposable
    {
        /// <summary>
        /// Gets the root GameObject that represents this wearable render in the scene hierarchy.
        /// </summary>
        GameObject Root { get; }
        
        /// <summary>
        /// Gets the bounding box that encompasses the entire rendered wearable including all its elements.
        /// </summary>
        Bounds Bounds { get; }
        
        /// <summary>
        /// Gets or sets whether region debugging is enabled for visual inspection of wearable regions.
        /// </summary>
        bool RegionDebugging { get; set; }
        
        /// <summary>
        /// Applies a complete wearable configuration including all elements and their styling.
        /// </summary>
        /// <param name="wearable">The wearable configuration containing all element and styling data.</param>
        /// <returns>A task that completes when the wearable has been fully applied and rendered.</returns>
        UniTask ApplyWearableAsync(Wearable wearable);
        
        /// <summary>
        /// Calculates the bounding box of the wearable when rotated by the specified quaternion.
        /// </summary>
        /// <param name="rotation">The rotation to apply when calculating bounds.</param>
        /// <returns>The axis-aligned bounding box after rotation.</returns>
        Bounds GetAlignedBounds(Quaternion rotation);
        
        /// <summary>
        /// Sets whether a specific element should be solo rendered (hiding all other elements).
        /// </summary>
        /// <param name="elementId">The unique identifier of the element.</param>
        /// <param name="soloRendered">True to enable solo rendering for this element; false to disable.</param>
        void SetElementIdSoloRendered(string elementId, bool soloRendered);
        
        /// <summary>
        /// Sets whether multiple elements should be solo rendered (hiding all other elements).
        /// </summary>
        /// <param name="elementIds">The collection of element identifiers.</param>
        /// <param name="soloRendered">True to enable solo rendering for these elements; false to disable.</param>
        void SetElementIdsSoloRendered(IEnumerable<string> elementIds, bool soloRendered);
        
        /// <summary>
        /// Clears all solo rendering states, making all elements visible again.
        /// </summary>
        void ClearAllSoloRenders();
        
        /// <summary>
        /// Starts playing an animation on all regions of a specific element within the wearable.
        /// </summary>
        /// <param name="elementId">The unique identifier of the element to animate.</param>
        /// <param name="animation">The animation configuration to play.</param>
        void PlayAnimation(string elementId, ValueAnimation animation);
        
        /// <summary>
        /// Stops all animations on a specific element within the wearable.
        /// </summary>
        /// <param name="elementId">The unique identifier of the element to stop animating.</param>
        void StopAnimation(string elementId);
        
        /// <summary>
        /// Starts playing an animation on a specific region of an element within the wearable.
        /// </summary>
        /// <param name="elementId">The unique identifier of the element.</param>
        /// <param name="regionIndex">The zero-based index of the region to animate.</param>
        /// <param name="animation">The animation configuration to play.</param>
        /// <param name="playAlone">If true, stops animations on all other regions before playing this one.</param>
        void PlayRegionAnimation(string elementId, int regionIndex, ValueAnimation animation, bool playAlone = false);
        
        /// <summary>
        /// Stops animation playback on a specific region of an element within the wearable.
        /// </summary>
        /// <param name="elementId">The unique identifier of the element.</param>
        /// <param name="regionIndex">The zero-based index of the region to stop animating.</param>
        void StopRegionAnimation(string elementId, int regionIndex);
        
        /// <summary>
        /// Stops all currently playing animations across all elements in the wearable.
        /// </summary>
        void StopAllAnimations();
    }
}
