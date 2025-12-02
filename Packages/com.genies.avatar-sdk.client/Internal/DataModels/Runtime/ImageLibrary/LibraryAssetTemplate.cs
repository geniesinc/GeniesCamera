using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
    public class LibraryAssetTemplate : OrderedScriptableObject, IDynamicAsset
    {
        /// <summary>
        /// BaseType for ImageLibrary assets, types include Patterns, Tattoos, Decals, Patches
        /// Meant for wearables (including generative) and can extend to hair and other “things”
        /// </summary>
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        [SerializeField] private string assetId;
        [SerializeField] private string assetType;
        [SerializeField] private string category;
        [SerializeField] private List<TextureMap> maps;


        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public string AssetType
        {
            get => assetType;
            set => assetType = value;
        }

        public string Category
        {
            get => category;
            set => category = value;
        }

        public List<TextureMap> Maps
        {
            get => maps;
            set => maps = value;
        }

        public Texture2D GetMap(TextureMapType mapType)
        {
            var map = maps.Find(t => t.Type == mapType);
            return map.Texture;
        }

        public Texture2D AlbedoTransparency => GetMap(TextureMapType.AlbedoTransparency);

        public Texture2D MetallicSmoothness => GetMap(TextureMapType.MetallicSmoothness);

        public Texture2D Normal => GetMap(TextureMapType.Normal);

        public Texture2D RgbaMask => GetMap(TextureMapType.RgbaMask);
    }
}
