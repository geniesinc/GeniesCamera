using Genies.AssetLocations;
using Genies.FeatureFlags;
using Genies.Inventory.Providers;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Inventory.Installers
{
    [AutoResolve]
    public class LocationsFromInventoryInstaller : IGeniesInstaller
    {
        public int OperationOrder => DefaultInstallationGroups.PostCoreServices;
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        public void Install(IContainerBuilder builder)
        {
            builder.Register<IResourceLocationMetadataProvider<UserInventoryItem>,
                UniversalContentLocationsFromInventory>(Lifetime.Singleton).AsSelf();

            builder.Register<IResourceLocationMetadataProvider<DefaultInventoryAsset>,
                UniversalContentLocationsFromDefaultInventory>(Lifetime.Singleton);
            
            builder.Register<IResourceLocationMetadataProvider<DefaultAnimationLibraryAsset>,
                DynamicContentLocationsFromDefaultInventory>(Lifetime.Singleton);
        }

        // Prior to Naf integration this is no longer needed
        private void InstallProvidersLegacy(IContainerBuilder builder)
        {
            if (_FeatureFlagsManager is not null && _FeatureFlagsManager.IsFeatureEnabled(SharedFeatureFlags.AddressablesInventoryLocations))
            {
                // Locations for addressables and NAf mutually exclusive only load 1 or the other
                // in the event of having both enabled Addressables will only resolve the first one that registers..
                if (_FeatureFlagsManager is not null && _FeatureFlagsManager.IsFeatureEnabled(SharedFeatureFlags.UniversalContentLocations))
                {
                    builder.Register<IResourceLocationMetadataProvider<UserInventoryItem>,
                        UniversalContentLocationsFromInventory>(Lifetime.Singleton).AsSelf();
                }
                else
                {
                    builder.Register<IResourceLocationMetadataProvider<UserInventoryItem>,
                        DynamicContentLocationsFromInventory>(Lifetime.Singleton).AsSelf();
                }
            }
        }
    }
}
