using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;
using Genies.Utilities;
using UnityEngine;
using VContainer;
using Exception = Genies.Services.Auth.Exception;

namespace Genies.Closet
{
    /// <summary>
    /// Default implementation of <see cref="IClosetService"/> that provides closet management functionality through remote APIs.
    /// This service handles user closet operations including NFT management, wearables, and collectible items with caching support.
    /// </summary>
    public sealed class ClosetService : IClosetService
    {
        private string _userId;
        private readonly IClosetApi _closetApi;
        private readonly IRuntimeCache _runtimeCache;
        private const int _secondsToSaveValidCache = 10;
        private UniTaskCompletionSource _feedApiInitializationSource;
        private readonly ClosetApiPathResolver _apiPathResolver = new();

        /// <summary>
        /// Defines the types of items that can be stored in a closet.
        /// </summary>
        private enum ItemType
        {
            /// <summary>
            /// Invalid or unknown item type.
            /// </summary>
            Invalid = 0,

            /// <summary>
            /// Wearable items such as clothing and accessories.
            /// </summary>
            Wearable = 1,

            /// <summary>
            /// Collectible things with protocol associations.
            /// </summary>
            Things = 2,

            /// <summary>
            /// Non-fungible token (NFT) items.
            /// </summary>
            Nft = 3,

            /// <summary>
            /// Combined NFT and wearable items.
            /// </summary>
            NftandWearables = 4
        }


