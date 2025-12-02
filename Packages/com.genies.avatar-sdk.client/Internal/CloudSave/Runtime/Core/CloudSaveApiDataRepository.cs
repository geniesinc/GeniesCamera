using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Genies.DataRepositoryFramework;
using Genies.DataRepositoryFramework.Caching;
using Genies.Login;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;
using Genies.Utilities;
using static Genies.CrashReporting.CrashReporter;

namespace Genies.CloudSave
{
    /// <summary>
    /// Data repository that handles CRUD api calls for the <see cref="GameFeatureApi"/>
    /// </summary>
    internal class CloudSaveApiDataRepository : IDataRepository<GameFeature>
    {
        public GameFeature.GameFeatureTypeEnum FeatureTypeEnum { get; }
        private readonly CloudFeatureResolver _cloudFeatureResolver = new();
        private readonly IGameFeatureApi _gameFeatureApi;
        private UniTaskCompletionSource _apiInitializationSource;
        private string _currentUserId;

        /// <summary>
        /// Due to a limitation in the API, we can't currently fetch per feature type, the api will return all the save data
        /// for all the features. To avoid redundant calls to the API we will handle the response as a singleton and cache it.
        /// </summary>
        private static Dictionary<GameFeature.GameFeatureTypeEnum, DataRepositoryMemoryCache<GameFeature>> _fetchedFeatures = new Dictionary<GameFeature.GameFeatureTypeEnum, DataRepositoryMemoryCache<GameFeature>>();

        private static UniTaskCompletionSource _gameFeatureFetchCsc;
        private static bool _didFetch = false;

        public CloudSaveApiDataRepository(GameFeature.GameFeatureTypeEnum featureTypeEnum)
        {
            FeatureTypeEnum = featureTypeEnum;
            var gameFeatureConfig = new Configuration()
            {
                BasePath = _cloudFeatureResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment),
            };
            _gameFeatureApi = new GameFeatureApi(gameFeatureConfig);
        }

        /// <summary>
        /// Awaits until auth is ready and updates the access token for the look api.
        /// </summary>
        private async UniTask AwaitApiInitialization()
        {
            if (_apiInitializationSource != null)
            {
                await _apiInitializationSource.Task;
                return;
            }

            _apiInitializationSource = new UniTaskCompletionSource();
            //Await auth access token being set.
            await UniTask.WaitUntil(GeniesLoginSdk.IsUserSignedIn);
            
            _gameFeatureApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;
            _apiInitializationSource.TrySetResult();
        }

        private async UniTask<bool> HasIdAsync(string id)
        {
            var currentIds = await GetIdsAsync();
            return currentIds.Contains(id);
        }


        private void EnsureCacheInitialized()
        {
            if (!_fetchedFeatures.ContainsKey(FeatureTypeEnum))
            {
                _fetchedFeatures[FeatureTypeEnum] = new DataRepositoryMemoryCache<GameFeature>(g => g.GameFeatureId);
            }
        }

        /// <summary>
        /// Gets the user id or throws an exception if one doesn't exist.
        /// If the user did change it will clear the data cache.
        /// </summary>
        private async UniTask CheckUpdateUserIdAndInvalidateCacheIfNeeded()
        {
            await AwaitApiInitialization();

            try
            {
                //Get user
                var loggedInUserId = await GeniesLoginSdk.GetUserIdAsync();

                //Invalidate cache if user changed
                if (string.IsNullOrEmpty(_currentUserId) || !_currentUserId.Equals(loggedInUserId))
                {
                    foreach (var kvp in _fetchedFeatures)
                    {
                        kvp.Value.Clear();
                    }

                    _didFetch = false;
                }

                //Update current user
                _currentUserId = loggedInUserId;
            }
            catch (Exception e)
            {
                throw new CloudSaveException("Failed to get user id, you can't use cloud save without a logged in user", e);
            }
        }

        public async UniTask<int> GetCountAsync()
        {
            await AwaitApiInitialization();

            //We've already done initial fetch.
            if (_didFetch && _fetchedFeatures.ContainsKey(FeatureTypeEnum))
            {
                return _fetchedFeatures[FeatureTypeEnum].GetRecordsCount();
            }

            var ids = await GetIdsAsync();
            return ids.Count;
        }

