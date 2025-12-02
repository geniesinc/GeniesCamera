using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Inventory;
using Genies.Inventory.Installers;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Naf.Content
{
    [AutoResolve]
    public class NafContentInstaller : IGeniesInstaller, IGeniesInitializer,
        IRequiresInstaller<LocationsFromInventoryInstaller>
    {
        public int OperationOrder => (DefaultInstallationGroups.DefaultServices);
        // public int InitializationOrder => DefaultInstallationGroups.DefaultServices + 2;

        // Only load inventory V1 when we enable support for old AIGC wearables
        public bool IncludeInventoryV1 = false;
        
        public void Install(IContainerBuilder builder)
        {
            // Register ContentConfigService as singleton
            builder.Register<IContentConfigService, ContentConfigService>(Lifetime.Singleton)
                .AsSelf();
            
            builder.Register<NafContentService>(Lifetime.Singleton)
                .WithParameter(IncludeInventoryV1);
            
            builder.Register<NafContentLocalParamsService>(Lifetime.Singleton);

            // Register inventory event handler to respond to asset minting events
            builder.Register<NafInventoryEventHandler>(Lifetime.Singleton);

            builder.Register<IEnumerable<IAssetParamsService>>(resolver => new IAssetParamsService[]
                {
                    // order of resolution matters
                    resolver.Resolve<NafContentService>(),
                    resolver.Resolve<NafContentLocalParamsService>(),
                }
            , Lifetime.Singleton);

            builder.Register<IEnumerable<IAssetIdConverter>>(resolver => new IAssetIdConverter[]
                {
                    // keep same order
                    resolver.Resolve<NafContentService>(),
                    resolver.Resolve<NafContentLocalParamsService>(),
                }
                , Lifetime.Singleton);

            // Register the chained service as the single IAssetParamsService
            builder.Register<NafContentChainedParamsService>(Lifetime.Singleton)
                .As<IAssetParamsService>()
                .As<IAssetIdConverter>();
        }

        public UniTask Initialize()
        {
            if (NafPlugin.IsInitialized is false)
            {
                NafPlugin.Initialize();
            }

            // Initialize the inventory event handler to subscribe to events
            var eventHandler = this.GetService<NafInventoryEventHandler>();
            eventHandler?.Initialize();

            return UniTask.CompletedTask;
        }
    }
}
