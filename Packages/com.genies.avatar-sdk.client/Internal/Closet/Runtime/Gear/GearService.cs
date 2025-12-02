using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.SDKServices.Api;
using Genies.SDKServices.Client;
using Genies.SDKServices.Model;
using Genies.Services.Configs;
using VContainer;

namespace Genies.Closet.Gear
{
    /// <summary>
    /// Default implementation of <see cref="IGearService"/> that provides gear management functionality through remote APIs.
    /// This service handles gear operations including creation, retrieval, updates, and provides proper error handling and authentication.
    /// </summary>
    public class GearService : IGearService
    {
        private readonly IGearApi _gearApi;
        private readonly ClosetApiPathResolver _apiPathResolve = new();
        private UniTaskCompletionSource _apiInitializationSource;

        /// <summary>
        /// Initializes a new instance of the GearService class using dependency injection.
        /// This constructor automatically configures the API client and starts initialization.
        /// </summary>
        [Inject]
        public GearService()
        {
            var config = new Configuration()
            {
                BasePath = _apiPathResolve.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment),
            };
            _gearApi = new GearApi(config);

            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Initializes a new instance of the GearService class with a specific gear API instance.
        /// This constructor is useful for testing or when using a custom API implementation.
        /// </summary>
        /// <param name="gearApi">The gear API implementation to use for gear operations.</param>
        public GearService(IGearApi gearApi)
        {
            _gearApi = gearApi;
            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Waits for the gear service to be fully initialized and ready for API calls.
        /// This includes authentication setup and API client configuration.
        /// </summary>
        /// <returns>A task that completes when the service is initialized.</returns>
        public virtual UniTask WaitUntilInitializedAsync()
        {
            if (_apiInitializationSource == null)
            {
                return UniTask.CompletedTask;
            }

            return _apiInitializationSource.Task;
        }

        private async UniTask AwaitApiInitialization()
        {
            if (_apiInitializationSource != null)
            {
                await _apiInitializationSource.Task;
                return;
            }

            _apiInitializationSource = new UniTaskCompletionSource();
            await UniTask.WaitUntil(GeniesLoginSdk.IsUserSignedIn);

            _gearApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            _apiInitializationSource.TrySetResult();
            _apiInitializationSource = null;
        }

        /// <inheritdoc cref="IGearService.GetGearListAsync"/>
        public async UniTask<GearListResponse> GetGearListAsync(string cursor = null, decimal? limit = null)
        {
            await WaitUntilInitializedAsync();

            try
            {
                return await _gearApi.GetGearListAsync(cursor, limit);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return null;
            }
        }

        /// <inheritdoc cref="IGearService.CreateGearAsync"/>
        public async UniTask<GearCreateResponse> CreateGearAsync(GearCreateRequest body)
        {
            await WaitUntilInitializedAsync();

            try
            {
                return await _gearApi.CreateGearAsync(body);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return null;
            }
        }

        /// <inheritdoc cref="IGearService.GetGearsByIdsAsync"/>
        public async UniTask<GearGetByIdsResponse> GetGearsByIdsAsync(List<string> gearIds)
        {
            await WaitUntilInitializedAsync();

            var ids = new List<Guid?>();
            foreach (var gearId in gearIds)
            {
                ids.Add(Guid.Parse(gearId));
            }

            try
            {
                return await _gearApi.GetGearsByIdsAsync(ids);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return null;
            }
        }

        /// <inheritdoc cref="IGearService.UpdateGearAsync"/>
        public async UniTask<MessageResponse> UpdateGearAsync(GearUpdateRequest body, string gearId)
        {
            await WaitUntilInitializedAsync();

            try
            {
                return await _gearApi.UpdateGearAsync(body, gearId);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return null;
            }
        }
    }
}
