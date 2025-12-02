using UnityEngine.ResourceManagement.ResourceProviders;

namespace Genies.Addressables.CustomResourceLocation
{
    public class CustomBundledAssetProvider : BundledAssetProvider
    {
        private const string _providerSuffix = "dynamic_custom";
        private static string _customProviderIdOverride;
        public static string CustomProviderId => _customProviderIdOverride ??= $"{typeof(CustomBundledAssetProvider).FullName}{_providerSuffix}";
        
        public CustomBundledAssetProvider() {}

        public CustomBundledAssetProvider(string providerId = null)
        {
            _customProviderIdOverride = providerId;
            m_ProviderId = providerId ?? CustomProviderId;
        }
    }
}
