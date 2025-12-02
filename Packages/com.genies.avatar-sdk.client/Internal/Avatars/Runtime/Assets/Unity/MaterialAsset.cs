using UnityEngine;

namespace Genies.Avatars
{
    public sealed class MaterialAsset : IAsset
    {
        public string Id { get; }
        public string Lod { get; }
        public Material Material { get; }
        
        public MaterialAsset(string id, string lod, Material material)
        {
            Id = id;
            Lod = lod;
            Material = material;
        }
    }
}