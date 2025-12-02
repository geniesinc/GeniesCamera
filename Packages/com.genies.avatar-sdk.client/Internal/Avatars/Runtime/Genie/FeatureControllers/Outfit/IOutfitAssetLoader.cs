using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;

namespace Genies.Avatars
{
    public interface IOutfitAssetLoader
    {
        IReadOnlyList<string> SupportedTypes { get; }

        UniTask<Ref<OutfitAsset>> LoadAsync(OutfitAssetMetadata metadata, string lod = AssetLod.Default);
        bool IsOutfitAssetTypeSupported(string type);
    }
}
