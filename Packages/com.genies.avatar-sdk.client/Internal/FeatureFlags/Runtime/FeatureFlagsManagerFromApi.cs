using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.FeatureFlags;
using Genies.ServiceManagement;
using Genies.Services.Configs;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Components.FeatureFlags
{
    public class FeatureFlagsManagerFromApi: IFeatureFlagsManager
    {
        private GeniesAppStateManager _StateManager => this.GetService<GeniesAppStateManager>();

        private const string SelectedBackendEnvironment = "SelectedBackendEnvironment";

        // version to use on the devtools, so we can save the changes on the session
        private static FeatureFlagsFileData _sessionFlagsFileData = null;
        // local version of the flags in case the API is not available
        private static FeatureFlagsFileData _localFlagsFallback = null;

        private FeatureFlagsFileData _flagsFileData;
        private FeatureFlagsToolBehavior _ffToolBehavior;
        private UniTaskCompletionSource _apiInitializationSource;
        private BackendEnvironment _currentEnvironment = BackendEnvironment.Dev;
        private bool _prodOverride;

        public FeatureFlagsManagerFromApi(FeatureFlagsToolBehavior ffToolBehavior, bool prodOverride = false)
        {
            _prodOverride = prodOverride;
            if (_prodOverride)
            {
                _currentEnvironment = BackendEnvironment.Prod;
            }

            _ffToolBehavior = ffToolBehavior;
            FallbackInitialization().Forget();

#if PRODUCTION_BUILD
            _currentEnvironment = BackendEnvironment.Prod;
#else

            var hasEnvironmentValue = false;
            if (_StateManager != null)
            {
                hasEnvironmentValue = _StateManager.HasState(SelectedBackendEnvironment);
            }

            if (!prodOverride)
            {
                _currentEnvironment = hasEnvironmentValue
                    ? _StateManager.GetState<BackendEnvironment>(SelectedBackendEnvironment)
                    : BackendEnvironment.Dev;
            }
#endif

            AwaitApiInitialization().Forget();
        }

        public FeatureFlagsManagerFromApi()
        {

        }


        private async UniTask FallbackInitialization()
        {
            _localFlagsFallback  = await _ffToolBehavior.FetchLocalFeatureFlags();
        }

        private async UniTask AwaitApiInitialization()
        {
            try
            {
                // eventually pass trait service before chat service
                if (_apiInitializationSource != null)
                {
                    await _apiInitializationSource.Task;
                    return;
                }

                _apiInitializationSource = new UniTaskCompletionSource();

                //if a session version is already exist, we will consume that one from cache to keep changes from the devtools
                if (_sessionFlagsFileData == null)
                {
                    if (_ffToolBehavior.UseLocalVersion)
                    {
                        _flagsFileData = await _ffToolBehavior.FetchLocalFeatureFlags();
                    }
                    else
                    {
                        var flagsDataInfo = await _ffToolBehavior.FetchFlagsDataInfo();
#if PRODUCTION_BUILD
                        //load from the BE API for Dev
                        _flagsFileData = await _ffToolBehavior.FetchApiFeatureFlags(flagsDataInfo, requestDev:false, requestProd:true);
#else

                        //load from the BE API for Dev
                        _flagsFileData = await _ffToolBehavior.FetchApiFeatureFlags(flagsDataInfo, requestDev:!_prodOverride, requestProd:_prodOverride);
#endif

                    }

                    _sessionFlagsFileData = _flagsFileData;
                    await ProcessFlagsForDevTools();
                }

                _apiInitializationSource.TrySetResult();
                _apiInitializationSource = null;

            }
            catch (Exception exception)
            {
                CrashReporter.LogError(
                    $"Failed to Initialize API in {nameof(FeatureFlagsManagerFromApi)}: {exception}");
            }
        }

        private UniTask ProcessFlagsForDevTools()
        {
            //before returning the values, we have to override the FlagsHelper for updating the devtools window properly
            if (_sessionFlagsFileData?.Data?.TryGetValue(BackendEnvironment.Dev, out var sessionFeatureFlags) == true)
            {
                var stringValues = sessionFeatureFlags.Values.Select(v => v.ToString()).ToList();
                FeatureFlagsHelper.OverrideFlags(sessionFeatureFlags.Keys.ToList(),stringValues );
            }

            return UniTask.CompletedTask;
        }
        public async UniTask<Dictionary<string, bool>> GetAllFeatureFlagsStatus()
        {
            await AwaitApiInitialization();

            if (_sessionFlagsFileData?.Data?.TryGetValue(_currentEnvironment, out var environmentFlags) == true)
            {
                return environmentFlags;
            }

            // Return empty dictionary as fallback
            return new Dictionary<string, bool>();
        }

        public void SetFeatureFlagOverride(string featureFlag, Func<bool> isEnabledGetter)
        {
            //legacy
        }

        public void RemoveFeatureFlagOverride(string featureFlag)
        {
            //clean the session
            _sessionFlagsFileData = null;
        }

        public bool IsFeatureEnabled(string featureFlag)
        {
            //read from the latest data request if it is available
            if(_sessionFlagsFileData != null &&
               _sessionFlagsFileData.Data != null &&
               _sessionFlagsFileData.Data.TryGetValue(_currentEnvironment, out var sessionEnvData) &&
               sessionEnvData.TryGetValue(featureFlag, out var flag))
            {
                return flag;
            }

            if(_localFlagsFallback != null &&
               _localFlagsFallback.Data != null &&
               _localFlagsFallback.Data.TryGetValue(_currentEnvironment, out var localEnvData) &&
               localEnvData.TryGetValue(featureFlag, out var localFallbackFlag))
            {
                return localFallbackFlag;
            }

            Debug.LogWarning($"[{nameof(FeatureFlagsManagerFromApi)}] - Flag Not Found: {featureFlag}");
            return false;
        }
    }
}
