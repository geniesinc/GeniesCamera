using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.ServiceManagement;
using Genies.FeatureFlags;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Genies.Addressables
{
    public sealed class AddressablesCatalogService
    {
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();
        public bool AreCatalogsLoaded { get; private set; }
        public string LoadedCatalogCount { get; private set; }

        private const int _retryAttempts = 100; //Nothing to loose!
        private const string _errorCodeConnectionIssue = "-1";
        private readonly string _catalogBaseUri;
        private readonly List<string> _contentTypes;
        private readonly Dictionary<string, BuildInfo> _contentTypeBuildInfos;
        private ConcurrentDictionary<string, BuildInfo> _uriToBuildInfo = new();

        private bool _catalogSettingsLoaded;

        private UniTaskCompletionSource _loadOperation;
        private UniTaskCompletionSource _loadExternalOperation;

        private readonly HttpClient _httpClient = new HttpClient();

        public enum App
        {
            GeniesParty,
            Experiences,
            ContentGenV2,
        }

        public AddressablesCatalogService()
        {
            _contentTypeBuildInfos = new Dictionary<string, BuildInfo>();
            AreCatalogsLoaded = false;
        }

        [System.Obsolete("Use ServiceManager.Get<AddressablesCatalogService>() directly instead. Make sure AddressablesCatalogService is registered to the ServiceManager.")]
        public static AddressablesCatalogService Instance => ServiceManager.Get<AddressablesCatalogService>();

        public UniTask InitializeCatalogs(AddressablesCatalogProvider catalogProvider, App application)
        {
            switch (application)
            {
                case App.GeniesParty:
                    {
                        var addressablesCatalogConfigs = new List<AddressablesCatalogConfig>()
                        {
                            catalogProvider.UniversalConfig,
                        };

                        if (!(_FeatureFlagsManager?.IsFeatureEnabled(SharedFeatureFlags.AddressablesInventoryLocations) ?? false))
                        {
                            addressablesCatalogConfigs.AddRange(new[]
                            {
                                catalogProvider.ExternalShadersDynamicConfig,
                                catalogProvider.ExternalGearDynamicConfig,
                                catalogProvider.ExternalSubSpeciesDynamicConfig,
                                catalogProvider.ExternalModelLibraryDynamicConfig,
                            });
                        }

                        if (_FeatureFlagsManager != null)
                        {
                            if (_FeatureFlagsManager.IsFeatureEnabled(SharedFeatureFlags.ExternalThingsContent))
                            {
                                addressablesCatalogConfigs.Add(catalogProvider.ExternalThingsDynamicConfig);
                            }
                        }

                        // load Genies Party Content Catalogs
                        return LoadCatalogsAsync(catalogProvider.BaseUrl, BaseAddressableProvider.DynBaseUrl,
                            addressablesCatalogConfigs);
                    }
                case App.Experiences:
                    {
                        var addressablesCatalogConfigs = new List<AddressablesCatalogConfig>()
                        {
                            catalogProvider.ExternalThingsDynamicConfig,
                            catalogProvider.ExternalShadersDynamicConfig,
                        };

                        // load SDK content catalogs
                        return LoadCatalogsAsync(catalogProvider.BaseUrl, BaseAddressableProvider.DynBaseUrl,
                            addressablesCatalogConfigs);
                    }
                case App.ContentGenV2:
                {
                    var addressablesCatalogConfigs = new List<AddressablesCatalogConfig>()
                    {
                        catalogProvider.StaticConfig,
                        catalogProvider.DynamicConfig,
                        catalogProvider.ExternalShadersDynamicConfig,
                    };
                    // load Content Gen content catalogs
                    return LoadCatalogsAsync(catalogProvider.BaseUrl, BaseAddressableProvider.DynBaseUrl,
                        addressablesCatalogConfigs);
                }
                default:
                    if (_FeatureFlagsManager != null)
                    {
                        if (_FeatureFlagsManager.IsFeatureEnabled(SharedFeatureFlags.DynamicContent))
                        {
                            // return default (v3)
                            return LoadCatalogsAsync(catalogProvider.BaseUrl, catalogProvider.DynContentTypes, BaseAddressableProvider.DynBaseUrl);
                        }
                    }
                    // return default (v2)
                    return LoadCatalogsAsync(catalogProvider.BaseUrl, catalogProvider.ContentTypes, BaseAddressableProvider.DynBaseUrl);
            }
        }

        public async UniTask LoadCatalogAsync(string catalogUri, string contentType = null, string dynBaseUrl = "")
        {
            AddressablesRuntimeProperties.ClearCachedPropertyValues();
            var baseUri = catalogUri.Substring(0, catalogUri.LastIndexOf('/'));
            SetAddressablesRuntimeProperties(baseUri, dynBaseUrl);
            AsyncOperationHandle<IResourceLocator> handle =
                UnityEngine.AddressableAssets.Addressables.LoadContentCatalogAsync(catalogUri, true, contentType);
            await UniTask.WhenAll(handle.ToUniTask());
        }

        public async UniTask LoadCatalogsAsync(string catalogBaseUrl, IEnumerable<string> contentTypes, string dynBaseUrl = "")
        {
            var allContentTypes = Enum.GetValues(typeof(AddressablesCatalogContentType))
                .Cast<AddressablesCatalogContentType>()
                .Where(enumValue => contentTypes.Contains(enumValue.ToString().ToLower()))
                .ToList();

            if (_loadOperation is not null)
            {
                await _loadOperation.Task;
                return;
            }

            SetAddressablesRuntimeProperties(catalogBaseUrl, dynBaseUrl);

            _loadOperation = new UniTaskCompletionSource();
            var platform = BaseAddressablesService.GetPlatformString();

            _catalogInfos = new List<CatalogInfo>(allContentTypes.Count);

            var catalogTasks = new List<UniTask>();
            foreach (var contentType in allContentTypes)
            {
                catalogTasks.Add( GetOrFetchCatalogInfosAsync(catalogBaseUrl, platform, contentType, Array.Empty<string>()));
            }

            //fill the _catalogInfos
            await UniTask.WhenAll(catalogTasks);
            await UniTask.WhenAll(_catalogInfos.Select(LoadContentCatalogAsync));

            LoadedCatalogCount = _catalogInfos.Count.ToString();

            AreCatalogsLoaded = true;
            _loadOperation.TrySetResult();
        }

        private List<CatalogInfo> _catalogInfos;

        /// <summary>
        /// Load Catalogs Async Overload - This is the new loader that can load external catalogs
        /// </summary>
        /// <param name="staticBaseUrl"></param>
        /// <param name="dynBaseUrl"></param>
        /// <param name="addressablesCatalogConfigs"></param>
        public async UniTask LoadCatalogsAsync(string staticBaseUrl, string dynBaseUrl, List<AddressablesCatalogConfig> addressablesCatalogConfigs)
        {
            if (_loadOperation is not null)
            {
                await _loadOperation.Task;
                return;
            }

            SetAddressablesRuntimeProperties(staticBaseUrl, dynBaseUrl);

            _loadOperation = new UniTaskCompletionSource();
            var platform = BaseAddressablesService.GetPlatformString();
            _catalogInfos = new List<CatalogInfo>(addressablesCatalogConfigs.Count);

            var catalogTasks = new List<UniTask>();
            foreach (var addressablesCatalogConfig in addressablesCatalogConfigs)
            {
                catalogTasks.Add( GetOrFetchCatalogInfosAsync(addressablesCatalogConfig.CatalogBaseUrl, platform, addressablesCatalogConfig.ContentType, addressablesCatalogConfig.ExcludeContentTypes));
            }

            //fill the _catalogInfos
            await UniTask.WhenAll(catalogTasks);
            await UniTask.WhenAll(_catalogInfos.Select(LoadContentCatalogAsync));

            LoadedCatalogCount = _catalogInfos.Count.ToString();

            AreCatalogsLoaded = true;
            _loadOperation.TrySetResult();
        }

        private async UniTask GetOrFetchCatalogInfosAsync(string baseUrl,
            string platform,
            AddressablesCatalogContentType contentType,
            string[] excludeContentTypes)
        {
            var catalog = await GetOrFetchCatalogInfos(baseUrl, platform, contentType, excludeContentTypes);
            _catalogInfos.AddRange(catalog);
        }

        private async UniTask<List<CatalogInfo>> GetOrFetchCatalogInfos(
            string baseUri,
            string platform,
            AddressablesCatalogContentType contentType,
            string[] excludeContentTypes)
        {
            var catalogInfos = new List<CatalogInfo>();
            var buildInfoUri = $"{baseUri}/{platform}/{contentType.ToLowercaseString()}/build-info.json";
            var catalogUri = $"{baseUri}/{platform}/{contentType.ToLowercaseString()}/catalog_genies.json";

            switch (contentType)
            {
                case AddressablesCatalogContentType.Dynamic:
                    try
                    {
                        BuildInfo buildInfo = await GetBuildInfoFrmUri(buildInfoUri, _retryAttempts);
                        List<string> filenames = buildInfo.catalogFileNames;
                        foreach (var catalogName in filenames)
                        {
                            catalogUri = $"{baseUri}/{platform}/{contentType.ToLowercaseString()}/{catalogName}";
                            var catalogNameSplit = catalogName.Split('_');
                            var contentTypeName = catalogNameSplit.Length > 1 ? catalogNameSplit[1] : string.Empty;
                            var dynContentType = $"{contentType.ToLowercaseString()}_{contentTypeName}"; // dynamic_wardrobestatic

                            if (!excludeContentTypes.Contains(contentTypeName))
                            {
                                catalogInfos.Add(new CatalogInfo(catalogUri, buildInfoUri, dynContentType, excludeContentTypes));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CrashReporter.LogHandledException(e);
                    }
                    break;

                case AddressablesCatalogContentType.DynamicExternal:
                    try
                    {
                        buildInfoUri = $"{baseUri}/{platform}/build-info.json";
                        BuildInfo buildInfo = await GetBuildInfoFrmUri(buildInfoUri, _retryAttempts);
                        catalogUri = $"{baseUri}/{platform}/{buildInfo.catalogFileName}";
                        var exCatalogNameSplit = buildInfo.catalogFileName.Split('_');
                        var exContentTypeName = exCatalogNameSplit.Length > 1 ? exCatalogNameSplit[1] : string.Empty;
                        var exDynContentType = $"{contentType.ToLowercaseString()}_{exContentTypeName}";

                        catalogInfos.Add(new CatalogInfo(catalogUri, buildInfoUri, exDynContentType, excludeContentTypes));
                    }
                    catch (Exception e)
                    {
                        CrashReporter.LogHandledException(e);
                    }
                    break;

                default:
                    try
                    {
                        catalogInfos.Add(new CatalogInfo(catalogUri, buildInfoUri, contentType.ToLowercaseString(), excludeContentTypes));
                    }
                    catch (Exception e)
                    {
                        CrashReporter.LogHandledException(e);
                    }
                    break;
            }

            return catalogInfos;
        }

        private async UniTask LoadContentCatalogAsync(CatalogInfo catalog)
        {
            AsyncOperationHandle<IResourceLocator> handle =
                UnityEngine.AddressableAssets.Addressables.LoadContentCatalogAsync(catalog.catalogUri, true, catalog.contentType);

            await UniTask.WhenAll(
                handle.ToUniTask()
            );

            //after loading the content catalog, we can call the build info async and forget it, so we can avoid get stuck the
            //process of downloading the build info
            LoadBuildInfoAsync(catalog.buildInfoUri, catalog.contentType).Forget();
        }

        private void SetAddressablesRuntimeProperties(string staticBaseUrl, string dynBaseUrl)
        {
            if (_catalogSettingsLoaded)
            {
                return;
            }

            _catalogSettingsLoaded = true;
            AddressablesRuntimeProperties.ClearCachedPropertyValues();
            AddressablesRuntimeProperties.SetPropertyValue("BaseUrl", staticBaseUrl);
            AddressablesRuntimeProperties.SetPropertyValue("DynBaseUrl", dynBaseUrl);
        }

        private async UniTask LoadBuildInfoAsync(string buildInfoUri, string contentType)
        {
            BuildInfo buildInfo = await GetBuildInfoFrmUri(buildInfoUri, _retryAttempts);
            _contentTypeBuildInfos.Add(contentType, buildInfo);
        }

        public string TryGetContentVersion(string contentType)
        {
            BuildInfo outBuildInfo = new BuildInfo();
            _contentTypeBuildInfos.TryGetValue(contentType, out outBuildInfo);
            return outBuildInfo.timestamp;
        }

        private async UniTask<BuildInfo> GetBuildInfoFrmUri(string buildInfoUri, int retryAttempts = 1)
        {
            //Return cached catalog in case we already have it
            if (_uriToBuildInfo.ContainsKey(buildInfoUri))
            {
                return _uriToBuildInfo[buildInfoUri];
            }

            RunExtractionOnTheThreadPool(buildInfoUri, retryAttempts);

            await UniTask.WaitUntil(() => _uriToBuildInfo.ContainsKey(buildInfoUri));

            if (_uriToBuildInfo[buildInfoUri].timestamp == _errorCodeConnectionIssue)
            {
                CrashReporter.LogError($"Could not load catalog build info from {buildInfoUri} due to internet connection issue.");
                return new BuildInfo();
            }

            var buildInfo = _uriToBuildInfo[buildInfoUri];
            return buildInfo;
        }

        private void RunExtractionOnTheThreadPool(string buildInfoUri, int retryAttempts = 1)
        {
            //Running operation on the thread pool
            //since creating new threads is expensive
            ThreadPool.QueueUserWorkItem(Extractor, new object[] {buildInfoUri, retryAttempts});
        }

        private async void Extractor(object state)
        {
            var array = state as object[];
            var buildInfoUri = Convert.ToString(array[0]);
            var retryAttempts = Convert.ToInt32(array[1]);

            var json = string.Empty;

            // Check if the URI is a local file path
            if (Uri.IsWellFormedUriString(buildInfoUri, UriKind.Absolute) && (buildInfoUri.StartsWith("http://") || buildInfoUri.StartsWith("https://")))
            {
                // Remote URL
                for (int i = 0; i < retryAttempts; i++)
                {
                    try
                    {
                        HttpResponseMessage response = await _httpClient.GetAsync(buildInfoUri);
                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                        }

                        if (!string.IsNullOrEmpty(json))
                        {
                            lock (_uriToBuildInfo)
                            {
                                _uriToBuildInfo[buildInfoUri] = BuildInfo.FromJson(json);
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CrashReporter.LeaveBreadcrumb($"Could not load catalog build info from {buildInfoUri}");
                        CrashReporter.LogHandledException(e);

                        if (retryAttempts > 0)
                        {
                            CrashReporter.LeaveBreadcrumb($"Retry attempt {i + 1}");
                            await UniTask.Delay(5000);
                        }
                    }
                }
            }
            else
            {
                // Local file path
                try
                {
                    json = await File.ReadAllTextAsync(buildInfoUri);
                    if (!string.IsNullOrEmpty(json))
                    {
                        lock (_uriToBuildInfo)
                        {
                            _uriToBuildInfo[buildInfoUri] = BuildInfo.FromJson(json);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    CrashReporter.LeaveBreadcrumb($"Could not load catalog build info from local file {buildInfoUri}");
                    CrashReporter.LogHandledException(e);
                }
            }

            _uriToBuildInfo[buildInfoUri] = new BuildInfo() { timestamp = _errorCodeConnectionIssue };
        }

        private struct BuildInfo
        {
            public string timestamp;
            public string catalogFileName;
            public List<string> catalogFileNames;

            public static BuildInfo FromJson(string json) => JsonUtility.FromJson<BuildInfo>(json);
        }

        public struct CatalogInfo
        {
            public string catalogUri;
            public string buildInfoUri;
            public string contentType;
            public string[] excludeCatalogs;

            public CatalogInfo(string catalogUri, string buildInfoUri, string contentType, string[] excludeCatalogs)
            {
                this.catalogUri = catalogUri;
                this.buildInfoUri = buildInfoUri;
                this.contentType = contentType;
                this.excludeCatalogs = excludeCatalogs;
            }
        }
    }
}
