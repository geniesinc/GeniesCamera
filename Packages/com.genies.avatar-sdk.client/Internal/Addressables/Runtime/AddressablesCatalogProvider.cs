using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.ABTesting;
using Genies.Addressables.CustomResourceLocation;
using Genies.FeatureFlags;
using Genies.ServiceManagement;
using Genies.Services.Configs;
using UnityEngine.Assertions;
using VContainer;

namespace Genies.Addressables
{
    public class AddressablesCatalogProvider : BaseAddressableProvider
    {
        private static AddressablesCatalogProvider _instance;

        private const string AbTestingCloudFrontUrlConfigName = "mobile-app-content-url";
        private const string AbTestingCloudFrontUrlKeyName = "url";

        private IABTestingService AbTestingService => ServiceManager.Get<IABTestingService>();
        private static IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        private string _staticBaseUrl;

        private const string ProdContentUrl = "https://d21fza0bihiyu8.cloudfront.net";
        private const string QaContentUrl = "https://d2oc63q9ws3yu.cloudfront.net";
        private const string LatestContentUrl = "https://dhumicg3s3z9l.cloudfront.net";
        private const string DynamicContentExternalThingsUrl = "https://deapxbkb5h2hk.cloudfront.net";
        private const string DynamicContentExternalGearUrl = "https://d282aabcatgfu2.cloudfront.net";
        private const string DynamicContentExternalSubSpeciesUrl = "https://dra0j9dd2q97.cloudfront.net";
        private const string DynamicContentExternalModelLibraryUrl = "https://d38q5c0lolpwy1.cloudfront.net";
        private const string DynamicContentExternalShadersUrl = "https://d23pxnteb1pne0.cloudfront.net";
        private const string SpecifyBucketUrl = @"https://genies-content.s3.us-west-2.amazonaws.com/{0}";
        private const string LegacyCloudfrontUrl = "https://d3q0qk695gf1v0.cloudfront.net";

        private static string _overrideContentUrl;
        private static bool _isOverride;

        private static string CloudfrontUrl => GeniesApiConfigManager.TargetEnvironment == BackendEnvironment.Prod
            ? ProdContentUrl
            : QaContentUrl;

        public readonly List<string> ContentTypes = new List<string>() {"static", "generative", "looks", "library" };
        public readonly List<string> DynContentTypes = new List<string>() {"static", "dynamic" };

        public enum Environment
        {
            Prod,
            Qa,
            Latest,
            Current,
        }

        public static bool InitializeCatalogs { get; set; }

        [System.Obsolete("Use ServiceManager.Get<AddressablesCatalogProvider>() directly instead. Make sure AddressablesCatalogProvider is registered to the ServiceManager.")]
        public static AddressablesCatalogProvider Instance => ServiceManager.Get<AddressablesCatalogProvider>();

        public string BaseUrl
        {
            get
            {
                //return "https://genies-content.s3.us-west-2.amazonaws.com/1711034229"; // for testing last Unity 2020 looks content build
                var url = "";

                if (AbTestingService != null)
                {
                    url = AbTestingService.GetFromConfig(AbTestingCloudFrontUrlConfigName, AbTestingCloudFrontUrlKeyName, "");
                }

                if (GeniesApiConfigManager.TargetEnvironment != BackendEnvironment.Prod)
                {
                    if (_staticBaseUrl != null)
                    {
                        url = _staticBaseUrl;
                    }

                    if (_isOverride && !string.IsNullOrEmpty(_overrideContentUrl))
                    {
                        url = _overrideContentUrl;
                    }
                }

                return string.IsNullOrEmpty(url) ? CloudfrontUrl : url;
            }
        }

        public string DynExtThingsUrl => DynamicContentExternalThingsUrl;

        public string DynExtGearUrl => DynamicContentExternalGearUrl;

        public AddressablesCatalogConfig StaticConfig =>
            new()
            {
                CatalogBaseUrl = BaseUrl,
                ContentType = AddressablesCatalogContentType.Static,
                ExcludeContentTypes = Array.Empty<string>(),
            };

        public AddressablesCatalogConfig DynamicConfig =>
            new()
            {
                CatalogBaseUrl = BaseUrl,
                ContentType = AddressablesCatalogContentType.Dynamic,
                ExcludeContentTypes = new[]{"shaders" },
            };

