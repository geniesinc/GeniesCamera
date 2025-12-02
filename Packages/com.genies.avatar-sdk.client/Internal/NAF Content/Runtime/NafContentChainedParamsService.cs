using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Chained set of IAssetParamsServices that will try to resolve params and ids for a given assetId
    /// order of resolution depends on input
    /// </summary>
    public class NafContentChainedParamsService : IAssetParamsService, IAssetIdConverter
    {
        private readonly IEnumerable<IAssetParamsService> _services;
        private readonly IEnumerable<IAssetIdConverter> _converters;

        public NafContentChainedParamsService(IEnumerable<IAssetParamsService> paramServices, IEnumerable<IAssetIdConverter> converterServices)
        {
            if (paramServices is null)
            {
                _services = Array.Empty<IAssetParamsService>();
                return;
            }
            _services = paramServices;

            if (converterServices is null)
            {
                _converters = Array.Empty<IAssetIdConverter>();
                return;
            }
            _converters = converterServices;
        }


        public async UniTask<string> ConvertToUniversalIdAsync(string assetId)
        {
            foreach (IAssetIdConverter converters in _converters)
            {
                var universalId = await converters.ConvertToUniversalIdAsync(assetId);
                // return first success
                if (!string.IsNullOrEmpty(universalId))
                {
                    return universalId;
                }
            }
            return assetId;
        }

        public async UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId)
        {
            foreach (IAssetParamsService service in _services)
            {
                Dictionary<string, string> fParams = await service.FetchParamsAsync(assetId);
                if (fParams != null)
                {
                    return fParams;
                }
            }
            return default;
        }

    }
}
