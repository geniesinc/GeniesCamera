
using Cysharp.Threading.Tasks;

namespace Genies.APIResolver
{
    public interface IAPIResolverService
    {
        public UniTask<string> GetPartyId(string partyBundle);
    }
}
