using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Exception = System.Exception;

namespace Genies.FeatureFlags
{
    /// <summary>
    /// Class responsible to isolate all the logic from Feature Flag Tool Window
    /// </summary>
    public class FeatureFlagsToolBehavior
    {
        private const string _dataPath = "Party/Data";
        private FeatureFlagsAppState _currentAppState = new FeatureFlagsAppState();
        private IDynamicConfigApi _dynamicConfigApiDev;
        private IDynamicConfigApi _dynamicConfigApiProd;
        private FeatureFlagsDataInfo _listPartnerDataInfo;
        public FeatureFlagsDataInfo ListPartnerDataInfo => _listPartnerDataInfo;
        private string _userId = string.Empty;
        private bool _usePartnerListData;
        private List<string> _fallbackFeatureFlags;

        /// <summary>
        /// Indicates whether this instance is configured for API-only operation (no local files)
        /// </summary>
        private bool IsApiOnlyMode => _fallbackFeatureFlags != null && _fallbackFeatureFlags.Count > 0;

        /// <summary>
        /// Creates a FeatureFlagsFileData with fallback feature flags set to default values
        /// </summary>
        private FeatureFlagsFileData CreateFeatureFlagsWithDefaults()
        {
            var flagsData = new FeatureFlagsFileData();
            
            // Get the list of flags to populate
            var flagsToPopulate = new List<string>();
            if (_fallbackFeatureFlags != null)
            {
                flagsToPopulate.AddRange(_fallbackFeatureFlags);
            }
            
            var sharedFlags = SharedFeatureFlags.GetList();
            if (sharedFlags != null)
            {
                flagsToPopulate.AddRange(sharedFlags);
            }
            
            // Remove duplicates
            flagsToPopulate = flagsToPopulate.Distinct().ToList();
            
            // Create default flag dictionaries for both environments
            var defaultFlags = flagsToPopulate.ToDictionary(flag => flag, flag => GetDefaultValueForFlag(flag));
            
            flagsData.SetDataPerEnvironment(BackendEnvironment.Dev, new Dictionary<string, bool>(defaultFlags));
            flagsData.SetDataPerEnvironment(BackendEnvironment.Prod, new Dictionary<string, bool>(defaultFlags));
            
            return flagsData;
        }
        
        /// <summary>
        /// Gets the default value for a specific feature flag
        /// Most essential Avatar Editor flags should be enabled by default
        /// </summary>
        private bool GetDefaultValueForFlag(string flagName)
        {
            // Essential flags that should be enabled by default for Avatar Editor
            var enabledByDefault = new HashSet<string>
            {
                SharedFeatureFlags.DynamicConfigsFromApi,
                SharedFeatureFlags.BaserowCms,
                SharedFeatureFlags.AddressablesCmsLocationService,
                SharedFeatureFlags.AddressablesInventoryLocations,
                SharedFeatureFlags.InventoryClient,
                SharedFeatureFlags.SmartAvatar,
                SharedFeatureFlags.NonUmaAvatar,
                SharedFeatureFlags.LanguageSupport,
                SharedFeatureFlags.GearContent,
                SharedFeatureFlags.ExternalGearContent
            };
            
            return enabledByDefault.Contains(flagName);
        }

        public FeatureFlagsToolBehavior(bool usePartnerListData = true)
        {
            Initialize(null, usePartnerListData);
        }

        public FeatureFlagsToolBehavior(List<string> fallbackFeatureFlags)
        {
            Initialize(fallbackFeatureFlags, false);
        }

