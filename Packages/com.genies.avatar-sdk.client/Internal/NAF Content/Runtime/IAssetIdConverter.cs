using Cysharp.Threading.Tasks;

namespace Genies.Naf.Content
{
    public interface IAssetIdConverter
    {
        UniTask<string> ConvertToUniversalIdAsync(string assetId);
    }
}
