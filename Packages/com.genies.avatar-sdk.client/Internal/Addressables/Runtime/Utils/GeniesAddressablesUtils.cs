using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Genies.Addressables.Utils
{
    public class GeniesAddressablesUtils
    {
        // can call it multiple times it will only  register the provider once
        public static void RegisterNewResourceProviderOnAddressables(ResourceProviderBase newProvider)
        {
            IList<IResourceProvider> resourceProviders = UnityEngine.AddressableAssets.Addressables.ResourceManager.ResourceProviders;
            // Check if our provider is already registered with Addressables
            if (resourceProviders.Any(provider => provider.ProviderId == newProvider.ProviderId))
            {
                return;
            }

            resourceProviders.Add(newProvider);
        }
    }
}
