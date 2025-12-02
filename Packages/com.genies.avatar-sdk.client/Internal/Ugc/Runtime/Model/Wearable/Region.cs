using Newtonsoft.Json;
using Genies.Utilities;

/*
 *   If changes are made to this file, please advance the JsonVersion number, and
 *   make the corresponding changes to the WearableDefinitionSchema.json
 */

namespace Genies.Ugc
{
    /// <summary>
    /// Represents a specific styleable area within a split element.
    /// Each region can be styled independently with its own colors, patterns, and material properties.
    /// Regions are numbered to identify different areas of a mesh that can receive different styling.
    /// </summary>
    public class Region : IModel<Region>
    {
        /// <summary>
        /// The numerical identifier for this region within its parent split.
        /// Region numbers are used to map styling information to specific areas of a 3D mesh.
        /// </summary>
        [JsonProperty("RegionNumber")]
        public int RegionNumber;
        
        /// <summary>
        /// The style configuration applied to this region, including colors, patterns, and material settings.
        /// If null, the region will use default styling or inherit from parent configurations.
        /// </summary>
        [JsonProperty("Style")]
        public Style Style;

        /// <summary>
        /// Determines whether this region is equivalent to another region by comparing RegionNumber and Style.
        /// Two regions are considered equivalent if they have the same region number and equivalent styles.
        /// </summary>
        /// <param name="other">The other region to compare against.</param>
        /// <returns>True if the regions are equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(Region other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || GetType() != other.GetType())
            {
                return false;
            }

            return
                RegionNumber == other.RegionNumber &&
                (Style?.IsEquivalentTo(other.Style) ?? other.Style is null);
        }
        
        /// <summary>
        /// Computes a hash code for this region based on its RegionNumber and Style.
        /// The hash ensures that equivalent regions produce the same hash code.
        /// </summary>
        /// <returns>A hash code representing the current state of this region.</returns>
        public int ComputeHash()
        {
            return HashingUtils.GetCombinedHashCode(RegionNumber, Style?.ComputeHash());
        }
        
        /// <summary>
        /// Creates a deep copy of this region, including its style and all nested properties.
        /// The returned region is completely independent of the original and can be modified
        /// without affecting the source region.
        /// </summary>
        /// <returns>A new Region instance that is a deep copy of this region.</returns>
        public Region DeepCopy()
        {
            return new Region()
            {
                RegionNumber = RegionNumber,
                Style = Style?.DeepCopy()
            };
        }
        
        /// <summary>
        /// Performs a deep copy of this region's properties into the specified destination region.
        /// This method updates the destination region's properties to match this region,
        /// including deep copying of the style and nested objects.
        /// </summary>
        /// <param name="destination">The target region to copy properties into. If null, no operation is performed.</param>
        public void DeepCopy(Region destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.RegionNumber = RegionNumber;
            Style.DeepCopy(ref destination.Style);
        }

        bool IModel.IsEquivalentTo(object other)
            => other is Region region && IsEquivalentTo(region);

        object ICopyable.DeepCopy()
            => DeepCopy();

        void ICopyable.DeepCopy(object destination)
            => DeepCopy(destination as Region);
    }
}
