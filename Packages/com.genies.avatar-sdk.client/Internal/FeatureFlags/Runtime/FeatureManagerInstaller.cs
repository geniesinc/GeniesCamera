using System;
using System.Collections.Generic;
using Genies.Components.FeatureFlags;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.FeatureFlags
{
    [AutoResolve]
    [Serializable]
    public class FeatureManagerInstaller : IGeniesInstaller
    {
        public FeatureConfig featureManagerConfig;

        /// <summary>
        /// Optional list of feature flag IDs to use when no local state exists.
        /// This enables API-only operation without requiring local files.
        /// </summary>
        public List<string> FallbackFeatureFlags { get; set; }

        /// <summary>
        /// Force enable the API service. Default is true when FallbackFeatureFlags are provided.
        /// </summary>
        public bool ForceEnableApiService { get; set; }

        private FeatureFlagsToolBehavior _ffToolBehavior;
        public int OperationOrder => (DefaultInstallationGroups.CoreDependency + 1);

        public void Install(IContainerBuilder builder)
        {
            // Create tool behavior with API-only mode if fallback flags are provided
            _ffToolBehavior = FallbackFeatureFlags != null && FallbackFeatureFlags.Count > 0
                ? new FeatureFlagsToolBehavior(FallbackFeatureFlags)
                : new FeatureFlagsToolBehavior();

            if (featureManagerConfig != null)
            {
                builder.RegisterInstance(featureManagerConfig);
            }
            else
            {
                Debug.LogWarning($"No {nameof(FeatureConfig)} provided with the {nameof(FeatureManagerInstaller)} instance.");
            }


            // Auto-enable API service if we have fallback feature flags or if explicitly requested
            bool shouldUseApiService = ForceEnableApiService ||
                                     (FallbackFeatureFlags != null && FallbackFeatureFlags.Count > 0) ||
                                     _ffToolBehavior.EnablingUsageToggle;

            // Override the toggle for API-only operation
            if (shouldUseApiService && !_ffToolBehavior.EnablingUsageToggle)
            {
                _ffToolBehavior.EnablingUsageToggle = true;
            }

            if (_ffToolBehavior.EnablingUsageToggle)
            {
                builder.Register<IFeatureFlagsManager, FeatureFlagsManagerFromApi>(Lifetime.Singleton)
                    .WithParameter(_ffToolBehavior)
                    .WithParameter(false)
                    .AsSelf();
            }
            else
            {
                builder.Register<IFeatureFlagsManager, FeatureManager>(Lifetime.Singleton)
                    .AsSelf();
            }
        }
    }
}
