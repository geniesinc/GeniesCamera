using Newtonsoft.Json;
using System;
using Genies.Utilities;
using UnityEngine;

/*
 * There will likely be several of these definitions.  This one is created to
 * match the current MegaShader5.7, though that version '5.7' reflects changes to
 * the internals of the shader AND its parameters, whereas this definition
 * should version just changes to the parameters, yet match a particular shader.
 *
 * MaterialVersion format should be <MaterialName_SchemaVer> where MaterialName includes
 * the shader internals version.
 * For example, MegaShader5.7 and MegaShader5.8 both have the same parameters:
 * they use the same definition, and their MaterialVersion should be
 * MegaShader5.7_1-0-0, MegaShader5.8_1-0-0 respectively.
 * MegaShader5.9 introduces a new parameter, it has MaterialVersion MegaShader5.9_1-0-1,
 * and creates a new MegaShaderStyleDefinition.cs and MegaShaderStyleDefinitionSchema.json.
 * An entirely new material/shader is written, it has MaterialVersion: AwesomeShader1.0_1-0-0,
 * and also creates a new defnition class and schema.
 */

namespace Genies.Ugc
{
    /// <summary>
    /// Defines the visual styling properties for a region within a UGC element.
    /// A style includes color, surface texture, pattern, and material version information
    /// that determines how a specific region of a mesh will be rendered.
    /// </summary>
    [Serializable]
    public class Style : IModel<Style>
    {
        /// <summary>
        /// A unique identifier for this style based on its computed hash.
        /// This ID can be used for caching and comparison purposes.
        /// </summary>
        [JsonIgnore]
        public string Id => ComputeHash().ToString();
        
        /// <summary>
        /// The version of the material/shader system this style is compatible with.
        /// This ensures proper rendering compatibility when materials are updated.
        /// </summary>
        [JsonProperty("MaterialVersion", Required = Required.Always)]
        public string MaterialVersion = string.Empty;
        
        /// <summary>
        /// The base color applied to this region.
        /// This color is combined with textures and patterns during rendering.
        /// </summary>
        [JsonProperty("Color")]
        public Color Color = Color.black;
        
        /// <summary>
        /// The identifier for the surface texture applied to this region.
        /// Surface textures provide detail like normal maps, roughness, or other material properties.
        /// </summary>
        [JsonProperty("SurfaceTextureName")]
        public string SurfaceTextureId = string.Empty;
        
        /// <summary>
        /// The scaling factor applied to the surface texture.
        /// Values greater than 1.0 will tile the texture, while values less than 1.0 will scale it up.
        /// </summary>
        [JsonProperty("SurfaceScale")]
        public float SurfaceScale = 1.0f;
        
        /// <summary>
        /// The pattern configuration applied to this region, including texture, scale, rotation, and color adjustments.
        /// If null, no pattern will be applied to the region.
        /// </summary>
        [JsonProperty("Pattern")]
        public Pattern Pattern;

        /// <summary>
        /// Determines whether this style is equivalent to another style by comparing all properties.
        /// Two styles are considered equivalent if they have the same MaterialVersion, Color,
        /// SurfaceTextureId, SurfaceScale, and Pattern configuration.
        /// </summary>
        /// <param name="other">The other style to compare against.</param>
        /// <returns>True if the styles are equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(Style other)
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
                ModelUtils.AreEqual(MaterialVersion, other.MaterialVersion) &&
                Color == other.Color &&
                ModelUtils.AreEqual(SurfaceTextureId, other.SurfaceTextureId) &&
                SurfaceScale == other.SurfaceScale &&
                (Pattern?.IsEquivalentTo(other.Pattern) ?? other.Pattern is null);
        }

        /// <summary>
        /// Computes a hash code for this style based on all its properties.
        /// The hash includes MaterialVersion, Color, SurfaceTextureId, SurfaceScale, and Pattern
        /// to ensure that equivalent styles produce the same hash code.
        /// </summary>
        /// <returns>A hash code representing the current state of this style.</returns>
        public int ComputeHash()
        {
            return HashingUtils.GetCombinedHashCode(
                MaterialVersion,
                Color,
                SurfaceTextureId,
                SurfaceScale,
                Pattern?.ComputeHash()
            );
        }

        /// <summary>
        /// Creates a deep copy of this style, including the pattern and all nested properties.
        /// The returned style is completely independent of the original and can be modified
        /// without affecting the source style.
        /// </summary>
        /// <returns>A new Style instance that is a deep copy of this style.</returns>
        public Style DeepCopy()
        {
            return new Style()
            {
                MaterialVersion = MaterialVersion,
                Color = Color,
                SurfaceTextureId = SurfaceTextureId,
                SurfaceScale = SurfaceScale,
                Pattern = Pattern?.DeepCopy(),
            };
        }

        /// <summary>
        /// Performs a deep copy of this style's properties into the specified destination style.
        /// This method updates the destination style's properties to match this style,
        /// including deep copying of the pattern and nested objects.
        /// </summary>
        /// <param name="destination">The target style to copy properties into. If null, no operation is performed.</param>
        public void DeepCopy(Style destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.MaterialVersion = MaterialVersion;
            destination.Color = Color;
            destination.SurfaceTextureId = SurfaceTextureId;
            destination.SurfaceScale = SurfaceScale;
            Pattern.DeepCopy(ref destination.Pattern);
        }

        bool IModel.IsEquivalentTo(object other)
            => other is Style style && IsEquivalentTo(style);

        object ICopyable.DeepCopy()
            => DeepCopy();

        void ICopyable.DeepCopy(object destination)
            => DeepCopy(destination as Style);
    }
}
