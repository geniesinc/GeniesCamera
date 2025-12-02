using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Naf
{
    public interface IAssetParamsService
    {
        UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId);
    }
}