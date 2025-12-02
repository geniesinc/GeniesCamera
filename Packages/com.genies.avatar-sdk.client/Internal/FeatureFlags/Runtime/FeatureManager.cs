using System;
using Genies.ABTesting;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.FeatureFlags
{

/*
 * Features can be enabled / disabled at runtime or compile time.
 * To enable at compile time, add the define symbol to
 * Project Settings -> Player -> Scripting Define Symbols.
 * (can be automated via Genies Defines editor menu)
 * To enable at runtime, use the SetFeature() method.
 *
 * Use compiletime enabling for experimental features and release
 * toggles.
 *
 * Runtime enabling allows things like features enabling other
 * features, A/B testing, and unit testing.
 *
 * To add a new feature:
 *   1. Add your feature to the FeatureFlag enum in FeatureConfig.cs
 *      If your feature requires no custom logic, and can be enabled/disabled at runtime, you are done.
 *   2. Optional: add custom getter containing logic controlling conditions
 *      under which feature should be enabled/disabled, etc
 *      (can also enable/disable interdependent/incompatible features,
 *      though be careful about method call order). Do this preferrably as a private method so you set is
 *      as an override and it is automatically executed when trying to access the feature state.
 *   3. Optional: add your custom getter to the GetterOverrides dictionary.
 *   4. Optional: if needed, do the same for custom setter.
 *
*/
    public class FeatureManager: IFeatureFlagsManager
    {
        private readonly IABTestingService _abTestingService;
        private IABTestingService AbTestingService => _abTestingService ?? ServiceManager.Get<IABTestingService>();
        private Dictionary<string, bool> _localFeatureStates;
        private readonly Dictionary<string, Func<bool>> _compileTimeFeatureGetterOverrides = new Dictionary<string, Func<bool>>();

        /// <summary>
        /// Tracks any overrides that were changed at runtime (mainly for dev tools but could have other use cases)
        /// this is static because we don't want feature flags to be lost in between scene reloads.
        ///
        /// TODO in the future if we do need runtime overrides to be reset we can evaluate a better solution
        /// </summary>
        private static readonly Dictionary<string, Func<bool>> _runtimeGetterOverrides = new Dictionary<string, Func<bool>>();

        private FeatureManager(){}

        [Inject]
        public FeatureManager(FeatureConfig featureConfig)
        {
            InitializeOverrides();
            InitializeFeatureFlagStates(featureConfig);
        }

        public FeatureManager(FeatureConfig featureConfig, IABTestingService abTestingService)
        {
            _abTestingService = abTestingService;
            InitializeOverrides();
            InitializeFeatureFlagStates(featureConfig);
        }

        /// <summary>
        /// Resolve feature flag state on load.
        /// </summary>
        private void InitializeFeatureFlagStates(FeatureConfig featureConfig)
        {
            //Initialize flag states
            _localFeatureStates = new Dictionary<string, bool>();

            //If we have a valid config
            if (featureConfig != null)
            {
                //Load all local config first
                foreach (FeatureData featureData in featureConfig.features)
                {
                    if (featureData != null)
                    {
                        SetLocalFeatureState(featureData.featureFlag, featureData.enabled);


#if UNITY_EDITOR
                    if (featureData.shouldOverrideRemote)
                    {
                        SetFeatureFlagOverride(featureData.featureFlag, () => featureData.enabled);
                    }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Use this if you want a feature flag to ignore <see cref="FeatureConfig"/> or <see cref="IABTestingService"/>
        /// </summary>
        private void InitializeOverrides()
        {
            // set feature getter overrides

            _compileTimeFeatureGetterOverrides[SharedFeatureFlags.BypassAuth] = BypassAuthGetter;
        }

        public UniTask<Dictionary<string, bool>> GetAllFeatureFlagsStatus()
        {
            var featureFlags = new Dictionary<string, bool>();
            foreach (var flag in FeatureFlagsHelper.AllFlagValues)
            {
                if (flag == "none")
                {
                    continue;
                }

                featureFlags[flag] = IsFeatureEnabled(flag);
            }

            return UniTask.FromResult(featureFlags);
        }

        /// <summary>
        /// Set a feature flag override at runtime.
        /// </summary>
        public void SetFeatureFlagOverride(string featureFlag, Func<bool> isEnabledGetter)
        {
            _runtimeGetterOverrides[featureFlag] = isEnabledGetter;
        }

        /// <summary>
        /// Remove a feature flag override at runtime.
        /// </summary>
        public void RemoveFeatureFlagOverride(string featureFlag)
        {
            if (_runtimeGetterOverrides.ContainsKey(featureFlag))
            {
                _runtimeGetterOverrides.Remove(featureFlag);
            }
        }

        /// <summary>
        ///  This check statement should prioritize:
        ///  1- any override for that flag
        ///  2- after checking if that flag have remote config
        ///  3- and then, check locally
        /// </summary>
        public bool IsFeatureEnabled(string featureFlag)
        {
            if (string.IsNullOrEmpty(featureFlag))
            {
                return false;
            }

            if (_runtimeGetterOverrides.TryGetValue(featureFlag, out var runtimeGetter))
            {
                return runtimeGetter();
            }

            if (_compileTimeFeatureGetterOverrides.TryGetValue(featureFlag, out var compileTimeGetter))
            {
                return compileTimeGetter();
            }

            try
            {
                if (AbTestingService != null)
                {
                    return AbTestingService.CheckFeatureFlag(featureFlag);
                }
            }
            catch (Exception e)
            {
                //Report crash
                CrashReporter.LogHandledException(e);

                //Fallback to returning local config
                return GetLocalFeatureState(featureFlag);
            }

            return GetLocalFeatureState(featureFlag);
        }

        /// <summary>
        /// Sets the local feature state
        /// </summary>
        private void SetLocalFeatureState(string featureFlag, bool enabled)
        {
            _localFeatureStates[featureFlag] = enabled;
        }

        private bool GetLocalFeatureState(string featureFlag)
            => _localFeatureStates.TryGetValue(featureFlag, out bool value) && value;

        #region Getter overrides

        private bool BypassAuthGetter()
        {
            // process symbol defines
#if BYPASS_AUTH
        return true;
#endif
            return false;
        }

        #endregion
    }
}
