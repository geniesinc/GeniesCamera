using System;
using Cysharp.Threading.Tasks;
using Genies.Addressables.UniversalResourceLocation;
using Genies.CrashReporting;
using Genies.Inventory.Installers;
using Genies.Naf.Content;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Naf.Addressables
{
    [AutoResolve]
    [Serializable]
    public class NafResourceProviderInstaller : IGeniesInstaller, IGeniesInitializer,
        IRequiresInstaller<NafContentInstaller>,
        IRequiresInstaller<InventoryServiceInstaller>
    {
        public int OperationOrder => DefaultInstallationGroups.DefaultServices + 2;

        // if this does not work just make a mono that on enable Registers this classes..
        public NafAssetResolverConfig nafResolverConfig;
        public void Install(IContainerBuilder builder)
        {
            if (nafResolverConfig == null)
            {
#if GENIES_INTERNAL
                CrashReporter.Log("Using default NAF asset resolver configuration.");
#endif
                if (NafSettings.TryLoadDefault(out NafSettings nafSettings))
                {
                    nafResolverConfig = nafSettings.defaultAssetResolverConfig;
                }
            }

            if (nafResolverConfig != null)
            {
                builder.RegisterInstance(nafResolverConfig);
            }
            else
            {
                Debug.LogWarning($"Failed to register instance of {nameof(NafAssetResolverConfig)}.");
            }

            builder.Register<NafContentResourceProvider>(Lifetime.Singleton)
                .As<ICustomResourceProvider>()
                .WithParameter(nafResolverConfig);

            builder.Register<NafContentLocationsFromInventory>(Lifetime.Singleton)
                .As<IInventoryNafLocationsProvider>();
        }

        public async UniTask Initialize()
        {
            if (NafPlugin.IsInitialized is false)
            {
                NafPlugin.Initialize();
            }

            if (NafContentInitializer.IsInitialized is false)
            {
                await NafContentInitializer.Initialize();
            }
        }
    }
}
