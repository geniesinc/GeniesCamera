using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Ugc
{
    /// <summary>
    /// Creates default <see cref="Wearable"/> definitions from a template ID. Useful if you
    /// don't have a wearable definition coming from a user's account and you just want to
    /// instantiate a wearable from a template with all defaults.
    /// </summary>
    public sealed class DefaultWearableCreator
    {
        // dependencies
        private readonly IAssetLoader<UgcTemplateAsset> _ugcTemplateLoader;

        /// <summary>
        /// If no UGC template loader is provided you will only be able to create default
        /// wearables from <see cref="UgcTemplateAsset"/> instances directly.
        /// </summary>
        public DefaultWearableCreator(IAssetLoader<UgcTemplateAsset> ugcTemplateLoader = null)
        {
            _ugcTemplateLoader = ugcTemplateLoader;
        }

        public async UniTask<Wearable> CreateAsync(string templateId)
        {
            if (_ugcTemplateLoader is null)
            {
                Debug.LogError($"[{nameof(DefaultWearableCreator)}] cannot create a default wearable from a template ID since no UGC template loader was provided");
                return null;
            }

            using Ref<UgcTemplateAsset> templateRef = await _ugcTemplateLoader.LoadAsync(templateId);
            if (!templateRef.IsAlive)
            {
                return null;
            }

            return Create(templateRef.Item);
        }

        public Wearable Create(UgcTemplateAsset template)
        {
            if (template is null)
            {
                return null;
            }

            IReadOnlyList<UgcTemplateSplitData> splitsData = template.Data.Splits;
            var wearable = new Wearable
            {
                TemplateId = template.Data.TemplateId,
                Tags = new HashSet<string>(),
                Splits = new List<Split>(splitsData.Count),
            };

            foreach (UgcTemplateSplitData splitData in splitsData)
            {
                wearable.Splits.Add(CreateDefaultSplit(splitData));
            }

            foreach (Split split in wearable.Splits)
            {
                split.UseDefaultColors = true;
            }

            return wearable;
        }

        private static Split CreateDefaultSplit(UgcTemplateSplitData splitData)
        {
            UgcTemplateElementData defaultElementData = splitData.Elements.FirstOrDefault();
            if (defaultElementData is null)
            {
                return null;
            }

            return new Split
            {
                MaterialVersion = defaultElementData.MaterialVersion,
                ElementId = defaultElementData.ElementId,
                Regions = CreateDefaultRegions(defaultElementData),
                // use default colors for the default splits since we are creating a default template asset with no styling
                UseDefaultColors = true,
            };
        }

        private static List<Region> CreateDefaultRegions(UgcTemplateElementData elementData)
        {
            var regions = new List<Region>(elementData.Regions);

            for (int regionIndex = 0; regionIndex < elementData.Regions; ++regionIndex)
            {
                regions.Add(CreateDefaultRegion(regionIndex, elementData.MaterialVersion));
            }

            return regions;
        }

        private static Region CreateDefaultRegion(int regionIndex, string materialVersion)
        {
            var pattern = new Pattern { Type = PatternType.Textured, TextureId = string.Empty };

            var style = new Style
            {
                MaterialVersion = materialVersion,
                SurfaceTextureId = string.Empty,
                SurfaceScale = 5.0f,
                Pattern = pattern,
            };

            return new Region { RegionNumber = regionIndex + 1, Style = style };
        }
    }
}
