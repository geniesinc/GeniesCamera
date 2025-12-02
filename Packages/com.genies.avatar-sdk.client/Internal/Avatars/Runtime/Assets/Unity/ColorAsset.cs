using Genies.Assets.Services;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class ColorAsset : IAsset
    {
        public string Id { get; }
        public string Lod => AssetLod.Default;
        public Color Color { get; }

        public ColorAsset(string id, Color color)
        {
            Id = id;
            Color = color;
        }
    }
}
