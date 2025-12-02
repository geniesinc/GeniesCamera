using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Services.DynamicConfigs
{
    [AutoResolve]
    public class DynamicConfigServiceInstaller: IGeniesInstaller, IGeniesInitializer
    {
        /// <summary>
        /// Optional list of dynamic config IDs to fetch when no local state exists.
        /// This enables API-only operation without requiring local files.
        /// </summary>
        public List<string> FallbackConfigIds { get; set; }

        /// <summary>
        /// Force enable the API service instead of Statsig. Default is true when FallbackConfigIds are provided.
        /// </summary>
        public bool ForceEnableApiService { get; set; }

        /// <summary>
        /// Force prod enviornment usage
        /// </summary>
        public bool ProdOverride { get; set; }

        public int OperationOrder => DefaultInstallationGroups.PostCoreServices;

        public void Install(IContainerBuilder builder)
        {
            var toolBehavior = new DynamicConfigsToolBehavior(FallbackConfigIds);

            // Auto-enable API service if we have fallback config IDs or if explicitly requested
            bool shouldUseApiService = ForceEnableApiService ||
                                     (FallbackConfigIds != null && FallbackConfigIds.Count > 0) ||
                                     toolBehavior.EnablingUsageToggle;

            // Override the toggle for API-only operation
            if (shouldUseApiService && !toolBehavior.EnablingUsageToggle)
            {
                toolBehavior.EnablingUsageToggle = true;
            }

            if (toolBehavior.EnablingUsageToggle)
            {
                builder.Register<IDynamicConfigService, DynamicConfigServiceFromApi>(Lifetime.Singleton)
                    .WithParameter(toolBehavior)
                    .WithParameter(ProdOverride)
                    .AsSelf();
            }
            else
            {
                builder.Register<IDynamicConfigService, DynamicConfigServiceFromStatsig>(Lifetime.Singleton).AsSelf();
            }

        }

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }
    }
}
