using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IOutfitAssetMetadataService
    {
        UniTask<OutfitAssetMetadata> FetchAsync(string assetId);
        UniTask FetchAsync(IEnumerable<string> assetIds, ICollection<OutfitAssetMetadata> assets);
    }
}