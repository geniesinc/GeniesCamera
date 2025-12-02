using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Genies.Utilities;

/*
 *   If changes are made to this file, please advance the JsonVersion number, and
 *   make the corresponding changes to the WearableDefinitionSchema.json
 */

namespace Genies.Ugc
{
    /// <summary>
    /// Represents an individual element (split) within a wearable item.
    /// A split contains a reference to a 3D mesh element and defines how it should be styled
    /// through regions, projected textures, and material settings. Each split can have
    /// multiple regions that can be styled independently with colors, patterns, and materials.
    /// </summary>
    public class Split : IModel<Split>
    {
        /// <summary>
        /// The version of the material/shader system this split is compatible with.
        /// This ensures proper rendering compatibility when materials are updated or changed.
        /// </summary>
        [JsonProperty("MaterialVersion", Required = Required.Always)]
        public string MaterialVersion = string.Empty;
        
        /// <summary>
        /// The unique identifier for the 3D mesh element this split represents.
        /// This ID corresponds to a specific mesh asset in the UGC template system.
        /// </summary>
        [JsonProperty("ElementId", Required = Required.Always)]
        public string ElementId = string.Empty;
        
        /// <summary>
        /// The collection of regions within this split that can be styled independently.
        /// Each region represents a specific area of the mesh that can have its own
        /// colors, patterns, and material properties applied.
        /// </summary>
        [JsonProperty("Regions", Required = Required.Always)]
        public List<Region> Regions;
        /// <summary>
        /// If true, any color or pattern set on the regions will be ignored and the default albedo texture
        /// will be used. Surface textures will still be applied.
        /// </summary>
        [JsonProperty("UseDefaultColors", Required = Required.Default)]
        public bool UseDefaultColors;
        /// <summary>
        /// Projected Textures are specific to the UV space of a given mesh.  If multiple are present, they
        /// will be composited in List order (first element on bottom) and flattened prior to being set in
        /// their specified MaterialProperty (texture channel).
        /// Note: for now they will represent color, but a more flexible idea is to have them store projected
        /// uv values so they can be reused for different textures.
        /// </summary>
        [JsonProperty("ProjectedTextures")]
        public List<ProjectedTexture> ProjectedTextures;
        
        
        /// <summary>
        /// Determines whether this split is equivalent to another split by comparing all properties.
        /// Two splits are considered equivalent if they have the same MaterialVersion, ElementId, 
        /// Regions, UseDefaultColors setting, and ProjectedTextures.
        /// </summary>
        /// <param name="other">The other split to compare against.</param>
        /// <returns>True if the splits are equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(Split other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || GetType() != other.GetType())
            {
                return false;
            }

            bool thisHasAnyRegions = !(Regions is null) && Regions.Count > 0;
            bool otherHasAnyRegions = !(other.Regions is null) && other.Regions.Count > 0;
            if (thisHasAnyRegions != otherHasAnyRegions)
            {
                return false;
            }

            bool thisHasAnyProjectedTextures = ProjectedTextures is not null && ProjectedTextures.Count > 0;
            bool otherHasAnyProjectedTextures = other.ProjectedTextures is not null && other.ProjectedTextures.Count > 0;
            if (thisHasAnyProjectedTextures != otherHasAnyProjectedTextures)
            {
                return false;
            }

            return
                ModelUtils.AreEqual(MaterialVersion, other.MaterialVersion) &&
                ModelUtils.AreEqual(ElementId, other.ElementId) &&
                (Regions?.SequenceEqual(other.Regions, ModelComparer.Instance) ?? true) &&
                (UseDefaultColors == other.UseDefaultColors) &&
                (ProjectedTextures?.SequenceEqual(other.ProjectedTextures, ModelComparer.Instance) ?? true);
        }
        
        /// <summary>
        /// Computes a hash code for this split based on all its properties.
        /// The hash includes MaterialVersion, ElementId, Regions, UseDefaultColors, and ProjectedTextures
        /// to ensure that equivalent splits produce the same hash code.
        /// </summary>
        /// <returns>A hash code representing the current state of this split.</returns>
        public int ComputeHash()
        {
            return HashingUtils.GetCombinedHashCode(
                MaterialVersion,
                ElementId,
                Regions.ComputeModelsCollectionHash(),
                UseDefaultColors,
                ProjectedTextures?.ComputeModelsCollectionHash()
            );
        }
        
        /// <summary>
        /// Creates a deep copy of this split, including all regions, projected textures, and nested properties.
        /// The returned split is completely independent of the original and can be modified
        /// without affecting the source split.
        /// </summary>
        /// <returns>A new Split instance that is a deep copy of this split.</returns>
        public Split DeepCopy()
        {
            return new Split()
            {
                ElementId = ElementId,
                MaterialVersion = MaterialVersion,
                Regions = Regions.DeepCopyList(),
                UseDefaultColors = UseDefaultColors,
                ProjectedTextures = ProjectedTextures.DeepCopyList()
            };
        }
        
        /// <summary>
        /// Performs a deep copy of this split's properties into the specified destination split.
        /// This method updates the destination split's properties to match this split,
        /// including deep copying of all regions, projected textures, and nested objects.
        /// </summary>
        /// <param name="destination">The target split to copy properties into. If null, no operation is performed.</param>
        public void DeepCopy(Split destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.ElementId = ElementId;
            destination.MaterialVersion = MaterialVersion;
            Regions.DeepCopyList(ref destination.Regions);
            destination.UseDefaultColors = UseDefaultColors;
            ProjectedTextures.DeepCopyList(ref destination.ProjectedTextures);
        }

        bool IModel.IsEquivalentTo(object other)
            => other is Split split && IsEquivalentTo(split);

        object ICopyable.DeepCopy()
            => DeepCopy();

        void ICopyable.DeepCopy(object destination)
            => DeepCopy(destination as Split);
    }
}
