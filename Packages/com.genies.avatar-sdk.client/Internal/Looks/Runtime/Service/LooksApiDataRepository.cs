using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.Looks.Models;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;

namespace Genies.Looks.Service
{
    /// <summary>
    /// Repository that interfaces with the <see cref="LookApi"/> to create, update, retrieve and delete looks.
    /// This class implements <see cref="ILooksDataRepository"/> and provides remote API-based data operations for look management.
    /// It handles authentication, API initialization, pagination, and error handling for looks-related operations.
    /// </summary>
    public class LooksApiDataRepository : ILooksDataRepository
    {
        private readonly ILookApi _lookApi;
        private readonly List<string> _cachedList = new List<string>();

        private UniTaskCompletionSource _lookApiInitializationSource;
        private UniTaskCompletionSource<List<LookData>> _fetchCompletionSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="LooksApiDataRepository"/> class with the specified API path resolver.
        /// Sets up the Look API client with appropriate configuration and starts the initialization process.
        /// </summary>
        /// <param name="apiPathResolver">The API client path resolver for determining base URLs for different environments.</param>
        public LooksApiDataRepository(IApiClientPathResolver apiPathResolver)
        {
            //Looks api requires a specific configuration as opposed to previous composer apis. The path and authorization are both different.
            var config = new Configuration { BasePath = apiPathResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment) };
            _lookApi = new LookApi(config);

            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Awaits until authentication is ready and updates the access token for the look API.
        /// This method ensures that the API client has a valid access token before making any API calls.
        /// Uses a completion source to prevent multiple concurrent initialization attempts.
        /// </summary>
        private async UniTask AwaitApiInitialization()
        {
            if (_lookApiInitializationSource != null)
            {
                await _lookApiInitializationSource.Task;
                return;
            }

            _lookApiInitializationSource = new UniTaskCompletionSource();
            //Await auth access token being set.
            await UniTask.WaitUntil(GeniesLoginSdk.IsUserSignedIn);

            _lookApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            _lookApiInitializationSource.TrySetResult();
            _lookApiInitializationSource = null;
        }

        /// <inheritdoc />
        public async UniTask<int> GetCountAsync()
        {
            var records = await GetAllAsync();
            return records.Count;
        }

        /// <inheritdoc />
        public async UniTask<List<string>> GetIdsAsync()
        {
            var records = await GetAllAsync();
            return records.Select(r => r.Id).ToList();
        }

        /// <inheritdoc />
        public async UniTask<List<LookData>> GetAllAsync()
        {
            await AwaitApiInitialization();

            if (_fetchCompletionSource != null)
            {
                return await _fetchCompletionSource.Task;
            }

            var userId = await GeniesLoginSdk.GetUserIdAsync();
            var looks  = new List<LookData>();
            _fetchCompletionSource = new UniTaskCompletionSource<List<LookData>>();

            //Reusable method for adding new pages.
            void AddPage(LookListPagination page)
            {
                if (page?.Looks == null || page.Looks.Count == 0)
                {
                    return;
                }

                looks.AddRange(page.Looks.Select(l => (LookData)l));
            }

            var pagination = await _lookApi.GetLooksByAsync(userId);
            AddPage(pagination);

            while (!string.IsNullOrEmpty(pagination.NextCursor))
            {
                //Get next page
                pagination = await _lookApi.GetLooksByAsync(userId, pagination.NextCursor);

                //Add records from page
                AddPage(pagination);
            }

            _fetchCompletionSource.TrySetResult(looks);
            _fetchCompletionSource = null;

            return looks;
        }

        /// <inheritdoc />
        public async UniTask<LookData> GetByIdAsync(string recordId)
        {
            await AwaitApiInitialization();

            _cachedList.Clear();
            _cachedList.Add(recordId);
            var response = await _lookApi.GetLooksByIdAsync(_cachedList);

            return (LookData)response.Looks[0];
        }

        /// <inheritdoc />
        public async UniTask<LookData> CreateAsync(LookData data)
        {
            await AwaitApiInitialization();
            try
            {
                var createLook = new LookCreate(data.AnimationId ?? "",
                                                data.DollDefinition ?? "",
                                                data.SceneId ?? "",
                                                data.AvatarDefinition,
                                                thumbnail: data.ThumbnailUrl ?? "",
                                                videoClip: data.VideoUrl ?? "");

                var newLook = await _lookApi.CreateLookAsync(createLook);
                return (LookData)newLook;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return default;
        }

        /// <inheritdoc />
        public async UniTask<List<LookData>> BatchCreateAsync(List<LookData> data)
        {
            await AwaitApiInitialization();

            try
            {
                var createdLooks = await UniTask.WhenAll(data.Select(CreateAsync));
                return createdLooks.ToList();
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return null;
        }

        /// <inheritdoc />
        public async UniTask<LookData> UpdateAsync(LookData data)
        {
            await AwaitApiInitialization();
            try
            {
                var updateLook = new LookUpdate(data.AnimationId ?? "",
                                                data.DollDefinition ?? "",
                                                data.SceneId ?? "",
                                                data.AvatarDefinition,
                                                thumbnail: data.ThumbnailUrl ?? "",
                                                videoClip: data.VideoUrl ?? "");

                var newLook = await _lookApi.UpdateLookByIdAsync(updateLook, data.Id);
                return (LookData)newLook;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return default;
        }

        /// <inheritdoc />
        public async UniTask<List<LookData>> BatchUpdateAsync(List<LookData> data)
        {
            await AwaitApiInitialization();

            try
            {
                var updatedLooks = await UniTask.WhenAll(data.Select(UpdateAsync));
                return updatedLooks.ToList();
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return null;
        }

        /// <inheritdoc />
        public async UniTask<bool> DeleteAsync(string id)
        {
            await AwaitApiInitialization();
            try
            {
                await _lookApi.DeleteLookByIdAsync(id);
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return false;
        }

        /// <inheritdoc />
        public async UniTask<bool> BatchDeleteAsync(List<string> ids)
        {
            await AwaitApiInitialization();

            try
            {
                await UniTask.WhenAll(ids.Select(DeleteAsync));
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return false;
        }

        /// <inheritdoc />
        public async UniTask<bool> DeleteAllAsync()
        {
            await AwaitApiInitialization();

            try
            {
                var ids = await GetIdsAsync();
                await UniTask.WhenAll(ids.Select(DeleteAsync));
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return false;
        }
    }
}