        public AddressablesCatalogConfig UniversalConfig =>
            new()
            {
                CatalogBaseUrl = BaseUrl,
                ContentType = AddressablesCatalogContentType.Dynamic,
                ExcludeContentTypes = new[]{"shaders", "wardrobegear", "flair", "things", "imagelibrary", }, // only leave animations
            };

        public AddressablesCatalogConfig ExternalThingsDynamicConfig =>
            new()
            {
                CatalogBaseUrl = DynamicContentExternalThingsUrl,
                ContentType = AddressablesCatalogContentType.DynamicExternal,
                ExcludeContentTypes = Array.Empty<string>(),
            };

        public AddressablesCatalogConfig ExternalGearDynamicConfig =>
            new()
            {
                CatalogBaseUrl = DynamicContentExternalGearUrl,
                ContentType = AddressablesCatalogContentType.DynamicExternal,
                ExcludeContentTypes = Array.Empty<string>(),
            };

        public AddressablesCatalogConfig ExternalSubSpeciesDynamicConfig =>
            new()
            {
                CatalogBaseUrl = DynamicContentExternalSubSpeciesUrl,
                ContentType = AddressablesCatalogContentType.DynamicExternal,
                ExcludeContentTypes = Array.Empty<string>(),
            };

        public AddressablesCatalogConfig ExternalModelLibraryDynamicConfig =>
            new()
            {
                CatalogBaseUrl = DynamicContentExternalModelLibraryUrl,
                ContentType = AddressablesCatalogContentType.DynamicExternal,
                ExcludeContentTypes = Array.Empty<string>(),
            };

        public AddressablesCatalogConfig ExternalShadersDynamicConfig =>
            new()
            {
                CatalogBaseUrl = DynamicContentExternalShadersUrl,
                ContentType = AddressablesCatalogContentType.DynamicExternal,
                ExcludeContentTypes = Array.Empty<string>(),
            };

        public AddressablesCatalogProvider(IContentOverrideService contentOverrideService)
        {
            //_contentOverrideService = contentOverrideService;
        }

        public AddressablesCatalogProvider()
        {
        }

        public static string CloudFrontUrl(Environment environment)
        {
            switch (environment)
            {
                case Environment.Prod:
                    return ProdContentUrl;
                case Environment.Qa:
                    return QaContentUrl;
                case Environment.Latest:
                    return LatestContentUrl;
            }

            return CloudfrontUrl;
        }

        public void SetStaticBaseUrl(string baseUrl)
        {
            _staticBaseUrl = baseUrl;
        }

        public static void SetOverrideContentUrl(bool isOverride, string mode, string overrideContentTimestamp)
        {
            switch (mode)
            {
                case "None":
                    _overrideContentUrl = ProdContentUrl;
                    break;
                case "Prod":
                    _overrideContentUrl = ProdContentUrl;
                    break;
                case "QA":
                    _overrideContentUrl = QaContentUrl;
                    break;
                case "Latest":
                    _overrideContentUrl = LatestContentUrl;
                    break;
                case "Specify":
                    _overrideContentUrl = string.Format(SpecifyBucketUrl, overrideContentTimestamp);;
                    break;
            }

            _isOverride = isOverride;
        }

        public static UniTask InitializeAddressablesAndCatalogsAsync(
            Environment contentEnvironment = Environment.Current,
            AddressablesCatalogService.App contentApplication = AddressablesCatalogService.App.GeniesParty,
            bool overrideContent = false,
            string contentOverrideUrl = null
        )
        {
            if (!InitializeCatalogs)
            {
                return UniTask.CompletedTask;
            }

            var service = ServiceManager.Get<AddressablesCatalogService>();
            Assert.IsNotNull(service,
                $"{nameof(AddressablesCatalogService)} expected to be registered to {nameof(ServiceManager)} but was not.)");
            var provider = ServiceManager.Get<AddressablesCatalogProvider>();
            Assert.IsNotNull(provider,
                $"{nameof(AddressablesCatalogProvider)} expected to be registered to {nameof(ServiceManager)} but was not.)");
            provider.SetStaticBaseUrl(overrideContent ? contentOverrideUrl : CloudFrontUrl(contentEnvironment));

            var tasks = new List<UniTask>()
            {
                service.InitializeCatalogs(provider, contentApplication),
            };

            return UniTask.WhenAll(tasks.ToArray());
        }
    }
}
