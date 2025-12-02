
using System;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;

namespace Genies.APIResolver
{
    public class APIResolverService: IAPIResolverService
    {
        private APIResolverData _apiResolverData;
        public APIResolverService(APIResolverData apiResolverData)
        {
            _apiResolverData = apiResolverData;
        }

        public UniTask<string> GetPartyId(string partyBundle)
        {
            try
            {
                return UniTask.FromResult(_apiResolverData.GetPartyId(partyBundle));
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"[APIResolverService] Party Not Found: {e}");
                return default;
            }
        }
    }
}
