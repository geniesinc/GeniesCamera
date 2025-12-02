using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class FlairAsset : IAsset
    {
        public string Id { get; }
        public string Lod { get; }

        public string SlotId;

        public Texture2D AlbedoTransparency;
        public Texture2D Normal;
        public Texture2D MetallicSmoothness;
        public Texture2D RgbaMask;
        
        public FlairAsset(string id, string lod, string slotId, Texture2D albedoTransparency, Texture2D normal, Texture2D metallicSmoothness, Texture2D rgbaMask)
        {
            Id = id;
            Lod = lod;
            SlotId = slotId;
            AlbedoTransparency = albedoTransparency;
            Normal = normal;
            MetallicSmoothness = metallicSmoothness;
            RgbaMask = rgbaMask;
        }
    }
}
