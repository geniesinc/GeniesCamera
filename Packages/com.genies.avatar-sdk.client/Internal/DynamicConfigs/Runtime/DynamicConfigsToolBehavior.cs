using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Configs;
using Genies.Services.Client;
using Genies.Services.DynamicConfigs.Utils;
using Genies.Services.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Services.DynamicConfigs
{
    public class DynamicConfigsToolBehavior
    {
        private DynamicConfigsAppState _currentAppState = new DynamicConfigsAppState();
        private DynamicConfigFiles _dynamicConfigFiles = new DynamicConfigFiles();
        private IDynamicConfigApi _dynamicConfigApiDev;
        private IDynamicConfigApi _dynamicConfigApiProd;
        private string _userId;
        private List<string> _fallbackConfigIds;

        /// <summary>
        /// Indicates whether this instance is configured for API-only operation (no local files)
        /// </summary>
        private bool IsApiOnlyMode => _fallbackConfigIds != null && _fallbackConfigIds.Count > 0;

        public DynamicConfigsToolBehavior()
        {
            Initialize(null);
        }

        public DynamicConfigsToolBehavior(List<string> configIds)
        {
            Initialize(configIds);
        }

        private void Initialize(List<string> configIds)
        {
            _fallbackConfigIds = configIds;

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

            FetchStateMachine().Forget();
        }
        public bool EnablingUsageToggle
        {
            get
            {
                return _currentAppState.EnablingUsageToggle;
            }
            set
            {
                var newState = new DynamicConfigsAppState()
                {
                    EnablingUsageToggle = value,
                    UseLocalVersion = _currentAppState.UseLocalVersion,
                    DynamicConfigIdList = _currentAppState.DynamicConfigIdList,
                };

                _currentAppState = newState;

                // Only attempt file operations if not in API-only mode
                if (!IsApiOnlyMode)
                {
                    CreateOrUpdateMachineState().Forget();
                }
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
                var newState = new DynamicConfigsAppState()
                {
                    EnablingUsageToggle = _currentAppState.EnablingUsageToggle,
                    UseLocalVersion = value,
                    DynamicConfigIdList = _currentAppState.DynamicConfigIdList,
                };

                _currentAppState = newState;

                // Only attempt file operations if not in API-only mode
                if (!IsApiOnlyMode)
                {
                    CreateOrUpdateMachineState().Forget();
                }
            }
        }

        public List<string> DynamicConfigIdList
        {
            get
            {
                // Use fallback config IDs if local state doesn't have any configured
                if (_currentAppState.DynamicConfigIdList == null || _currentAppState.DynamicConfigIdList.Count == 0)
                {
                    return _fallbackConfigIds ?? new List<string>();
                }
                return _currentAppState.DynamicConfigIdList;
            }
        }

        public async UniTask<DynamicConfigFiles> FetchDynamicConfigsFromApi(List<string> dynamicConfigIds, bool requestDev, bool requestProd)
        {
            var file = new DynamicConfigFiles();

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
                    DynamicConfigNames = dynamicConfigIds,
                    Email = String.Empty,
                    FeatureNames = new List<string>(),
                    Ip = String.Empty,
                    Locale = String.Empty,
                    UserAgent = String.Empty
                };

                if (requestDev)
                {
                    GetDynamicConfigAndFeatureResponse responseDev  = await _dynamicConfigApiDev.GetDynamicConfigAndFeatureAsync(requestConfig);
                    var dictioDev = new Dictionary<string, DynamicConfigFileData>();

                    foreach (var dataDynamicConfig in responseDev.Data.DynamicConfigs)
                    {
                        dictioDev.Add(dataDynamicConfig.Key, new DynamicConfigFileData(dataDynamicConfig.Value));
                    }

                    file.Files.Add(BackendEnvironment.Dev, dictioDev);
                }

                if (requestProd)
                {
                    GetDynamicConfigAndFeatureResponse responseProd  = await _dynamicConfigApiProd.GetDynamicConfigAndFeatureAsync(requestConfig);
                    var dictioProd = new Dictionary<string, DynamicConfigFileData>();

                    foreach (var dataDynamicConfig in responseProd.Data.DynamicConfigs)
                    {
                        dictioProd.Add(dataDynamicConfig.Key, new DynamicConfigFileData(dataDynamicConfig.Value));
                    }

                    file.Files.Add(BackendEnvironment.Prod, dictioProd);
                }

                _dynamicConfigFiles = file;

                // Only attempt file operations if not in API-only mode
                if (!IsApiOnlyMode)
                {
                    await CreateOrUpdateMachineState();
                }

                return file;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Exception fetching dynamic configs from API: {e}");
            }

            return null;
        }


        public async UniTask<bool> CreateOrUpdateLocalData()
        {
#if UNITY_EDITOR
            // Skip file operations entirely in API-only mode
            if (IsApiOnlyMode)
            {
                return false;
            }

            // Check if we have any config files to write
            if (_dynamicConfigFiles?.Files == null || _dynamicConfigFiles.Files.Count == 0)
            {
                return false;
            }

            try
            {
                var urlPath = $"{DynamicConfigUtils.FolderPath}/{DynamicConfigUtils.FolderName}";

                if (!UnityEditor.AssetDatabase.IsValidFolder($"{urlPath}"))
                {
                    UnityEditor.AssetDatabase.CreateFolder(DynamicConfigUtils.FolderPath, DynamicConfigUtils.FolderName);
                }

                foreach (KeyValuePair<BackendEnvironment, Dictionary<string, DynamicConfigFileData>> filesPerEnvironment in _dynamicConfigFiles.Files)
                {
                    if (!UnityEditor.AssetDatabase.IsValidFolder($"{urlPath}/{filesPerEnvironment.Key.ToString()}"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder($"{urlPath}", filesPerEnvironment.Key.ToString());
                    }

                    foreach (KeyValuePair<string, DynamicConfigFileData> dynamicConfigFile  in filesPerEnvironment.Value)
                    {
                        var filePath = $"{urlPath}/{filesPerEnvironment.Key.ToString()}/{dynamicConfigFile.Key}.json";
                        await System.IO.File.WriteAllTextAsync(filePath, dynamicConfigFile.Value.RawJson);
                    }
                }

                UnityEditor.AssetDatabase.Refresh();
                await CreateOrUpdateMachineState();
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to create/update local data files: {e}");
                return false;
            }
#else
            return false;
#endif
        }

        private UniTask<bool> FetchStateMachine()
        {
            // For API-only mode, initialize with defaults and skip file loading
            if (IsApiOnlyMode)
            {
                _currentAppState = new DynamicConfigsAppState()
                {
                    EnablingUsageToggle = false,
                    UseLocalVersion = false,
                    DynamicConfigIdList = _fallbackConfigIds
                };
                return UniTask.FromResult(true);
            }

            try
            {
                var path = $"{DynamicConfigUtils.MainFolderPrefix}/{DynamicConfigUtils.FolderName}/{DynamicConfigUtils.FileStateMachine}";
                TextAsset file = Resources.Load<TextAsset>(path);
                if (file == null)
                {
                    // Initialize with defaults when no local state exists
                    _currentAppState = new DynamicConfigsAppState()
                    {
                        EnablingUsageToggle = false,
                        UseLocalVersion = false,
                        DynamicConfigIdList = new List<string>()
                    };
                    return UniTask.FromResult(true);
                }

                _currentAppState = JsonConvert.DeserializeObject<DynamicConfigsAppState>(file.text);
                return UniTask.FromResult(true);

            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load state machine: {e}");

                // Initialize with defaults on any error
                _currentAppState = new DynamicConfigsAppState()
                {
                    EnablingUsageToggle = false,
                    UseLocalVersion = false,
                    DynamicConfigIdList = new List<string>()
                };
                return UniTask.FromResult(true);
            }
        }
        private async UniTask<bool> CreateOrUpdateMachineState()
        {
            #if UNITY_EDITOR
            // Skip file operations entirely in API-only mode
            if (IsApiOnlyMode)
            {
                return false;
            }

            try
            {
                //try refresh the list of ids
                if (_dynamicConfigFiles?.Files?.Count > 0)
                {
                    _currentAppState.DynamicConfigIdList = _dynamicConfigFiles.Files.First().Value.Keys.ToList();
                }

                var urlPath = $"{DynamicConfigUtils.FolderPath}/{DynamicConfigUtils.FolderName}";

                if (!UnityEditor.AssetDatabase.IsValidFolder(urlPath))
                {
                    UnityEditor.AssetDatabase.CreateFolder($"{DynamicConfigUtils.FolderPath}", $"{DynamicConfigUtils.FolderName}");
                }

                var serializedState = JsonConvert.SerializeObject(_currentAppState);
                await System.IO.File.WriteAllTextAsync($"{urlPath}/{DynamicConfigUtils.FileStateMachine}.json", serializedState);

                UnityEditor.AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to create/update local state machine file: {e}");
            }
            #endif

            return false;
        }

        public UniTask<T> GetDynamicConfig<T>(BackendEnvironment environment, string configName, string jsonKey = default)
        {
            try
            {
                //try to get the dynamic config from the BE Api
                if(_dynamicConfigFiles?.Files != null &&
                   _dynamicConfigFiles.Files.TryGetValue(environment, out var environmentFiles) &&
                   environmentFiles.TryGetValue(configName, out DynamicConfigFileData file))
                {
                    var rawJson = JsonConvert.SerializeObject(file.ApiJsonObject.Value[jsonKey]);
                    T data = JsonConvert.DeserializeObject<T>(rawJson);
                    return UniTask.FromResult(data);
                }

            }
            catch (Exception e)
            {
                CrashReporter.Log($"Failed to get a dynamic config: {configName} from API: {e}", LogSeverity.Error);
                return UniTask.FromResult(default(T));
            }

            return UniTask.FromResult(default(T));
        }

        public UniTask<T> GetLocalDynamicConfig<T>(BackendEnvironment environment, string configName, string jsonKey = default)
        {
            // Return default immediately for API-only mode (no local files expected)
            if (IsApiOnlyMode)
            {
                return default;
            }

            try
            {
                var path = $"{DynamicConfigUtils.MainFolderPrefix}/{DynamicConfigUtils.FolderName}/{environment.ToString()}/{configName}";
                TextAsset file = Resources.Load<TextAsset>(path);
                if (file == null)
                {
                    CrashReporter.LogError($"Failed to get a dynamic config: {path} on local files");
                    return default;
                }

                DynamicConfig dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(file.text);
                var rawJson = JsonConvert.SerializeObject(dynamicConfig.Value[jsonKey]);
                T data = JsonConvert.DeserializeObject<T>(rawJson);
                return UniTask.FromResult(data);

            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to get dynamic config '{configName}' from local files: {e}");
                return default;
            }
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
    public class DynamicConfigsAppState
    {
        public bool EnablingUsageToggle;
        public bool UseLocalVersion;
        public List<string> DynamicConfigIdList;
    }

    /// <summary>
    /// Represents the collection of all raw dynamic json files per environment
    /// </summary>
    [Serializable]
    public class DynamicConfigFiles
    {
        public Dictionary<BackendEnvironment, Dictionary<string, DynamicConfigFileData>> Files;

        public DynamicConfigFiles()
        {
            Files = new Dictionary<BackendEnvironment, Dictionary<string, DynamicConfigFileData>>();
        }

    }

    /// <summary>
    /// File that contains a json file of a Dynamic Config from Backend API
    /// This file is totally equal to the Statsig dynamic config JSON
    /// </summary>
    ///
    [Serializable]
    public class DynamicConfigFileData
    {
        public DynamicConfig ApiJsonObject { get; private set; }
        public string RawJson { get; private set; }

        public DynamicConfigFileData(DynamicConfig apiJsonObject)
        {
            ApiJsonObject = apiJsonObject;
            RawJson = JsonConvert.SerializeObject(ApiJsonObject);
        }

    }
}
