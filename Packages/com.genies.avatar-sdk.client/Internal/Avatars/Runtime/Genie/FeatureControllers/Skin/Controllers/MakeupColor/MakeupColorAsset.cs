using Genies.Assets.Services;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class MakeupColorAsset : IAsset
    {
        public string Id { get; }
        public string Lod => AssetLod.Default;
        public Color Color1 { get; }
        public Color Color2 { get; }
        public Color Color3 { get; }
        
        public MakeupColorAsset(string id, Color color1, Color color2, Color color3)
        {
            Id = id;
            Color1 = color1;
            Color2 = color2;
            Color3 = color3;
        }
        
        public MakeupColorAsset(Color color1, Color color2, Color color3)
        {
            Color1 = color1;
            Color2 = color2;
            Color3 = color3;
            Id = ToString();
        }
        
        public override string ToString()
        {
            return Id ?? $"{ColorUtility.ToHtmlStringRGBA(Color1)}|{ColorUtility.ToHtmlStringRGBA(Color2)}|{ColorUtility.ToHtmlStringRGBA(Color3)}";
        }
    }
}
