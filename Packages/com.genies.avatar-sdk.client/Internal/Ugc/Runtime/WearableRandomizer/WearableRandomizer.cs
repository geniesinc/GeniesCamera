using System.Collections.Generic;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Utility class that can randomize <see cref="Wearable"/> and <see cref="Split"/> definitions.
    /// The randomization uses color theory and other techniques to generate appealing color patterns.
    /// <br/><br/>
    /// These are the randomizing steps performed for each split:
    /// <list type="number">
    /// <item>Either use the provided base color or randomly generate an attractive curated base color (not too saturated or too bright, etc).</item>
    /// <item>Generate 4 colors (for each split region) based on rules from color theory.</item>
    /// <item>Randomly set patterns to some of the regions, based on proximity of their dominant colors to the generated color for their region.</item>
    /// <item>Set materials to all the regions from 3 randomly selected materials (2 basic and 1 accent materials).</item>
    /// </list>
    /// </summary>
    public static class WearableRandomizer
    {
        private const string _defaultDataResourcesPath = "WearableRandomizerData";
        private static WearableRandomizationSettings? _defaultSettings;
        private static readonly List<Split> _splits = new();// helper to avoid heap allocations

        /// <summary>
        /// Default randomization settings to be used when none are provided.
        /// </summary>
        public static WearableRandomizationSettings DefaultSettings
        {
            get => _defaultSettings ??= new WearableRandomizationSettings
            {
                RandomizeMaterials = true,
                BaseColor = null, // generate a random one
                Data = LoadDefaultData(),
            };

            set => _defaultSettings = value;
        }

        /// <summary>
        /// Randomizes all the splits from the given wearable.
        /// </summary>
        /// <param name="wearable">The given wearable to randomize its splits.</param>
        /// <param name="settings">If null, <see cref="DefaultSettings"/> will be used.</param>
        public static void RandomizeWearable(Wearable wearable, WearableRandomizationSettings? settings = null)
        {
            RandomizeSplits(wearable.Splits, settings);
        }

        /// <summary>
        /// Randomizes the given splits.
        /// </summary>
        /// <param name="splits">List of splits to be randomized.</param>
        /// <param name="settings">If null, <see cref="DefaultSettings"/> will be used.</param>
        public static void RandomizeSplits(IEnumerable<Split> splits, WearableRandomizationSettings? settings = null)
        {
            if (splits is null)
            {
                return;
            }

            _splits.Clear();
            _splits.AddRange(splits);

            if (_splits.Count == 0)
            {
                return;
            }

            WearableRandomizationSettings validatedSettings = GetValidatedSettings(settings);

            // find the split with the highest number of regions and generate randomized that for that region count
            var regionCount = 0;
            foreach (Split split in _splits)
            {
                if (split?.Regions?.Count > regionCount)
                {
                    regionCount = split.Regions.Count;
                }
            }

            // we will generate split randomized data once, and use it for all the splits
            GetRandomizedSplitData(validatedSettings, regionCount,
                out RegionTextures materials, out RegionTextures patterns, out RegionColors colors);

            // update wearable splits
            foreach (Split split in _splits)
            {
                UpdateSplit(split, materials, patterns, colors, validatedSettings.RandomizeMaterials);
            }

            _splits.Clear();
        }

        /// <summary>
        /// Randomizes the given split.
        /// </summary>
        /// <param name="split">The given split to be randomized.</param>
        /// <param name="settings">If null, <see cref="DefaultSettings"/> will be used.</param>
        public static void RandomizeSplit(Split split, WearableRandomizationSettings? settings = null)
        {
            if (split?.Regions is null || split.Regions.Count == 0)
            {
                return;
            }

            WearableRandomizationSettings validatedSettings = GetValidatedSettings(settings);

            // generate randomized data for the split
            GetRandomizedSplitData(validatedSettings, split.Regions.Count,
                out RegionTextures materials, out RegionTextures patterns, out RegionColors colors);

            // update the split with the randomized data
            UpdateSplit(split, materials, patterns, colors, validatedSettings.RandomizeMaterials);
        }

        // generates the split randomization for a given region count
        private static void GetRandomizedSplitData(
            WearableRandomizationSettings settings,
            int regionCount,
            out RegionTextures materials,
            out RegionTextures patterns,
            out RegionColors colors)
        {
            // generate colors
            Color baseColor = settings.BaseColor ?? CuratedColorGenerator.GetRandomCuratedColor();
            colors = CuratedColorGenerator.GetRandomCuratedRegionColors(baseColor, regionCount);

            // generate materials and patterns
            materials = settings.RandomizeMaterials ? CuratedMaterialGenerator.GetRandomMaterials(regionCount, settings.Data) : default;
            patterns = CuratedPatternGenerator.GetRandomPatterns(colors, regionCount, settings.Data);
        }

        // updates the given split with the given randomization data
        private static void UpdateSplit(Split split, RegionTextures materials, RegionTextures patterns, RegionColors colors, bool randomizeMaterials)
        {
            // make sure custom colors are enabled
            split.UseDefaultColors = false;

            // update materials
            if (randomizeMaterials)
            {
                foreach (Region region in split.Regions)
                {
                    int regionIndex = region.RegionNumber - 1;
                    region.Style.SurfaceTextureId = materials.GetId(regionIndex);
                    region.Style.SurfaceScale = materials.GetScale(regionIndex);
                }
            }

            // update patterns
            foreach (Region region in split.Regions)
            {
                // get the pattern and make sure that it is of the textured type
                Pattern pattern = region.Style.Pattern;
                pattern.Type = PatternType.Textured;

                // assign the generated pattern id and scale
                int regionIndex = region.RegionNumber - 1;
                pattern.TextureId = patterns.GetId(regionIndex);
                pattern.Scale = patterns.GetScale(regionIndex);
            }

            // update colors
            foreach (Region region in split.Regions)
            {
                region.Style.Color = colors[region.RegionNumber - 1];
            }
        }

        private static WearableRandomizationSettings GetValidatedSettings(WearableRandomizationSettings? settings)
        {
            if (!settings.HasValue)
            {
                return DefaultSettings;
            }

            WearableRandomizationSettings validatedSettings = settings.Value;
            validatedSettings.Data ??= DefaultSettings.Data; // ensure we have data
            return validatedSettings;
        }

        private static WearableRandomizerData LoadDefaultData()
        {
            // load default data json from resources
            using Ref<TextAsset> defaultDataRef = ResourcesUtility.LoadAsset<TextAsset>(_defaultDataResourcesPath);

            if (!defaultDataRef.IsAlive)
            {
                Debug.LogError($"[{nameof(WearableRandomizer)}] couldn't load default data from the resources path: {_defaultDataResourcesPath}");
                return null;
            }

            var data = WearableRandomizerData.Deserialize(defaultDataRef.Item.text);
            return data;
        }
    }
}
