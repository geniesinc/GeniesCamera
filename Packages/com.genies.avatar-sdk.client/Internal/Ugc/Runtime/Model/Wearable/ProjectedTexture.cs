using System;
using Newtonsoft.Json;
using Genies.Utilities;

namespace Genies.Ugc
{

    /// <summary>
    /// Represents a texture that is projected onto a 3D mesh using UV mapping.
    /// Projected textures are specific to the UV space of a mesh and can be composited
    /// together when multiple projected textures are applied to the same material channel.
    /// This system supports both local texture assets and remote texture loading.
    /// </summary>
    [Serializable]
    public class ProjectedTexture : IModel<ProjectedTexture>
    {
        /// <summary>
        /// The remote URL where the texture can be downloaded.
        /// Used for dynamic texture loading from external sources.
        /// </summary>
        [JsonProperty("TextureRemoteUrl")]
        public string TextureRemoteUrl = string.Empty;
        
        /// <summary>
        /// The local identifier for the texture asset.
        /// Used to reference textures that are bundled with the application or cached locally.
        /// </summary>
        [JsonProperty("TextureId")]
        public string TextureId = string.Empty;
        
        /// <summary>
        /// The name of the material property (texture channel) where this texture will be applied.
        /// Common values include "_MainTex" for albedo, "_BumpMap" for normal maps, etc.
        /// </summary>
        [JsonProperty("MaterialPropertyName")]
        public string MaterialPropertyName = string.Empty;

        /// <summary>
        /// The remote URL for projection mapping data.
        /// Reserved for future use when projection data might be decoupled from image data,
        /// allowing multiple ProjectedTextures to share the same projection mapping.
        /// </summary>
        [JsonProperty("ProjectionRemoteUrl")]
        public string ProjectionRemoteUrl = string.Empty;
        
        /// <summary>
        /// The local identifier for projection mapping data.
        /// Reserved for future use when projection data might be decoupled from image data,
        /// allowing multiple ProjectedTextures to share the same projection mapping.
        /// </summary>
        [JsonProperty("ProjectionId")]
        public string ProjectionId = string.Empty;

        /// <summary>
        /// Determines whether this projected texture is equivalent to another by comparing all properties.
        /// Two projected textures are considered equivalent if they have the same texture references,
        /// material property name, and projection data.
        /// </summary>
        /// <param name="other">The other projected texture to compare against.</param>
        /// <returns>True if the projected textures are equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(ProjectedTexture other)
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
                ModelUtils.AreEqual(TextureRemoteUrl, other.TextureRemoteUrl) &&
                ModelUtils.AreEqual(TextureId, other.TextureId) &&
                ModelUtils.AreEqual(MaterialPropertyName, other.MaterialPropertyName) &&
                ModelUtils.AreEqual(ProjectionRemoteUrl, other.ProjectionRemoteUrl) &&
                ModelUtils.AreEqual(ProjectionId, other.ProjectionId);
        }

        /// <summary>
        /// Computes a hash code for this projected texture based on all its properties.
        /// The hash includes texture references, material property name, and projection data
        /// to ensure that equivalent projected textures produce the same hash code.
        /// </summary>
        /// <returns>A hash code representing the current state of this projected texture.</returns>
        public int ComputeHash()
        {
            return HashingUtils.GetCombinedHashCode(
                TextureRemoteUrl,
                TextureId,
                MaterialPropertyName,
                ProjectionRemoteUrl,
                ProjectionId
            );
        }

        /// <summary>
        /// Creates a deep copy of this projected texture with all its properties.
        /// The returned projected texture is completely independent of the original and can be modified
        /// without affecting the source projected texture.
        /// </summary>
        /// <returns>A new ProjectedTexture instance that is a deep copy of this projected texture.</returns>
        public ProjectedTexture DeepCopy()
        {
            return new ProjectedTexture()
            {
                TextureRemoteUrl = TextureRemoteUrl,
                TextureId = TextureId,
                MaterialPropertyName = MaterialPropertyName,
                ProjectionRemoteUrl = ProjectionRemoteUrl,
                ProjectionId = ProjectionId
            };
        }

        /// <summary>
        /// Performs a deep copy of this projected texture's properties into the specified destination.
        /// This method updates the destination projected texture's properties to match this one,
        /// copying all texture references, material property settings, and projection data.
        /// </summary>
        /// <param name="destination">The target projected texture to copy properties into. If null, no operation is performed.</param>
        public void DeepCopy(ProjectedTexture destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.TextureRemoteUrl = TextureRemoteUrl;
            destination.TextureId = TextureId;
            destination.MaterialPropertyName = MaterialPropertyName;
            destination.ProjectionRemoteUrl = ProjectionRemoteUrl;
            destination.ProjectionId = ProjectionId;
        }

        bool IModel.IsEquivalentTo(object other)
            => other is ProjectedTexture projtex && IsEquivalentTo(projtex);

        object ICopyable.DeepCopy()
            => DeepCopy();

        void ICopyable.DeepCopy(object destination)
            => DeepCopy(destination as ProjectedTexture);
    }
}
