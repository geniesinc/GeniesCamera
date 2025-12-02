using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Models
{
    /// <summary>
    /// Represents a container for Flair data in the Genies application.
    /// </summary>
    public class FlairContainer : OrderedScriptableObject, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;
        
        [SerializeField] 
        private string assetId; // The unique identifier for the FlairContainer instance.
        
        [SerializeField] 
        private string guid; // The unique identifier for the FlairContainer instance.
        
        [SerializeField] 
        private FlairAssetType assetType; // The assetType of Flair contained in this instance.

        [SerializeField] 
        private Texture2D albedoTransparency; // Albedo
        
        [SerializeField] 
        private Texture2D normal; // Albedo
        
        [SerializeField] 
        private Texture2D metallicSmoothness; // Albedo
        
        [SerializeField] 
        private Texture2D rgbaMask; // Albedo
        
        /// <summary>
        /// Gets or sets the asset ID for the FlairContainer instance.
        /// </summary>
        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }
        
        /// <summary>
        /// Gets or sets the unique identifier for the FlairContainer instance.
        /// </summary>
        public string Guid
        {
            get => guid;
            set => guid = value;
        }

        /// <summary>
        /// Gets or sets the assetType of Flair contained in this instance.
        /// </summary>
        public FlairAssetType AssetType
        {
            get => assetType;
            set => assetType = value;
        }

        /// <summary>
        /// Gets texture map associated with this TextureMapType type.
        /// </summary>
        public Texture2D GetTexture(TextureMapType type)
        {
            switch (type)
            {
                case TextureMapType.AlbedoTransparency:
                    return albedoTransparency;
                case TextureMapType.Normal:
                    return normal;
                case TextureMapType.MetallicSmoothness:
                    return metallicSmoothness;
                case TextureMapType.RgbaMask:
                    return rgbaMask;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Sets texture map associated with this TextureMapType type.
        /// </summary>
        public Texture2D SetTexture(TextureMapType type, Texture2D texture)
        {
            switch (type)
            {
                case TextureMapType.AlbedoTransparency:
                    albedoTransparency = texture;
                    break;
                case TextureMapType.Normal:
                    normal = texture;
                    break;
                case TextureMapType.MetallicSmoothness:
                    metallicSmoothness = texture;
                    break;
                case TextureMapType.RgbaMask:
                    rgbaMask = texture;
                    break;
            }
            return texture;
        }
    }
}