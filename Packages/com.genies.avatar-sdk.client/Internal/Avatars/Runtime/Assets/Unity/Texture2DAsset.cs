using UnityEngine;

namespace Genies.Avatars
{
    public sealed class Texture2DAsset : IAsset
    {
        public string Id { get; }
        public string Lod { get; }
        public Texture2D Texture { get; }

        public Texture2DAsset(string id, string lod, Texture2D texture)
        {
            Id = id;
            Lod = lod;
            Texture = texture;
        }
    }
}