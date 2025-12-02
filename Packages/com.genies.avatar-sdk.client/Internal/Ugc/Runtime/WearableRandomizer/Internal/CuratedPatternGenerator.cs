using System.Collections.Generic;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Pattern generator utility for the <see cref="WearableRandomizer"/>.
    /// </summary>
    internal static class CuratedPatternGenerator
    {
        private static readonly int[] _randomRegionIndices = { 0, 1, 2, 3 };

        public static RegionTextures GetRandomPatterns(RegionColors colors, int regionCount, WearableRandomizerData data)
        {
            // default to no pattern and 10.0 scale (it is the official default used in the MegaShader)
            var patterns = new RegionTextures(null, 10.0f, null, 10.0f, null, 10.0f, null, 10.0f);

            // based on how many regions are available get a randomized count of patterns that we will add (the rest will be just colored regions)
            int patternCount = GetRandomPatternCount(regionCount);

            // select N regions randomly to assign random patterns to them (where N is the decided patternCount)
            int[] randomRegionIndices = GetRandomRegionIndices();
            for (int i = 0; i < patternCount; i++)
            {
                int regionIndex = randomRegionIndices[i];

                // randomize pattern ID and scale
                Color regionColor = colors[regionIndex];
                string patternId = GetRandomPatternIdCloseToColor(regionColor, data);
                float scale = Mathf.Clamp(30.0f - Mathf.Sqrt(Random.Range(1.0f, 30.0f * 30.0f)), 2.0f, 30.0f);
                patterns.SetId(regionIndex, patternId);
                patterns.SetScale(regionIndex, scale);
            }

            return patterns;
        }

        // given a number of regions returns a random pattern count to use
        private static int GetRandomPatternCount(int regionCount)
        {
            switch (regionCount)
            {
                case 1: case 2:
                    return Random.value <= 0.5f ? 1 : 0;

                case 3:
                    return Random.value switch
                    {
                        <= 0.40f => 0,
                        <= 0.90f => 1,
                        _ => 2,
                    };

                case 4:
                    return Random.value switch
                    {
                        <= 0.35f => 0,
                        <= 0.85f => 1,
                        _ => 2,
                    };

                default:
                    return 0;
            }
        }

        private static int[] GetRandomRegionIndices()
        {
            // perform 4 random switches to the region indices array and return it
            for (int i = 0; i < _randomRegionIndices.Length; ++i)
            {
                int switchWithIndex = Random.Range(0, _randomRegionIndices.Length);
                (_randomRegionIndices[i], _randomRegionIndices[switchWithIndex]) = (_randomRegionIndices[switchWithIndex], _randomRegionIndices[i]);
            }

            return _randomRegionIndices;
        }

        /*
         * Gets a random pattern ID from the available patterns by taking into account the given color.
         * At least one of the dominant colors of the returned pattern will be close to the given color.
         */
        private static string GetRandomPatternIdCloseToColor(Color color, WearableRandomizerData data)
        {
            var colorDistances = new List<(string path, float distance)>();

            foreach (WearableRandomizerData.Pattern pattern in data.Patterns)
            {
                foreach (Color dominantColor in pattern.DominantColors)
                {
                    float distance = GetColorDistance(color, dominantColor);
                    colorDistances.Add((pattern.Id, distance));
                }
            }

            // get the closest patterns first and pick a random pattern among the first 4 patterns
            colorDistances.Sort((x, y) => x.distance.CompareTo(y.distance));
            int randomIndex = Random.Range(0, Mathf.Min(4, colorDistances.Count));
            return colorDistances[randomIndex].path;
        }

        private static float GetColorDistance(Color c1, Color c2)
        {
            return Mathf.Sqrt(Mathf.Pow(c1.r - c2.r, 2) + Mathf.Pow(c1.g - c2.g, 2) + Mathf.Pow(c1.b - c2.b, 2));
        }
    }
}
