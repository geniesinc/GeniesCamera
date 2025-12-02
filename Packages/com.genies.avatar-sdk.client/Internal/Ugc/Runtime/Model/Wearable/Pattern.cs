using System;
using UnityEngine;
using Newtonsoft.Json;
using Genies.Utilities;

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
    /// Defines the type of pattern rendering mode to use.
    /// Different pattern types support different combinations of colors, textures, and effects.
    /// </summary>
    public enum PatternType : byte
    {
        /// <summary>
        /// Standard textured pattern mode that uses texture images with optional color adjustments.
        /// Supports texture scaling, rotation, offset, and HSG (Hue, Saturation, Gain) modifications.
        /// </summary>
        Textured,
        
        /// <summary>
        /// Duotone pattern mode that creates patterns using two colors and contrast controls.
        /// Instead of textures, this mode generates patterns using color blending and contrast adjustments.
        /// </summary>
        Duotone
    }

    /// <summary>
    /// Defines pattern properties that can be applied to a region for visual styling.
    /// Patterns support both textured and duotone modes, with various transformation and color adjustment options.
    /// This class handles texture mapping, color blending, geometric transformations, and procedural pattern generation.
    /// </summary>
    [Serializable]
    public class Pattern : IModel<Pattern>
    {
        /// <summary>
        /// The remote URL where the pattern texture can be downloaded.
        /// Used for dynamic texture loading from external sources.
        /// </summary>
        [JsonProperty("TextureRemoteUrl")]
        public string TextureRemoteUrl = string.Empty;
        
        /// <summary>
        /// The type of pattern rendering to use (Textured or Duotone).
        /// This determines which properties are active and how the pattern is generated.
        /// </summary>
        [JsonProperty("PatternType")]
        public PatternType Type = PatternType.Textured;
        
        /// <summary>
        /// The local identifier for the pattern texture asset.
        /// Used to reference textures that are bundled with the application or cached locally.
        /// </summary>
        [JsonProperty("TextureName")]
        public string TextureId = string.Empty;
        
        /// <summary>
        /// The scaling factor applied to the pattern texture.
        /// Values greater than 1.0 will tile the pattern, while values less than 1.0 will scale it up.
        /// </summary>
        [JsonProperty("Scale")]
        public float Scale = 1.0f;
        
        /// <summary>
        /// The UV offset applied to the pattern texture in 2D space.
        /// Used to shift the pattern's position on the surface without changing its scale or rotation.
        /// </summary>
        [JsonProperty("Offset")]
        public Vector2 Offset = Vector2.zero;
        
        /// <summary>
        /// The rotation angle applied to the pattern texture in degrees.
        /// Positive values rotate the pattern clockwise.
        /// </summary>
        [JsonProperty("Rotation")]
        public float Rotation = 0.0f;
        
        /// <summary>
        /// Hue shift adjustment applied to the pattern colors.
        /// Values range from -1.0 to 1.0, with 0.0 being no hue change.
        /// </summary>
        [JsonProperty("Hue")]
        public float Hue = 0.0f;
        
        /// <summary>
        /// Saturation adjustment applied to the pattern colors.
        /// Values around 1.0 maintain original saturation, while 0.0 creates grayscale and values > 1.0 increase vibrancy.
        /// </summary>
        [JsonProperty("Saturation")]
        public float Saturation = 1.0f;
        
        /// <summary>
        /// Gain (brightness) adjustment applied to the pattern.
        /// Values around 1.0 maintain original brightness, while values > 1.0 brighten and < 1.0 darken the pattern.
        /// </summary>
        [JsonProperty("Gain")]
        public float Gain = 1.0f;
        
        /// <summary>
        /// Contrast adjustment used in Duotone pattern mode.
        /// Higher values create sharper transitions between the two duotone colors.
        /// </summary>
        [JsonProperty("DuoContrast")]
        public float DuoContrast = 0.0f;
        
        /// <summary>
        /// The first color used in Duotone pattern mode.
        /// This color typically represents the darker or shadow areas of the pattern.
        /// </summary>
        [JsonProperty("DuoColor1")]
        public Color DuoColor1 = Color.black;
        
        /// <summary>
        /// The second color used in Duotone pattern mode.
        /// This color typically represents the lighter or highlight areas of the pattern.
        /// </summary>
        [JsonProperty("DuoColor2")]
        public Color DuoColor2 = Color.black;

        /// <summary>
        /// Determines whether this pattern is equivalent to another pattern by comparing all properties.
        /// Two patterns are considered equivalent if they have the same type, texture references,
        /// transformations, color adjustments, and duotone settings.
        /// </summary>
        /// <param name="other">The other pattern to compare against.</param>
        /// <returns>True if the patterns are equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(Pattern other)
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
                Type == other.Type &&
                ModelUtils.AreEqual(TextureId, other.TextureId) &&
                Rotation == other.Rotation &&
                Scale == other.Scale &&
                Offset == other.Offset &&
                Hue == other.Hue &&
                Saturation == other.Saturation &&
                Gain == other.Gain &&
                DuoContrast == other.DuoContrast &&
                DuoColor1 == other.DuoColor1 &&
                DuoColor2 == other.DuoColor2;
        }

        /// <summary>
        /// Computes a hash code for this pattern based on all its properties.
        /// The hash includes all transformation, color adjustment, and texture properties
        /// to ensure that equivalent patterns produce the same hash code.
        /// </summary>
        /// <returns>A hash code representing the current state of this pattern.</returns>
        public int ComputeHash()
        {
            return HashingUtils.GetCombinedHashCode(
                Type,
                TextureId,
                Rotation,
                Scale,
                Offset,
                Hue,
                Saturation,
                Gain,
                DuoContrast,
                DuoColor1,
                DuoColor2
            );
        }

        /// <summary>
        /// Creates a deep copy of this pattern with all its properties.
        /// The returned pattern is completely independent of the original and can be modified
        /// without affecting the source pattern.
        /// </summary>
        /// <returns>A new Pattern instance that is a deep copy of this pattern.</returns>
        public Pattern DeepCopy()
        {
            return new Pattern()
            {
                Type = Type,
                TextureId = TextureId,
                Rotation = Rotation,
                Scale = Scale,
                Offset = Offset,
                Hue = Hue,
                Saturation = Saturation,
                Gain = Gain,
                DuoContrast = DuoContrast,
                DuoColor1 = DuoColor1,
                DuoColor2 = DuoColor2,
            };
        }

        /// <summary>
        /// Performs a deep copy of this pattern's properties into the specified destination pattern.
        /// This method updates the destination pattern's properties to match this pattern,
        /// copying all transformation, color adjustment, and texture settings.
        /// </summary>
        /// <param name="destination">The target pattern to copy properties into. If null, no operation is performed.</param>
        public void DeepCopy(Pattern destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.Type = Type;
            destination.TextureId = TextureId;
            destination.Rotation = Rotation;
            destination.Scale = Scale;
            destination.Offset = Offset;
            destination.Hue = Hue;
            destination.Saturation = Saturation;
            destination.Gain = Gain;
            destination.DuoContrast = DuoContrast;
            destination.DuoColor1 = DuoColor1;
            destination.DuoColor2 = DuoColor2;
        }

        bool IModel.IsEquivalentTo(object other)
            => other is Pattern pattern && IsEquivalentTo(pattern);

        object ICopyable.DeepCopy()
            => DeepCopy();

        void ICopyable.DeepCopy(object destination)
            => DeepCopy(destination as Pattern);
    }
}
