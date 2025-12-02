using Genies.Assets.Services;

namespace Genies.Avatars
{
    public sealed class BlendShapePresetAsset : IAsset
    {
        public string Id { get; }
        public string Lod => AssetLod.Default;
        public BlendShapeAsset[] BlendShapeAssets { get; }
        
        public BlendShapePresetAsset(string id, BlendShapeAsset[] blendShapeAssets)
        {
            Id = id;
            BlendShapeAssets = blendShapeAssets;
        }
    }
}
