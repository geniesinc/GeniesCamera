using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Defines the contract for rendering individual UGC elements with support for styling, animations, and region-based operations.
    /// This interface provides methods for applying visual styles, managing animations, and controlling rendering behavior
    /// of UGC elements within the avatar system.
    /// </summary>
    public interface IElementRender : IDisposable
    {
        /// <summary>
        /// Gets the root GameObject that represents this element render in the scene hierarchy.
        /// </summary>
        GameObject Root { get; }
        
        /// <summary>
        /// Gets the bounding box that encompasses the entire rendered element.
        /// </summary>
        Bounds Bounds { get; }
        
        /// <summary>
        /// Gets or sets whether to use default colors for rendering instead of custom styling.
        /// </summary>
        bool UseDefaultColors { get; set; }
        
        /// <summary>
        /// Gets or sets whether region debugging is enabled for visual inspection of element regions.
        /// </summary>
        bool RegionDebugging { get; set; }
        
        /// <summary>
        /// Applies a complete split configuration to the element, including all regions and styles.
        /// </summary>
        /// <param name="split">The split configuration containing styling data for all regions.</param>
        /// <returns>A task that completes when the split has been applied to the element.</returns>
        UniTask ApplySplitAsync(Split split);
        
        /// <summary>
        /// Applies styling to multiple regions of the element simultaneously.
        /// </summary>
        /// <param name="regions">The collection of regions with their styling configurations.</param>
        /// <returns>A task that completes when all regions have been styled.</returns>
        UniTask ApplyRegionsAsync(IEnumerable<Region> regions);
        
        /// <summary>
        /// Applies styling to a single region of the element.
        /// </summary>
        /// <param name="region">The region configuration containing styling data.</param>
        /// <returns>A task that completes when the region styling has been applied.</returns>
        UniTask ApplyRegionAsync(Region region);
        
        /// <summary>
        /// Applies a specific style to a particular region of the element.
        /// </summary>
        /// <param name="style">The style configuration to apply.</param>
        /// <param name="regionIndex">The zero-based index of the region to style.</param>
        /// <returns>A task that completes when the style has been applied to the specified region.</returns>
        UniTask ApplyStyleAsync(Style style, int regionIndex);
        
        /// <summary>
        /// Calculates the bounding box of the element when rotated by the specified quaternion.
        /// </summary>
        /// <param name="rotation">The rotation to apply when calculating bounds.</param>
        /// <returns>The axis-aligned bounding box after rotation.</returns>
        Bounds GetAlignedBounds(Quaternion rotation);
        
        /// <summary>
        /// Starts playing an animation on all regions of the element.
        /// </summary>
        /// <param name="animation">The animation configuration to play.</param>
        void PlayAnimation(ValueAnimation animation);
        
        /// <summary>
        /// Stops all currently playing animations on the element.
        /// </summary>
        void StopAnimation();
        
        /// <summary>
        /// Starts playing an animation on a specific region of the element.
        /// </summary>
        /// <param name="regionIndex">The zero-based index of the region to animate.</param>
        /// <param name="animation">The animation configuration to play.</param>
        /// <param name="playAlone">If true, stops animations on all other regions before playing this one.</param>
        void PlayRegionAnimation(int regionIndex, ValueAnimation animation, bool playAlone = false);
        
        /// <summary>
        /// Stops animation playback on a specific region of the element.
        /// </summary>
        /// <param name="regionIndex">The zero-based index of the region to stop animating.</param>
        void StopRegionAnimation(int regionIndex);
    }
}
