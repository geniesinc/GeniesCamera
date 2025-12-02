using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Closet;
using Genies.DataRepositoryFramework.Caching;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;
using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using Exception = Genies.Services.Auth.Exception;

namespace Genies.Wearables
{
    /// <summary>
    /// Main implementation of the wearable service that handles wearable creation, retrieval, and management operations.
    /// This service integrates with the Genies API services to manage user-generated wearable content and caching.
    /// </summary>
    public sealed class WearableService : IWearableService
    {
        private string _userId;
        private readonly IClosetService _closetService;
        private readonly WearableApi _wearableApi = new WearableApi();
        private readonly WearableApi _thriftingWearableApi;
        private readonly DataRepositoryMemoryCache<Services.Model.Wearable> _memoryCachedWearables;
        private bool _didFetchFirstTime;

        private UniTaskCompletionSource _feedApiInitializationSource;
        private readonly WearableServiceApiPathResolver _apiPathResolver = new();

        /// <summary>
        /// Initializes a new instance of the WearableService class with a specific user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user for whom wearables are managed.</param>
        /// <param name="closetService">The closet service instance for managing user's wearable collection.</param>
        public WearableService(string userId, IClosetService closetService)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"[{nameof(WearableService)}] invalid null or empty user ID");
                return;
            }

            _userId = userId;
            _closetService = closetService;
            _memoryCachedWearables = new DataRepositoryMemoryCache<Services.Model.Wearable>(wearable => wearable.WearableId);
        }

        /// <summary>
        /// Initializes a new instance of the WearableService class for dependency injection.
        /// The user ID will be obtained from the authentication system after initialization.
        /// </summary>
        /// <param name="closetService">The closet service instance for managing user's wearable collection.</param>
        [Inject]
        public WearableService(IClosetService closetService)
        {
            // TODO: Create incorporate the repository framework in the Closet Service
            var config = new Configuration { BasePath = _apiPathResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment) };

            _userId = null;
            _closetService = closetService;

            // The reason we're separating the thrifting api calls with it's own base path is because they're partially
            // migrated in the backend https://geniesinc.slack.com/archives/C05KNECJN92/p1699463516296159?thread_ts=1699459201.110479&cid=C05KNECJN92
            _thriftingWearableApi = new WearableApi(config);
            _memoryCachedWearables = new DataRepositoryMemoryCache<Services.Model.Wearable>(wearable => wearable.WearableId);

            AwaitApiInitialization().Forget();
        }

        private async UniTask AwaitApiInitialization()
        {
            if (_feedApiInitializationSource != null)
            {
                await _feedApiInitializationSource.Task;
                return;
            }

            _feedApiInitializationSource = new UniTaskCompletionSource();
            //Await auth access token being set.
            await UniTask.WaitUntil(GeniesLoginSdk.IsUserSignedIn);

            _thriftingWearableApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            _feedApiInitializationSource.TrySetResult();
            _feedApiInitializationSource = null;
            _userId = await GeniesLoginSdk.GetUserIdAsync();
        }

        /// <inheritdoc />
        public UniTask<string> CreateWearableAsync(Ugc.Wearable wearable, byte[] icon, string wearableId = null, bool isThriftable = true)
        {
            string wearableJson = JsonConvert.SerializeObject(wearable);
            return CreateWearableAsync(wearable.TemplateId, wearableId, wearableJson, icon, isThriftable);
        }

        /// <summary>
        /// Creates a new wearable asynchronously with the specified template, JSON data, and icon.
        /// </summary>
        /// <param name="templateId">The template identifier for the wearable type.</param>
        /// <param name="wearableId">Optional existing wearable ID for updates, null for new creation.</param>
        /// <param name="wearableJson">The wearable configuration data as JSON string.</param>
        /// <param name="icon">The icon image data as a byte array.</param>
        /// <param name="isThriftable">Whether the wearable should be available for thrifting by other users.</param>
        /// <returns>A task that completes with the wearable ID of the created or updated wearable.</returns>
        public async UniTask<string> CreateWearableAsync(string templateId, string wearableId, string wearableJson, byte[] icon, bool isThriftable)
        {
            string fullAssetName = null;
            string backendAction;
            if (string.IsNullOrEmpty(wearableId))
            {
                backendAction = "Created";
            }
            else
            {
                backendAction = "Updated";

                //The backend is not taking the last underscore, since we use `_template` it updates the wearable with `template` as the id...
                var noUnderscore = templateId.Replace("_", "-");
                fullAssetName = $"{noUnderscore}_{wearableId}";
            }

            const int secondsToWait = 30;

            try
            {
                if (string.IsNullOrEmpty(_userId) )
                {
                    _userId = await GeniesLoginSdk.GetUserIdAsync();
                }

                var body = new WearableCreate(
                                              null,
                                              fullAssetName,
                                              templateId,
                                              GetCategoryFromId(templateId),
                                              null,
                                              icon,
                                              null,
                                              wearableJson,
                                              _userId,
                                              isThriftable
                                             );
                var requestWearable = _wearableApi.CreateWearableAsync(body).AsUniTask();
                var resultWearable  = await requestWearable.TimeoutWithoutException(TimeSpan.FromSeconds(secondsToWait));
                var result          = resultWearable.Result;

                if (result == null)
                {
                    return null;
                }

                //Cache for next fetch
                _memoryCachedWearables.CacheRecord(resultWearable.Result);
                wearableId = result.WearableId;
                Debug.Log($"{backendAction} the asset = " + result.WearableId);

                await _closetService.AddWearableToCloset(result.WearableId, result.FullAssetName);

                return wearableId;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        /// <inheritdoc />
        public async UniTask<Services.Model.Wearable> GetWearableByIdAsync(string id)
        {
            if (_memoryCachedWearables.TryGetRecord(id, out Services.Model.Wearable wearable))
            {
                return wearable;
            }

            var list = ListPool<string>.Get();
            list.Add(id);
            List<Services.Model.Wearable> results = await GetWearablesByIdsAsync(list);

            //Cache results
            _memoryCachedWearables.BatchCacheRecords(results);

            ListPool<string>.Release(list);

            if (results is null || results.Count == 0)
            {
                return null;
            }

            return results[0];
        }

        /// <inheritdoc />
        public async UniTask<List<Services.Model.Wearable>> GetWearablesByIdsAsync(List<string> ids)
        {
            if (_didFetchFirstTime)
            {
                var wearables = _memoryCachedWearables.GetRecords();
                var matchingItems = wearables.Where(w => ids.Contains(w.WearableId)).ToList();

                if(matchingItems.Count > 0)
                {
                    return matchingItems;
                }
            }

            var results = await ApiUtils.PaginateRequest(ids, _wearableApi.GetWearablesByIdAsync);
            _memoryCachedWearables.BatchCacheRecords(results);
            return results;
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            _didFetchFirstTime = false;
            _memoryCachedWearables.Clear();
        }

        /// <inheritdoc />
        public async UniTask<List<Services.Model.Wearable>> GetAllOwnedWearablesAsync()
        {
            if (_didFetchFirstTime)
            {
                return _memoryCachedWearables.GetRecords();
            }

            var ids     = await GetAllOwnedWearableIds();
            var results = await ApiUtils.PaginateRequest(ids, _wearableApi.GetWearablesByIdAsync);
            _memoryCachedWearables.BatchCacheRecords(results);
            _didFetchFirstTime = true;
            return results;
        }

        /// <inheritdoc />
        public async UniTask<List<string>> GetAllOwnedWearableIds()
        {
            var response = await _closetService.GetClosetItems();
            return response.Wearables.Select(w => w.WearableId).ToList();
        }

        /// <inheritdoc />
        public async UniTask<WearableThriftList> GetThriftableWearbles(string userId)
        {
            await AwaitApiInitialization();

            try
            {
                var result = await _thriftingWearableApi.GetWearablesAsync(userId);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return null;
        }

        private string GetCategoryFromId(string id)
        {
            var splitName = id.Split('-');
            return splitName.Length != 0 ? splitName[0] : null;
        }
    }
}