        /// <summary>
        /// Returns all the ids for the saved records.
        /// </summary>
        public async UniTask<List<string>> GetIdsAsync()
        {
            //Get save data
            var saves = await GetAllAsync();

            //Return save ids
            return saves.Select(s => s.GameFeatureId).ToList();
        }

        /// <summary>
        /// Returns all the json save data for a specific feature.
        /// </summary>
        public async UniTask<List<GameFeature>> GetAllAsync()
        {
            await AwaitApiInitialization();

            if (_gameFeatureFetchCsc != null)
            {
                //The fetched features will already have all the records from the api.
                await _gameFeatureFetchCsc.Task;
            }

            //We've already done initial fetch.
            await CheckUpdateUserIdAndInvalidateCacheIfNeeded();
            if (_didFetch && _fetchedFeatures.ContainsKey(FeatureTypeEnum))
            {
                return _fetchedFeatures[FeatureTypeEnum].GetRecords();
            }

            _gameFeatureFetchCsc = new UniTaskCompletionSource();

            //Ensure we initialize for all feature types
            var featureTypes = (GameFeature.GameFeatureTypeEnum[])Enum.GetValues(typeof(GameFeature.GameFeatureTypeEnum));
            foreach (var featureType in featureTypes)
            {
                if (!_fetchedFeatures.ContainsKey(featureType))
                {
                    _fetchedFeatures[featureType] = new DataRepositoryMemoryCache<GameFeature>(feature => feature.GameFeatureId);
                }
            }

            List<GameFeature> returnRecords = null;

            try
            {
                //Get save data for all features
                var allFeatures = await _gameFeatureApi.GetGameFeatureAsync(_currentUserId);

                if (allFeatures.Count > 0)
                {
                    foreach (var feature in allFeatures)
                    {
                        _fetchedFeatures[feature.GameFeatureType].CacheRecord(feature);
                    }

                    if (_fetchedFeatures.ContainsKey(FeatureTypeEnum))
                    {
                        returnRecords = _fetchedFeatures[FeatureTypeEnum].GetRecords();
                    }
                }
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException("Failed to fetch records", e));
            }

