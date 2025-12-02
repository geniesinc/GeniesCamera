using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Addressables.CustomResourceLocation;
using Genies.FeatureFlags;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Addressables
{
    /// <summary>
    /// Uses feature flags to load content from pipeline v2 (static) or v3 (dynamic content)
    /// </summary>
    [AutoResolve] [Serializable]
    public class AddressableServicesInstaller : IGeniesInstaller, IGeniesInitializer
    {
        public int OperationOrder => DefaultInstallationGroups.DefaultServices;

        public bool InitializeCatalogs;
        public AddressablesCatalogService.App Application
        {
            get => _application;
            set => _application = value;
        }

        [SerializeField]
        private AddressablesCatalogService.App _application;

        public AddressableServicesInstaller(AddressablesCatalogService.App application)
        {
            Application = application;
        }

        public AddressableServicesInstaller() {}

        public void Install(IContainerBuilder builder)
        {
            builder.Register<IContentOverrideService, DummyContentOverrideService>(Lifetime.Singleton);

            var addressablesCatalogProvider = new AddressablesCatalogProvider();
            AddressablesCatalogProvider.InitializeCatalogs = InitializeCatalogs;
            ServiceManager.RegisterService(addressablesCatalogProvider);

            builder.Register<AddressablesCatalogProvider>(Lifetime.Singleton);
            builder.Register<AddressablesCatalogService>(Lifetime.Singleton);
            builder.Register<CustomResourceLocationService>(Lifetime.Singleton);
        }

        public UniTask Initialize()
        {
            AddressablesCatalogProvider catalogProvider = this.GetService<AddressablesCatalogProvider>();
            AddressablesCatalogService catalogService = this.GetService<AddressablesCatalogService>();

            if (catalogService == null)
            {
                Debug.LogError($"[{nameof(AddressableServicesInstaller)}] Catalog Service not found");
                return default;
            }

            if (InitializeCatalogs)
            {
                var tasks = new List<UniTask>() { catalogService.InitializeCatalogs(catalogProvider, Application), };
                return UniTask.WhenAll(tasks.ToArray());
            }

            return UniTask.CompletedTask;
        }
    }
}
