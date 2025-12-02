using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory
{
    /// <summary>
    /// Service to get default assets from inventory, or assets scoped to an app or org.
    /// Eventually this will entirely replace the <see cref="InventoryService"/>
    /// but it is a separate service for now due to using a different API configuration
    /// </summary>
    public class DefaultInventoryService : IDefaultInventoryService
    {
        private IInventoryV2Api _inventoryApi;
        private UniTaskCompletionSource _apiInitializationSource;
        private const string _orgContext = "ALL", _appContext = "SDK_ALL";

        public event Action<string> AssetMinted;

        public DefaultInventoryService()
        {
            AwaitApiInitialization().Forget();
        }

        private async UniTask AwaitApiInitialization()
        {
            if (_apiInitializationSource != null)
            {
                await _apiInitializationSource.Task;
                return;
            }

            _apiInitializationSource = new UniTaskCompletionSource();

            _inventoryApi = new InventoryV2Api();
            await GeniesLoginSdk.WaitUntilLoggedInAsync();
            _inventoryApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            _apiInitializationSource.TrySetResult();
            _apiInitializationSource = null;
        }

        private class FetchState<TInventoryItem>
        {
            public List<TInventoryItem> Cache;
            public UniTaskCompletionSource<List<TInventoryItem>> CompletionSource;
            public bool IsTaskInFlight;
            public readonly object SyncLock = new();
            public string NextCursor;
            public List<string> CurrentCategories; // Store categories for LoadMore continuity
            public bool HasMoreData => !string.IsNullOrEmpty(NextCursor);

            public void ClearCache()
            {
                lock (SyncLock)
                {
                    Cache?.Clear();
                    Cache = null;
                    NextCursor = null;
                }
            }
        }

        /// <summary>
        /// Configuration object that defines how to fetch and map a specific inventory endpoint
        /// </summary>
        private class InventoryEndpointConfig<TResponse, TInventoryItem>
        {
            public FetchState<TInventoryItem> State { get; set; }
            public Func<int?, string, List<string>, Task<TResponse>> FetchFunc { get; set; }
            public Func<TResponse, List<TInventoryItem>> MapFunc { get; set; }
            public Func<TResponse, string> ExtractNextCursor { get; set; }
            public string ErrorContext { get; set; }
        }

        /// <summary>
        /// A generic method to call an endpoint and handle caching and concurrency.
        /// Used for calling all V2 Inventory endpoints
        /// </summary>
        /// <param name="config">Configuration that defines how to fetch and map this endpoint</param>
        /// <param name="limit">Number of items to fetch per page (null = no limit)</param>
        /// <param name="append">If true, append to existing cache; if false, replace cache</param>
        /// <param name="categories">Optional list of categories to filter by</param>
        /// <typeparam name="TResponse">The expected type to be returned from the fetch function</typeparam>
        /// <typeparam name="TInventoryItem">The type to map data from the fetch function into</typeparam>
        /// <returns>A list of the data mapped into <see cref="TInventoryItem"/> gathered from the fetch function</returns>
        private async UniTask<List<TInventoryItem>> FetchWithConfig<TResponse, TInventoryItem>(
            InventoryEndpointConfig<TResponse, TInventoryItem> config,
            int? limit = null,
            bool append = false,
            List<string> categories = null)
        {
            await AwaitApiInitialization();

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.State == null)
            {
                throw new NullReferenceException("The state of the config cannot be null");
            }

            var state = config.State;

            // Return cached data if we have it and not appending
            // Return a copy to prevent external modifications from affecting the cache
            if (!append && state.Cache?.Count > 0)
            {
                return new List<TInventoryItem>(state.Cache);
            }

            UniTaskCompletionSource<List<TInventoryItem>> completionSource;

            lock (state.SyncLock)
            {
                if (state.IsTaskInFlight || state.CompletionSource != null)
                {
                    completionSource = state.CompletionSource;
                }
                else
                {
                    state.IsTaskInFlight = true;
                    state.CompletionSource = new UniTaskCompletionSource<List<TInventoryItem>>();
                    completionSource = state.CompletionSource;

                    // Start the fetch operation asynchronously
                    FetchInternal().Forget();
                }
            }

            return await completionSource.Task;

            async UniTask FetchInternal()
            {
                try
                {
                    string cursor = append ? state.NextCursor : null;
                    List<string> categoriesToUse;

                    if (append)
                    {
                        // Use stored categories for LoadMore continuity
                        categoriesToUse = state.CurrentCategories;
                    }
                    else
                    {
                        // Store categories for future LoadMore calls
                        state.CurrentCategories = categories;
                        categoriesToUse = categories;
                    }

                    var response = await config.FetchFunc(limit, cursor, categoriesToUse);
                    if (response == null)
                    {
                        CrashReporter.LogError($"{config.ErrorContext}: response was null");
                        var emptyList = new List<TInventoryItem>();
                        if (!append)
                        {
                            state.Cache = emptyList;
                        }
                        state.CompletionSource.TrySetResult(state.Cache ?? emptyList);
                        return;
                    }

                    var items = config.MapFunc(response) ?? new List<TInventoryItem>();

                    // Extract and store the next cursor
                    if (config.ExtractNextCursor != null)
                    {
                        state.NextCursor = config.ExtractNextCursor(response);
                    }

                    if (append && state.Cache != null)
                    {
                        state.Cache.AddRange(items);
                        state.CompletionSource.TrySetResult(state.Cache);
                    }
                    else
                    {
                        state.Cache = items;
                        state.CompletionSource.TrySetResult(items);
                    }
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"{config.ErrorContext}: {ex.Message}");
                    var emptyList = new List<TInventoryItem>();
                    if (!append)
                    {
                        state.Cache = emptyList;
                    }
                    state.CompletionSource.TrySetResult(state.Cache ?? emptyList);
                }
                finally
                {
                    lock (state.SyncLock)
                    {
                        state.IsTaskInFlight = false;
                        state.CompletionSource = null;
                    }
                }
            }
        }

        private readonly FetchState<ColorTaggedInventoryAsset> _userWearablesState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _defaultWearablesState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _allWearablesState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _avatarState = new();
        private readonly FetchState<DefaultAvatarBaseAsset> _avatarBaseState = new();
        private readonly FetchState<DefaultAnimationLibraryAsset> _animationLibraryState = new();
        private readonly FetchState<ColoredInventoryAsset> _avatarEyesState = new();
        private readonly FetchState<DefaultInventoryAsset> _avatarFlairState = new();
        private readonly FetchState<DefaultInventoryAsset> _avatarMakeupState = new();
        private readonly Dictionary<string, FetchState<ColoredInventoryAsset>> _colorPresetsStatesByCategory = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _decorState = new();
        private readonly FetchState<DefaultInventoryAsset> _imageLibraryState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _modelLibraryState = new();

        private InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset> _userWearablesConfig;
        private InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset> _defaultWearablesConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarResponse, ColorTaggedInventoryAsset> _avatarConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarBaseResponse, DefaultAvatarBaseAsset> _avatarBaseConfig;
        private InventoryEndpointConfig<GetInventoryV2AnimationLibraryResponse, DefaultAnimationLibraryAsset> _animationLibraryConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarEyesResponse, ColoredInventoryAsset> _avatarEyesConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarFlairResponse, DefaultInventoryAsset> _avatarFlairConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarMakeupResponse, DefaultInventoryAsset> _avatarMakeupConfig;
        private InventoryEndpointConfig<GetInventoryV2ColorPresetsResponse, ColoredInventoryAsset> _colorPresetsConfig;
        private InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset> _decorConfig;
        private InventoryEndpointConfig<GetInventoryV2ImageLibraryResponse, DefaultInventoryAsset> _imageLibraryConfig;
        private InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset> _modelLibraryConfig;

        /// <summary>
        /// Initializes endpoint configurations with their respective mapping logic
        /// </summary>
        private void InitializeConfigurations()
        {
            _defaultWearablesConfig ??= new InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset>
            {
                State = _defaultWearablesState,
                FetchFunc = (limit, cursor, categories) => FetchGearAsync(includeUser: false, includeDefault: true, limit: limit, cursor: cursor, categories: categories),
                MapFunc = MapGear,
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default gear assets"
            };

            _userWearablesConfig ??= new InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset>
            {
                State = _userWearablesState,
                FetchFunc = (limit, cursor, categories) => FetchGearAsync(includeUser: true, includeDefault: false, limit: limit, cursor: cursor, categories: categories),
                MapFunc = MapGear,
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting user gear assets"
            };

            _avatarConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarResponse, ColorTaggedInventoryAsset>
            {
                State = _avatarState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.Avatar?.Select(item => new ColorTaggedInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.Avatar,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.ModelType,
                    Order = item.Asset?.Order ?? 0,
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar assets"
            };

            _avatarBaseConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarBaseResponse, DefaultAvatarBaseAsset>
            {
                State = _avatarBaseState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarBaseAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarBase?.Select(item => new DefaultAvatarBaseAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AvatarBase,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    SubCategories = item.Asset?.Subcategories,
                    PipelineData =  item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                        ? new PipelineData(item.Asset.Pipeline.First())
                        : null,
                    Tags = item.Asset?.Tags
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar base assets"
            };

            _animationLibraryConfig ??= new InventoryEndpointConfig<GetInventoryV2AnimationLibraryResponse, DefaultAnimationLibraryAsset>
            {
                State = _animationLibraryState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAnimationLibraryAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 500, nextCursor: cursor),
                MapFunc = response => response.AnimationLibrary?.Select(item => new DefaultAnimationLibraryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AnimationLibrary,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  new PipelineData(item.Asset?.Pipeline.First()),
                    MoodsTag = item.Asset?.MoodsTag,
                    ChildAssets = item.Asset?.ChildAssets?
                        .Select(child => new DefaultAnimationChildAsset(child))
                        .ToList() ?? new List<DefaultAnimationChildAsset>()
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default animation library assets"
            };

            _avatarEyesConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarEyesResponse, ColoredInventoryAsset>
            {
                State = _avatarEyesState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarEyesAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarEyes?.Select(item => new ColoredInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AvatarEyes,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                        ? new PipelineData(item.Asset.Pipeline.First())
                        : null,
                    Colors = item.Asset?.HexColors?.Select(c => c.ToUnityColor()).ToList()
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar eyes assets"
            };

            _avatarFlairConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarFlairResponse, DefaultInventoryAsset>
            {
                State = _avatarFlairState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarFlairAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarFlair?.Select(item => new DefaultInventoryAsset
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.Flair,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    SubCategories = new List<string>(),
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  new PipelineData(item.Asset?.Pipeline.First())

                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar flair assets"
            };

            _avatarMakeupConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarMakeupResponse, DefaultInventoryAsset>
            {
                State = _avatarMakeupState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarMakeupAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarMakeup?.Select(item => new DefaultInventoryAsset
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AvatarMakeup,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    SubCategories = item.Asset?.Subcategories,
                    PipelineData =  new PipelineData(item.Asset?.Pipeline.First())
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar makeup assets"
            };

            _colorPresetsConfig ??= new InventoryEndpointConfig<GetInventoryV2ColorPresetsResponse, ColoredInventoryAsset>
            {
                // State is set dynamically per category in GetDefaultColorPresets
                State = null,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultColorPresetsAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 200, nextCursor: cursor),
                MapFunc = response => response.ColorPresets?.Select(item => new ColoredInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.ColorPreset,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    SubCategories = item.Asset?.Subcategories,
                    Order = item.Asset?.Order ?? 0,
                    Colors = item.Asset?.HexColors?.Select(c => c.ToUnityColor()).ToList()
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default color preset assets"
            };

            _decorConfig ??= new InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset>
            {
                State = _decorState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultDecorAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.ModelLibrary?.Select(item => new ColorTaggedInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.Decor,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                        ? new PipelineData(item.Asset.Pipeline.First())
                        : null,
                    ColorTags = item.Asset?.ColorTags
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default decor assets"
            };

            _imageLibraryConfig ??= new InventoryEndpointConfig<GetInventoryV2ImageLibraryResponse, DefaultInventoryAsset>
            {
                State = _imageLibraryState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultImageLibraryAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 500, nextCursor: cursor),
                MapFunc = response => response.ImageLibrary?.Select(item => new DefaultInventoryAsset
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.ImageLibrary,
                    Name = item.Asset.Name,
                    Category = item.Asset.Category,
                    SubCategories = item.Asset.Subcategories,
                    Order = item.Asset.Order ?? 0,
                    PipelineData =  new PipelineData(item.Asset.Pipeline.First())
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default image library assets"
            };

            _modelLibraryConfig ??= new InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset>
            {
                State = _modelLibraryState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultModelLibraryAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.ModelLibrary?.Select(item => new ColorTaggedInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.ModelLibrary,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    SubCategories = item.Asset?.Subcategories,
                    Order = item.Asset?.Order ?? 0,
                    ColorTags = item.Asset?.ColorTags
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default model library assets"
            };
        }

        private async Task<GetInventoryV2GearResponse> FetchGearAsync(
            bool includeUser = true,
            bool includeDefault = true,
            int? limit = null,
            string cursor = null,
            List<string> categories = null)
        {
            Task<GetInventoryV2GearResponse> userTask = null;
            Task<GetInventoryV2GearResponse> defaultTask = null;

            if (includeUser)
            {
                userTask = _inventoryApi.GetInventoryV2GearAsync(await GeniesLoginSdk.GetUserIdAsync(),
                    category: categories, limit: limit ?? 500, nextCursor: cursor);
            }

            if (includeDefault)
            {
                defaultTask = _inventoryApi.GetDefaultGearAsync(orgId: _orgContext, appId: _appContext,
                    category: categories, limit: limit ?? 500, nextCursor: cursor);
            }

            var tasks = new List<Task<GetInventoryV2GearResponse>>();
            if (userTask != null)
            {
                tasks.Add(userTask);
            }

            if (defaultTask != null)
            {
                tasks.Add(defaultTask);
            }

            await Task.WhenAll(tasks);

            var combined = new GetInventoryV2GearResponse { Gear = new() };

            if (userTask?.Result?.Gear != null)
            {
                combined.Gear.AddRange(userTask.Result.Gear);
                combined.NextCursor = userTask.Result.NextCursor;
            }

            if (defaultTask?.Result?.Gear != null)
            {
                combined.Gear.AddRange(defaultTask.Result.Gear);
                // Use default's cursor if user task didn't have one
                if (string.IsNullOrEmpty(combined.NextCursor))
                {
                    combined.NextCursor = defaultTask.Result.NextCursor;
                }
            }

            return combined;
        }

        private List<ColorTaggedInventoryAsset> MapGear(GetInventoryV2GearResponse response)
        {
            return response.Gear?.Select(item => new ColorTaggedInventoryAsset()
            {
                AssetId = item.Asset?.AssetId,
                AssetType = AssetType.WardrobeGear,
                Name = item.Asset?.Name,
                Category = item.Asset?.Category,
                Order = item.Asset?.Order ?? 0,
                PipelineData = item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                    ? new PipelineData(item.Asset.Pipeline.First())
                    : null,
                ColorTags = item.Asset?.ColorTags
            }).ToList();
        }


        /// <summary>
        /// Methods for outside users to call to get inventory data
        /// </summary>
        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultWearables(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_defaultWearablesConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultWearables()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_defaultWearablesConfig, append: true);
        }

        public bool HasMoreDefaultWearables() => _defaultWearablesState.HasMoreData;

        public async UniTask<List<ColorTaggedInventoryAsset>> GetUserWearables(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_userWearablesConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreUserWearables()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_userWearablesConfig, append: true);
        }

        public bool HasMoreUserWearables() => _userWearablesState.HasMoreData;

        public async UniTask<List<ColorTaggedInventoryAsset>> GetAllWearables()
        {
            var userWearables = await GetUserWearables();
            var defaultWearables = await GetDefaultWearables();
            // Create a new list to avoid modifying the cached lists
            var allWearables = new List<ColorTaggedInventoryAsset>(userWearables);
            allWearables.AddRange(defaultWearables);
            return allWearables;
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultAvatar(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultAvatar()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarConfig, append: true);
        }

        public bool HasMoreDefaultAvatar() => _avatarState.HasMoreData;

        public async UniTask<List<DefaultAvatarBaseAsset>> GetDefaultAvatarBaseData(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarBaseConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultAvatarBaseAsset>> LoadMoreDefaultAvatarBaseData()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarBaseConfig, append: true);
        }

        public bool HasMoreDefaultAvatarBaseData() => _avatarBaseState.HasMoreData;

        public async UniTask<List<DefaultAnimationLibraryAsset>> GetDefaultAnimationLibrary(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_animationLibraryConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultAnimationLibraryAsset>> LoadMoreDefaultAnimationLibrary()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_animationLibraryConfig, append: true);
        }

        public bool HasMoreDefaultAnimationLibrary() => _animationLibraryState.HasMoreData;

        public async UniTask<List<ColoredInventoryAsset>> GetDefaultAvatarEyes(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarEyesConfig, limit, categories: categories);
        }

        public async UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultAvatarEyes()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarEyesConfig, append: true);
        }

        public bool HasMoreDefaultAvatarEyes() => _avatarEyesState.HasMoreData;

        public async UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarFlair(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarFlairConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarFlair()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarFlairConfig, append: true);
        }

        public bool HasMoreDefaultAvatarFlair() => _avatarFlairState.HasMoreData;

        public async UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarMakeup(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarMakeupConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarMakeup()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarMakeupConfig, append: true);
        }

        public bool HasMoreDefaultAvatarMakeup() => _avatarMakeupState.HasMoreData;

        public async UniTask<List<ColoredInventoryAsset>> GetDefaultColorPresets(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();

            // Get or create a state for this category
            string categoryKey = categories?.FirstOrDefault() ?? "default";

            if (_colorPresetsStatesByCategory.TryGetValue(categoryKey, out var state) is false)
            {
                state = new FetchState<ColoredInventoryAsset>();
                _colorPresetsStatesByCategory[categoryKey] = state;
            }

            _colorPresetsConfig.State = state;

            return await FetchWithConfig(_colorPresetsConfig, limit, categories: categories);
        }

        public async UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultColorPresets()
        {
            InitializeConfigurations();
            // State should already be set from the initial GetDefaultColorPresets call
            return await FetchWithConfig(_colorPresetsConfig, append: true);
        }

        public bool HasMoreDefaultColorPresets()
        {
            // Check if current config has a state and if it has more data
            return _colorPresetsConfig?.State?.HasMoreData ?? false;
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultDecor(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_decorConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultDecor()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_decorConfig, append: true);
        }

        public bool HasMoreDefaultDecor() => _decorState.HasMoreData;

        public async UniTask<List<DefaultInventoryAsset>> GetDefaultImageLibrary(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_imageLibraryConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultImageLibrary()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_imageLibraryConfig, append: true);
        }

        public bool HasMoreDefaultImageLibrary() => _imageLibraryState.HasMoreData;

        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultModelLibrary(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_modelLibraryConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultModelLibrary()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_modelLibraryConfig, append: true);
        }

        public bool HasMoreDefaultModelLibrary() => _modelLibraryState.HasMoreData;

        private void ClearUserWearablesCache()
        {
            // Clear user wearables cache
            if (_userWearablesState != null)
            {
                _userWearablesState.ClearCache();
            }

            // Clear all wearables cache since it combines user + default
            if (_allWearablesState != null)
            {
                _allWearablesState.ClearCache();
            }
        }

        public async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                string error = "You must be logged in in order to have an asset minted";
                CrashReporter.LogError(error);
                return (false, error);
            }

            try
            {
                var userAssets = await GetUserWearables();
                var userAssetIds = userAssets.Select(u => u.AssetId).ToList();
                if (userAssetIds.Contains(assetId))
                {
                    string note = $"Asset with id {assetId} is already in the user's inventory";
                    CrashReporter.Log(note);
                    return (true, note);
                }

                var request = new MintAssetOnceRequest(assetId,  Guid.NewGuid());
                await _inventoryApi.MintAssetOnceAsync(request, await GeniesLoginSdk.GetUserIdAsync());

                ClearUserWearablesCache();

                // Fire event to notify other NAF that an asset was minted
                AssetMinted?.Invoke(assetId);

                return (true, "Asset granted");
            }
            catch(Exception ex)
            {
                string error = $"Exception trying to mint asset to a user: {ex.Message}";
                CrashReporter.LogError(error);
                return (false, error);
            }
        }


        public async UniTask<string> CreateCustomColor(List<Color> colors, CreateCustomColorRequest.CategoryEnum category)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to create a custom color");
                return null;
            }

            List<string> hexColors = new();
            foreach (var color in colors)
            {
                string hex = $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                hexColors.Add(hex);
            }

            try
            {
                var request = new CreateCustomColorRequest(category, hexColors, null, _appContext, _orgContext);
                var response = await _inventoryApi.CreateCustomColorAsync(request, await GeniesLoginSdk.GetUserIdAsync());
                return response.InstanceId;
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to create a custom color: {ex.Message}");
                return null;
            }
        }

        public async UniTask UpdateCustomColor(string instanceId, List<Color> colors)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to create a custom color");
                return;
            }

            List<string> hexColors = new();
            foreach (var color in colors)
            {
                string hex = $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                hexColors.Add(hex);
            }

            try
            {
                var request = new UpdateCustomColorRequest(hexColors);
                await _inventoryApi.UpdateCustomColorAsync(request, await GeniesLoginSdk.GetUserIdAsync(), instanceId);
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to create a custom color: {ex.Message}");
            }
        }

        public async UniTask DeleteCustomColor(string instanceId, List<Color> colors)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to create a custom color");
                return;
            }

            try
            {
                await _inventoryApi.DeleteCustomColorAsync(await GeniesLoginSdk.GetUserIdAsync(), instanceId);
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to create a custom color: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets custom colors with full metadata including instance IDs for a specific category
        /// </summary>
        /// <param name="category">Category to filter by (hair, skin, flair)</param>
        /// <returns>List of custom color responses with instance IDs and metadata</returns>
        public async UniTask<List<CustomColorResponse>> GetCustomColors(string category = null)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to get custom colors");
                return new();
            }

            try
            {
                var response = await _inventoryApi.ListCustomColorsAsync(await GeniesLoginSdk.GetUserIdAsync(), category, _appContext, _orgContext);
                return response.Colors ?? new List<CustomColorResponse>();
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to get custom colors: {ex.Message}");
                return new();
            }
        }
    }
}
