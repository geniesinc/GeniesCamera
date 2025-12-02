using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Naf
{
    public sealed class NoOpAssetParamsService : IAssetParamsService
    {
        public UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId)
        {
            return UniTask.FromResult<Dictionary<string, string>>(null);
        }
    }
}