        private void Initialize(List<string> fallbackFeatureFlags, bool usePartnerListData)
        {
            _fallbackFeatureFlags = fallbackFeatureFlags;
            _usePartnerListData = usePartnerListData;
            
            var configDev = new Configuration()
            {
                BasePath = "https://api.dev.genies.com",
            };

            var configProd = new Configuration()
            {
                BasePath = "https://api.genies.com",
            };

            _dynamicConfigApiDev = new DynamicConfigApi(configDev);
            _dynamicConfigApiProd = new DynamicConfigApi(configProd);

            FetchLocalFeatureFlags().Forget();
        }
        public bool EnablingUsageToggle
        {
            get
            {
                return _currentAppState.EnablingUsageToggle;
            }
            set
            {
                var newState = new FeatureFlagsAppState()
                {
                    EnablingUsageToggle = value,
                    UseLocalVersion = _currentAppState.UseLocalVersion,
                    FeatureFlagsFileData = _currentAppState.FeatureFlagsFileData,
                };

                _currentAppState = newState;

                // Only attempt file operations if not in API-only mode
                if (!IsApiOnlyMode)
                {
                    CreateOrUpdateLocalData(newState).Forget();
                }
            }
        }

        /// <summary>
        ///  This Behavior will fetch all the flags that comes from:
        ///  - Flags Data Info (Scriptable Object)
        ///  - SharedFeatureFlags (Common Flags for multiple usage on different packages)
        ///  - GeniesPartyFeatureFlags (Flags that we're using only on Genies Party)
        /// </summary>
        /// <returns></returns>
        public UniTask<List<string>> FetchFlagsDataInfo()
        {
            List<string> sharedFlags = SharedFeatureFlags.GetList();
            
            try
            {
                // For API-only mode, use fallback feature flags
                if (IsApiOnlyMode)
                {
                    var apiOnlyList = new List<string>();
                    if (_fallbackFeatureFlags != null)
                    {
                        apiOnlyList.AddRange(_fallbackFeatureFlags);
                    }
                    if (sharedFlags != null)
                    {
                        apiOnlyList.AddRange(sharedFlags);
                    }
                    return UniTask.FromResult(apiOnlyList);
                }

                if (_listPartnerDataInfo != null)
                {
                    //considering the list from shared feature flag as well
                    var currentList = new List<string>();
                    if (_listPartnerDataInfo.Data != null)
                    {
                        currentList.AddRange(_listPartnerDataInfo.Data);
                    }

                    if (sharedFlags != null)
                    {
                        currentList.AddRange(sharedFlags);
                    }
                    return UniTask.FromResult(currentList);
                }

                if (_usePartnerListData)
                {
                    FeatureFlagsDataInfo[] dataFiles = Resources.LoadAll<FeatureFlagsDataInfo>(_dataPath);

                    if (dataFiles == null || dataFiles.Length == 0)
                    {
                        Debug.LogError($"ListPartnerDataInfo not found it");
  
                        return UniTask.FromResult(sharedFlags ?? new List<string>());
                    }

                    _listPartnerDataInfo = dataFiles.FirstOrDefault(d => d.name.Contains("Flag"));

                    if (_listPartnerDataInfo == null)
                    {
                        Debug.LogError($"Invalid data info for Feature Flags");
                        return UniTask.FromResult(sharedFlags ?? new List<string>());
                    }
                }

                //considering the list from shared feature flag as well
                var finalList = new List<string>();
                if (_listPartnerDataInfo != null && _listPartnerDataInfo.Data != null)
                {
                    finalList.AddRange(_listPartnerDataInfo.Data);
                }
                
                if (sharedFlags != null)
                {
                    finalList.AddRange(sharedFlags);
                }

                return UniTask.FromResult(finalList);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return UniTask.FromResult(sharedFlags ?? new List<string>());
            }
        }

        public bool UseLocalVersion
        {
            get
            {
                return _currentAppState.UseLocalVersion;
            }
            set
            {
                var newState = new FeatureFlagsAppState()
                {
                    EnablingUsageToggle = _currentAppState.EnablingUsageToggle,
                    UseLocalVersion = value,
                    FeatureFlagsFileData = _currentAppState.FeatureFlagsFileData,
                };

                _currentAppState = newState;

                // Only attempt file operations if not in API-only mode
                if (!IsApiOnlyMode)
                {
                    CreateOrUpdateLocalData(newState).Forget();
                }
            }
        }

