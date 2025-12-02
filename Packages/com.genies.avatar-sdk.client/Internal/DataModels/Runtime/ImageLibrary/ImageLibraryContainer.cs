using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Models
{
    public class ImageLibraryContainer : OrderedScriptableObject, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;
        public virtual ImageLibraryAssetType AssetType => ImageLibraryAssetType.none;
        public int MapCount => maps?.Count ?? 0;
        public Texture2D MainMap => maps.Count > 0 ? GetMap(TextureMapType.AlbedoTransparency) ?? maps[0].Texture : null;

        public string AssetId;
        public string Guid;
        public string Category;
        public string Skin;

        [SerializeField] private List<TextureMap> maps;

        public Texture2D GetMap(TextureMapType mapType)
        {
            maps ??= new List<TextureMap>();
            TextureMap map = maps.Find(t => t.Type == mapType);
            return map?.Texture;
        }

        public List<TextureMap> GetMaps()
        {
            return maps.ToList();
        }

        public bool MapExists(TextureMapType mapType)
        {
            maps ??= new List<TextureMap>();
            TextureMap map = maps.Find(t => t.Type == mapType);
            return map != null;
        }

        public void SetMap(TextureMapType mapType, Texture2D texture)
        {
            maps ??= new List<TextureMap>();
            TextureMap map = maps.Find(t => t.Type == mapType);
            if (map != null)
            {
                map.Texture = texture;
            }
            else
            {
                maps.Add(new TextureMap { Type = mapType, Texture = texture });
            }
        }
    }
}

