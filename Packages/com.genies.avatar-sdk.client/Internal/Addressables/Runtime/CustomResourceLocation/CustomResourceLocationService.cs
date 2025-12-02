using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine.ResourceManagement;

namespace Genies.Addressables.CustomResourceLocation
{
    public class CustomResourceLocationService : BaseAddressablesService
    {
        public static void InitializeResourceProviders()
        {
            ResourceManager rm = UnityEngine.AddressableAssets.Addressables.ResourceManager;

            if (rm.ResourceProviders.All(provider => provider.ProviderId != CustomBundledAssetProvider.CustomProviderId))
            {
                rm.ResourceProviders.Add(new CustomBundledAssetProvider(CustomBundledAssetProvider.CustomProviderId));
            }

            if (rm.ResourceProviders.All(provider => provider.ProviderId != CustomAssetBundleProvider.CustomProviderId))
            {
                rm.ResourceProviders.Add(new CustomAssetBundleProvider(CustomAssetBundleProvider.CustomProviderId));
            }
        }
    }
}