        /// <summary>
        /// It will try to access the file that already exist,otherwise return and error
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> UpdateLocalFromBackend()
        {
#if UNITY_EDITOR
            try
            {
                await FetchFlagsDataInfo();

                //considering the list from shared feature flag as well
                var finalList = new List<string>();
                finalList.AddRange(_listPartnerDataInfo.Data);
                finalList.AddRange(SharedFeatureFlags.GetList());

                FeatureFlagsFileData apiVersion = await FetchApiFeatureFlags(finalList, requestDev:true, requestProd:true);

                if (!AssetDatabase.IsValidFolder($"{FeatureFlagsUtils.FolderPath}/{FeatureFlagsUtils.FolderName}"))
                {
                    AssetDatabase.CreateFolder(FeatureFlagsUtils.FolderPath, FeatureFlagsUtils.FolderName);
                }

                var serialized = JsonConvert.SerializeObject(apiVersion, Formatting.Indented);

                await System.IO.File.WriteAllTextAsync($"{FeatureFlagsUtils.FilePath}", serialized);
                await CreateOrUpdateLocalData(apiVersion);

                return true;

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// It will try to access the file that already exist,otherwise return and error
        /// </summary>
        /// <returns></returns>
        public UniTask<FeatureFlagsFileData> FetchLocalFeatureFlags()
        {
            // For API-only mode, initialize with defaults and skip file loading
            if (IsApiOnlyMode)
            {
                var defaultFeatureFlagsData = CreateFeatureFlagsWithDefaults();
                _currentAppState = new FeatureFlagsAppState()
                {
                    EnablingUsageToggle = false,
                    UseLocalVersion = false,
                    FeatureFlagsFileData = defaultFeatureFlagsData
                };
                return UniTask.FromResult(defaultFeatureFlagsData);
            }

            try
            {
                TextAsset file = Resources.Load<TextAsset>($"{FeatureFlagsUtils.FolderName}/{FeatureFlagsUtils.FileName}");
                if (file == null)
                {
                    return UniTask.FromResult(CreateFeatureFlagsWithDefaults());
                }

                _currentAppState = JsonConvert.DeserializeObject<FeatureFlagsAppState>(file.text);
                return UniTask.FromResult(_currentAppState.FeatureFlagsFileData);

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return UniTask.FromResult(CreateFeatureFlagsWithDefaults());
            }
        }

        /// <summary>
        /// It will create or update the local file based on the BE data
        /// </summary>
        /// <returns></returns>
        public async UniTask<FeatureFlagsFileData> FetchApiFeatureFlags(List<string> availableFlags, bool requestDev, bool requestProd)
        {
            try
            {
                //during runtime, we can access the current user Id and send it to the API
                if(Application.isPlaying)
                {
                    await TryGetUserId();
                }

                //setup de BE model with the list of flags requests and the current app configuration
                var requestConfig = new GetDynamicConfigAndFeatureRequest()
                {
                    AppVersion =  Application.version,
                    Country = String.Empty,
                    Custom = new Dictionary<string, object>()
                    {
                        { "BundleId", Application.identifier },
                        { "AppName", Application.productName },
                        { "Platform", Application.platform.ToString() },
                        { "CognitoId", _userId},
                    },
                    DynamicConfigNames = new List<string>(),
                    Email = String.Empty,
                    FeatureNames = availableFlags,
                    Ip = String.Empty,
                    Locale = String.Empty,
                    UserAgent = String.Empty
                };


                return await FetchCombinedFeatureFlagsAndDynamicConfig(requestConfig,requestDev, requestProd);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            return CreateFeatureFlagsWithDefaults();
        }

        /// <summary>
        /// Get combined dynamic config and feature gate
        /// </summary>
        /// <returns></returns>
        public async UniTask<FeatureFlagsFileData> FetchCombinedFeatureFlagsAndDynamicConfig(GetDynamicConfigAndFeatureRequest requestConfig, bool requestDev, bool requestProd)
        {
            var flagsData = new FeatureFlagsFileData();

            if (requestDev)
            {
                try
                {
                    GetDynamicConfigAndFeatureResponse responseDev  = await _dynamicConfigApiDev.GetDynamicConfigAndFeatureAsync(requestConfig);
                    Dictionary<string, bool> dictionaryDev = await ProcessFlagsByEnvironment(responseDev.Data.Features);
                    flagsData.SetDataPerEnvironment(BackendEnvironment.Dev, dictionaryDev);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }

            if (requestProd)
            {
                try
                {
                    GetDynamicConfigAndFeatureResponse responseProd  = await _dynamicConfigApiProd.GetDynamicConfigAndFeatureAsync(requestConfig);
                    Dictionary<string, bool> dictionaryDev = await ProcessFlagsByEnvironment(responseProd.Data.Features);
                    flagsData.SetDataPerEnvironment(BackendEnvironment.Prod, dictionaryDev);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }

            }


            return flagsData;
        }


        private UniTask<Dictionary<string,bool>> ProcessFlagsByEnvironment(Dictionary<string, object> data)
        {
            var dictio = data
                .ToDictionary( p=> p.Key, p=> (bool)p.Value);

            return UniTask.FromResult(dictio);
        }


        private async UniTask<bool> CreateOrUpdateLocalData(FeatureFlagsAppState newAppState)
        {
            _currentAppState = newAppState;
            return await CreateOrUpdateLocalData(_currentAppState.FeatureFlagsFileData);
        }

        private async UniTask<bool> CreateOrUpdateLocalData(FeatureFlagsFileData fileData)
        {
#if UNITY_EDITOR
            // Skip file operations entirely in API-only mode
            if (IsApiOnlyMode)
            {
                return false;
            }

            try
            {
                var currentState = new FeatureFlagsAppState()
                {
                    EnablingUsageToggle = this.EnablingUsageToggle,
                    UseLocalVersion = this.UseLocalVersion,
                    FeatureFlagsFileData = fileData,
                };

                if (!AssetDatabase.IsValidFolder($"{FeatureFlagsUtils.FolderPath}/{FeatureFlagsUtils.FolderName}"))
                {
                    AssetDatabase.CreateFolder(FeatureFlagsUtils.FolderPath, FeatureFlagsUtils.FolderName);
                }

                var serializedState = JsonConvert.SerializeObject(currentState, Formatting.Indented);
                await System.IO.File.WriteAllTextAsync($"{FeatureFlagsUtils.FilePath}", serializedState);

                AssetDatabase.Refresh();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
#else
            return false;
#endif
        }

        public void OverrideLocalFeatureFlag(BackendEnvironment env, string flagId, bool newValue)
        {
            FeatureFlagsFileData newFileData = _currentAppState.FeatureFlagsFileData;
            newFileData.Data[env][flagId] = newValue;

            var newState = new FeatureFlagsAppState()
            {
                EnablingUsageToggle = _currentAppState.EnablingUsageToggle,
                UseLocalVersion = _currentAppState.UseLocalVersion,
                FeatureFlagsFileData = newFileData,
            };

            CreateOrUpdateLocalData(newState).Forget();
        }

        private async UniTask<string> TryGetUserId()
        {
            await UniTask.WaitUntil(()=>GeniesLoginSdk.IsInitialized);

            try
            {
                _userId = await GeniesLoginSdk.GetUserIdAsync();
            }
            catch (Exception)
            {
                _userId = string.Empty;

            }

            return _userId;
        }

    }

    [Serializable]
    public class FeatureFlagsAppState
    {
        public bool EnablingUsageToggle;
        public bool UseLocalVersion;
        public FeatureFlagsFileData FeatureFlagsFileData;
    }
}