        /// <summary>
        /// Initializes a new instance of the ClosetService class with a specific user ID.
        /// This constructor is obsolete and should not be used for new implementations.
        /// </summary>
        /// <param name="userId">The user ID for which to manage the closet.</param>
        [Obsolete]
        public ClosetService(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"[{nameof(ClosetService)}] invalid null or empty user ID");
                return;
            }
            _userId = userId;
            _closetApi = new ClosetApi();
            _runtimeCache = new RuntimeCache();
        }

        /// <summary>
        /// Initializes a new instance of the ClosetService class using dependency injection.
        /// This constructor automatically configures the API client and starts initialization.
        /// </summary>
        [Inject]
        public ClosetService()
        {
            // TODO: Create incorporate the repository framework in the Closet Service
            var config = new Configuration { BasePath = _apiPathResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment) };
            _userId = null;
            _closetApi = new ClosetApi(config);
            _runtimeCache = new RuntimeCache();

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

            _closetApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            _feedApiInitializationSource.TrySetResult();
            _feedApiInitializationSource = null;
            _userId = await GeniesLoginSdk.GetUserIdAsync();
        }

        /// <inheritdoc cref="IClosetService.GetOwnedNftGuids"/>
        public async UniTask<List<string>> GetOwnedNftGuids()
        {
            await AwaitApiInitialization();

            var items = await GetClosetItems();
            return items.Nfts.Select(n => n.Guid).ToList();
        }

        /// <inheritdoc cref="IClosetService.GetOwnedNftInfo"/>
        public async UniTask<List<IClosetService.NftInfo>> GetOwnedNftInfo()
        {
            await AwaitApiInitialization();

            var items = await GetClosetItems();
            return items.Nfts.Select(n => new IClosetService.NftInfo()
            {
                Guid = n.Guid,
                Id = ToNullableInt(n.NftId)
            }).ToList();
        }

        /// <inheritdoc cref="IClosetService.GetOwnedUnlockablesInfo"/>
        public async UniTask<List<IClosetService.UnlockablesInfo>> GetOwnedUnlockablesInfo()
        {
            await AwaitApiInitialization();

            var items = await GetClosetItems();

            return items.Nfts.
                FindAll(n => n.NftId == "REWARD").
                Select(n => new IClosetService.UnlockablesInfo()
                {
                    Guid = n.Guid,
                    Id = n.NftId
                })
                .ToList();
        }

        /// <summary>
        /// Utility method to safely convert a string to a nullable integer.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The parsed integer if successful, otherwise null.</returns>
        private static int? ToNullableInt(string str)
        {
            var i = 0;
            if (int.TryParse(str, out i))
            {
                return i;
            }

            return null;
        }

        /// <inheritdoc cref="IClosetService.GetClosetItems"/>
        public async UniTask<ClosetItemResponse> GetClosetItems()
        {
            await AwaitApiInitialization();

            ClosetItemResponse result = null;

            try
            {
                if (string.IsNullOrEmpty(_userId))
                {
                    _userId = await GeniesLoginSdk.GetUserIdAsync();
                }

                if (_runtimeCache.TryGetValidRecord(_userId, out ClosetItemResponse cacheResponse))
                {
                    return cacheResponse;
                }

                result = await _closetApi.GetClosetAsync(_userId);

                // checking again for race condition
                if (_runtimeCache.TryGetValidRecord(_userId, out ClosetItemResponse processResponse))
                {
                    return processResponse;
                }

                _runtimeCache.RegisterRecord(_userId, result, _secondsToSaveValidCache);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return result;
        }

        /// <inheritdoc cref="IClosetService.AddWearableToCloset(string, string)"/>
        public async UniTask AddWearableToCloset(string wearableId, string fullAssetName)
        {
            await AwaitApiInitialization();

            await AddWearableToCloset(wearableId, fullAssetName, _userId);
        }

        /// <inheritdoc cref="IClosetService.AddWearableToCloset(string, string, string)"/>
        public async UniTask AddWearableToCloset(string wearableId, string fullAssetName, string createdBy)
        {
            var closeItem        = new ClosetItem(null, new ClosetItemWearable(wearableId , fullAssetName, createdBy));
            var updateUpdates    = new ClosetUpdateClosetUpdates(ClosetUpdateClosetUpdates.OpEnum.Deposit, closeItem);
            var closetUpdateBody = new ClosetUpdate(new List<ClosetUpdateClosetUpdates>(){updateUpdates});

            if (string.IsNullOrEmpty(_userId))
            {
                _userId = await GeniesLoginSdk.GetUserIdAsync();
            }

            var closetResult = await _closetApi.UpdateClosetAsync(closetUpdateBody, _userId);
            Debug.Log(closetResult);

            if (closetResult != null)
            {
                //clear runtime cache to get fresh data
                _runtimeCache.ClearRecords();
            }
        }

        /// <inheritdoc cref="IClosetService.RemoveWearableFromCloset"/>
        public async UniTask RemoveWearableFromCloset(string wearableId, string fullAssetName, string createdBy)
        {
            await AwaitApiInitialization();

            var closeItem = new ClosetItem(null, new ClosetItemWearable(wearableId, fullAssetName, createdBy));
            var updateUpdates = new ClosetUpdateClosetUpdates(ClosetUpdateClosetUpdates.OpEnum.Withdraw, closeItem);
            var closetUpdateBody = new ClosetUpdate(new List<ClosetUpdateClosetUpdates>() { updateUpdates });

            if (string.IsNullOrEmpty(_userId))
            {
                _userId = await GeniesLoginSdk.GetUserIdAsync();
            }
            var closetResult = await _closetApi.UpdateClosetAsync(closetUpdateBody, _userId);
            Debug.Log(closetResult);

            if (closetResult != null)
            {
                //clear runtime cache to get fresh data
                _runtimeCache.ClearRecords();
            }
        }

        /// <inheritdoc cref="IClosetService.AddThingToCloset"/>
        public async UniTask AddThingToCloset(string thingId)
        {
            await AwaitApiInitialization();

            var closeItem        = new ClosetItem(null, null, new ClosetItemThings(thingId));
            var updateUpdates    = new ClosetUpdateClosetUpdates(ClosetUpdateClosetUpdates.OpEnum.Deposit, closeItem);
            var closetUpdateBody = new ClosetUpdate(new List<ClosetUpdateClosetUpdates>(){updateUpdates});

            if (string.IsNullOrEmpty(_userId))
            {
                _userId = await GeniesLoginSdk.GetUserIdAsync();
            }

            var closetResult = await _closetApi.UpdateClosetAsync(closetUpdateBody, _userId);

            if (closetResult != null)
            {
                //clear runtime cache to get fresh data
                _runtimeCache.ClearRecords();
            }
        }

        /// <inheritdoc cref="IClosetService.GetThingsByProtocol"/>
        public async UniTask<ClosetItemResponse> GetThingsByProtocol(List<string> protocolIds, string minSdk)
        {
            await AwaitApiInitialization();

            ClosetItemResponse result = null;

            try
            {
                if (string.IsNullOrEmpty(_userId))
                {
                    _userId = await GeniesLoginSdk.GetUserIdAsync();
                }

                if (_runtimeCache.TryGetValidRecord(_userId, out ClosetItemResponse cacheResponse))
                {
                    return cacheResponse;
                }

                var itemType = ((int)ItemType.Things).ToString();
                result = await _closetApi.GetClosetAsync(_userId, itemType, minSdk, protocolIds);

                // checking again for race condition
                if (_runtimeCache.TryGetValidRecord(_userId, out cacheResponse))
                {
                    return cacheResponse;
                }

                _runtimeCache.RegisterRecord(_userId, result, _secondsToSaveValidCache);
            }

            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return result;
        }
    }
}
