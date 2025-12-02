using Cysharp.Threading.Tasks;
using Genies.ServiceManagement;
using Genies.Utilities.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Genies.Addressables
{
    /// <summary>
    /// Initializer that loads our Addressables content catalogs. If the catalog service singleton is already initialized and
    /// loaded then it will skip loading again (ignoring the configured base URL and content types).
    /// </summary>
    public sealed class AddressablesCatalogInitializer : Initializer
    {
        [SerializeField] private bool overrideContent; // override the content URL flag
        [SerializeField] private string contentOverrideUrl = AddressablesCatalogProvider.CloudFrontUrl(AddressablesCatalogProvider.Environment.Current); // override content URL
        [SerializeField] private AddressablesCatalogProvider.Environment contentEnvironment = AddressablesCatalogProvider.Environment.Current;
        [SerializeField] private AddressablesCatalogService.App contentApplication = AddressablesCatalogService.App.GeniesParty;

        protected override string _InitializationSuccessMessage => "Loaded Addressables content catalogs";
        private static bool _hasInitiailizedAtLeastOnce = false;
        public static bool _forceStaticInitiailizeOnce = false;

        protected override UniTask InitializeAsync()
        {
            if (_forceStaticInitiailizeOnce && _hasInitiailizedAtLeastOnce)
            {
                return UniTask.CompletedTask;
            }

            var contentUrl = overrideContent ? contentOverrideUrl : AddressablesCatalogProvider.CloudFrontUrl(contentEnvironment);

            var service = ServiceManager.Get<AddressablesCatalogService>();
            Assert.IsNotNull(service,
                $"Ensure service installers are called prior to initializing. See {nameof(AddressableServicesInstaller)}.");

            var provider = ServiceManager.Get<AddressablesCatalogProvider>();
            provider?.SetStaticBaseUrl(contentUrl);
            Assert.IsNotNull(provider,
                $"Ensure service installers are called prior to initializing. See {nameof(AddressableServicesInstaller)}.");

            _hasInitiailizedAtLeastOnce = true;

            return service.AreCatalogsLoaded ? UniTask.CompletedTask : service.InitializeCatalogs(provider, contentApplication);
        }
    }
}
