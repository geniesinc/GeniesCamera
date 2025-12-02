using Cysharp.Threading.Tasks;

namespace Genies.Inventory
{
    public interface IContentConfigService
    {
        public UniTask<RootConfig> FetchConfig(string configId);
    }
}