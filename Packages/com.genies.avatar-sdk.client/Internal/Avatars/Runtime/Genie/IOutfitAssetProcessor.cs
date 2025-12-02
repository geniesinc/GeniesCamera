using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IOutfitAssetProcessor
    {
        UniTask ProcessAddedAssetAsync(OutfitAsset asset);
        UniTask ProcessRemovedAssetAsync(OutfitAsset asset);
    }
}