            returnRecords ??= new List<GameFeature>();
            _didFetch = true;
            if (_gameFeatureFetchCsc != null)
            {
                _gameFeatureFetchCsc.TrySetResult();
                _gameFeatureFetchCsc = null;
            }
            return returnRecords;
        }

        /// <summary>
        /// Returns a specific record
        /// </summary>
        public async UniTask<GameFeature> GetByIdAsync(string id)
        {
            await AwaitApiInitialization();
            EnsureCacheInitialized();

            try
            {
                //Fetch and update cache
                await GetAllAsync();

                //Get record from cache if it exists
                var cache = _fetchedFeatures[FeatureTypeEnum];
                cache.TryGetRecord(id, out var record);

                return record;
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to fetch record: {id}", e));
            }

            return default;
        }

        /// <summary>
        /// Creates a new cloud save for the specific feature.
        /// </summary>
        public async UniTask<GameFeature> CreateAsync(GameFeature data)
        {
            await AwaitApiInitialization();
            EnsureCacheInitialized();

            try
            {
                await CheckUpdateUserIdAndInvalidateCacheIfNeeded();

                var hasId = await HasIdAsync(data.GameFeatureId);
                if (hasId)
                {
                    return await UpdateAsync(data);
                }

                //Create new record
                await _gameFeatureApi.CreateGameFeatureAsync(new GameFeatureCreate(new List<GameFeature> { data }), _currentUserId);

                //Update the cache
                _fetchedFeatures[FeatureTypeEnum].CacheRecord(data);

                return data;
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to create record: {data.GameFeatureId}", e));
            }

            return default;
        }

        /// <summary>
        /// Creates records on the cloud
        /// </summary>
        public async UniTask<List<GameFeature>> BatchCreateAsync(List<GameFeature> newFeatures)
        {
            await AwaitApiInitialization();
            EnsureCacheInitialized();

            try
            {
                await CheckUpdateUserIdAndInvalidateCacheIfNeeded();


                var featuresToCreate = new List<GameFeature>();
                var featuresToUpdate = new List<GameFeature>();

                foreach (var feature in newFeatures)
                {
                    var hasId = await HasIdAsync(feature.GameFeatureId);
                    if (hasId)
                    {
                        featuresToUpdate.Add(feature);
                    }
                    else
                    {
                        featuresToCreate.Add(feature);
                    }
                }

                var apiRequests = new List<UniTask>();

                if (featuresToUpdate.Count > 0)
                {
                    apiRequests.Add(BatchUpdateAsync(featuresToUpdate));
                }

                if (featuresToCreate.Count > 0)
                {
                    apiRequests.Add(_gameFeatureApi.CreateGameFeatureAsync(new GameFeatureCreate(featuresToCreate), _currentUserId).AsUniTask());
                }

                //Create new records
                await UniTask.WhenAll(apiRequests);

                //Update the cache
                _fetchedFeatures[FeatureTypeEnum].BatchCacheRecords(featuresToCreate);

                //The server doesn't create anything new. The records will be the same.
                return newFeatures;
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to create records", e));
            }

            return default;
        }

        /// <summary>
        /// Update a record
        /// </summary>
        public async UniTask<GameFeature> UpdateAsync(GameFeature featureUpdate)
        {
            await AwaitApiInitialization();
            EnsureCacheInitialized();

            try
            {
                await CheckUpdateUserIdAndInvalidateCacheIfNeeded();

                //Create new record
                var updatedFeature = await _gameFeatureApi.UpdateGameFeatureAsync(new GameFeatureUpdate(featureUpdate), _currentUserId);

                //Update the cache
                _fetchedFeatures[FeatureTypeEnum].CacheRecord(featureUpdate);

                return updatedFeature;
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to update record: {featureUpdate.GameFeatureId}", e));
            }

            return default;
        }

        /// <summary>
        /// Batch update records
        /// </summary>
        public async UniTask<List<GameFeature>> BatchUpdateAsync(List<GameFeature> updatedRecords)
        {
            await AwaitApiInitialization();
            EnsureCacheInitialized();

            try
            {
                await CheckUpdateUserIdAndInvalidateCacheIfNeeded();

                //Batch update records
                var tasks   = updatedRecords.Select(r => _gameFeatureApi.UpdateGameFeatureAsync(new GameFeatureUpdate(r), _currentUserId).AsUniTask());
                var updates = await UniTask.WhenAll(tasks);

                //Update the cache
                _fetchedFeatures[FeatureTypeEnum].BatchCacheRecords(updatedRecords);

                return updates.ToList();
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException("Failed to update records", e));
            }

            return default;
        }

        public async UniTask<bool> DeleteAsync(string id)
        {
            await AwaitApiInitialization();

            try
            {
                await CheckUpdateUserIdAndInvalidateCacheIfNeeded();

                //Delete record
                await _gameFeatureApi.DeleteGameFeatureAsync(_currentUserId, id);

                //Update the cache
                _fetchedFeatures[FeatureTypeEnum].DeleteRecord(id);

                return true;
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException($"Failed to delete record {id}", e));
            }


            return false;
        }

        public async UniTask<bool> BatchDeleteAsync(List<string> ids)
        {
            EnsureCacheInitialized();

            try
            {
                await ApiUtils.PaginateRequest(ids, BatchDeleteRecordsPageAsync);
                return true;
            }
            catch (CloudSaveException cse)
            {
                LogHandledException(cse);
            }
            catch (Exception e)
            {
                LogHandledException(new CloudSaveException("Failed to update records", e));
            }

            return false;
        }

        public async UniTask<bool> DeleteAllAsync()
        {
            return await BatchDeleteAsync(await GetIdsAsync());
        }

        private async UniTask BatchDeleteRecordsPageAsync(List<string> recordsIds)
        {
            await AwaitApiInitialization();

            //No records to delete
            if (recordsIds.Count == 0)
            {
                return;
            }

            var concatenatedString = string.Join(",", recordsIds);
            try
            {
                await CheckUpdateUserIdAndInvalidateCacheIfNeeded();

                //Delete records
                await _gameFeatureApi.DeleteGameFeatureAsync(_currentUserId, concatenatedString);

                //Update the cache
                _fetchedFeatures[FeatureTypeEnum].BatchDeleteRecords(recordsIds);
            }
            catch (Exception e)
            {
                throw new CloudSaveException("Failed to batch delete records", e);
            }
        }
    }
}
