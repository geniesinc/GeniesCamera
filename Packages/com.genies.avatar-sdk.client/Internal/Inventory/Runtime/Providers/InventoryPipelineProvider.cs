using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.ServiceManagement;
using Genies.Services.DynamicConfigs;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory.Providers
{
    public static class InventoryPipelineProvider
    {
        private static readonly ConcurrentDictionary<string, PipelineVersion> _pipelineVersionsCache = new ConcurrentDictionary<string, PipelineVersion>();

        public static async UniTask<PipelineItem> ResolvePipelineItem(InventoryItemAsset asset)
        {
            if (asset.Pipeline == null || asset.Pipeline.Count == 0)
            {
                CrashReporter.LogWarning($"[InventoryPipelineService.ResolvePipelineItem] Asset {asset.AssetId} has no pipeline defined.");
                //return null;
                asset.Pipeline = new List<PipelineItem>()
                {
                    new ()
                    {
                        AssetVersion = 0,
                        PipelineVersion = 0,
                        ParentId = asset.AssetId,
                    },
                };
            }

            if (!_pipelineVersionsCache.TryGetValue(asset.AssetType, out PipelineVersion pipelineVersion))
            {
                IDynamicConfigService dynamicConfigService = ServiceManager.Get<IDynamicConfigService>();

                if (dynamicConfigService == null)
                {
                    CrashReporter.LogError("[InventoryPipelineService.ResolvePipelineItem] DynamicConfigService is not available.");
                    return asset.Pipeline[^1]; // Return the last pipeline item as a fallback
                }

                pipelineVersion = await dynamicConfigService.GetDynamicConfig<PipelineVersion>("pipeline_version", asset.AssetType);

                _pipelineVersionsCache.TryAdd(asset.AssetType, pipelineVersion);
            }

            if (pipelineVersion == null)
            {
                return asset.Pipeline[^1];
            }

            try
            {
                var validPipelines = asset.Pipeline
                    .Where(p =>
                    {
                        var maxTrue = pipelineVersion.max == -1 || p.PipelineVersion <= pipelineVersion.max;
                        var minTrue = pipelineVersion.min == -1 || p.PipelineVersion >= pipelineVersion.min;

                        return minTrue && maxTrue;
                    })
                    .ToList();

                if (validPipelines.Count == 0)
                {
                    return null;
                }

                return validPipelines
                    .OrderByDescending(p => Convert.ToInt32(p.PipelineVersion))
                    .FirstOrDefault();
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"[InventoryPipelineService.ResolvePipelineItem] Error resolving pipeline version for asset {asset.AssetId}: {e}");
                return null;
            }
        }
    }
}
