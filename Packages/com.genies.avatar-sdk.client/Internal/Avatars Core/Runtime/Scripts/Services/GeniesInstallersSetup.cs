using System.Collections.Generic;
using Genies.Addressables;
using Genies.Avatars.Services;
using Genies.Closet;
using Genies.Inventory.Installers;
using Genies.Login;
using Genies.Login.Native;
using Genies.Naf.Addressables;
using Genies.Naf.Content;
using Genies.ServiceManagement;
using Genies.Services.Configs;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Configuration class for setting up service installers for the Genies Avatar SDK.
    /// Manages default configurations for feature flags and dynamic configs, and provides
    /// override properties for customizing specific installer instances. You can override
    /// specific installers by setting the corresponding property (e.g., AvatarCmsInstallerOverride).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesInstallersSetup
#else
    public class GeniesInstallersSetup
#endif
    {
        public AddressableServicesInstaller AddressableServicesInstallerOverride { get; set; }
        public AvatarServiceInstaller AvatarServiceInstallerOverride { get; set; }
        public ClosetServiceInstaller ClosetServiceInstallerOverride { get; set; }
        public GeniesAvatarSdkInstaller GeniesAvatarSdkInstallerOverride { get; set; }
        public IGeniesLoginInstaller GeniesLoginInstallerOverride { get; set; }
        public InventoryServiceInstaller InventoryServiceInstallerOverride { get; set; }
        public LocationsFromInventoryInstaller LocationsFromInventoryInstallerOverride { get; set; }
        public NafContentInstaller NafContentInstallerOverride { get; set; }
        public NafResourceProviderInstaller NafResourceProviderInstallerOverride { get; set; }

        public GeniesInstallersSetup(GeniesApiConfig config = null)
        {
            if (config is not null)
            {
                GeniesApiConfigManager.SetApiConfig(config);
            }
        }

        public List<IGeniesInstaller> ConstructInstallersList()
        {
            return new()
            {

                // Deps for GeniesAvatarSdkInstaller ordered by requirements.
                GeniesLoginInstallerOverride ?? new NativeGeniesLoginInstaller()
                {
                    BaseUrl = GeniesApiConfigManager.GetApiPath(),
                },

                AvatarServiceInstallerOverride ?? new AvatarServiceInstaller(),
                ClosetServiceInstallerOverride ?? new ClosetServiceInstaller(),
                AddressableServicesInstallerOverride ?? new AddressableServicesInstaller()
                {
                    InitializeCatalogs = false
                },
                LocationsFromInventoryInstallerOverride ?? new LocationsFromInventoryInstaller(),
                InventoryServiceInstallerOverride ?? new InventoryServiceInstaller(),
                NafContentInstallerOverride ?? new NafContentInstaller(),
                NafResourceProviderInstallerOverride ?? new NafResourceProviderInstaller(),

                // Core installer for this library
                GeniesAvatarSdkInstallerOverride ?? new GeniesAvatarSdkInstaller(),
            };
        }
    }
}
