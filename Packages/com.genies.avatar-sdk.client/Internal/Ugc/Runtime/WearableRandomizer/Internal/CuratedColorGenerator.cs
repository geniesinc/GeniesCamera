using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Color generator utility for the <see cref="WearableRandomizer"/>.
    /// </summary>
    internal static class CuratedColorGenerator
    {
        /// <summary>
        /// Generates a random curated color.
        /// </summary>
        public static Color GetRandomCuratedColor()
        {
            // generate a random color, randomly darken it and curate it
            Color color = GenerateRandomColor();
            color = DarkenColor(color);
            color = CurateColor(color);

            return color;
        }

        /// <summary>
        /// Generates four random region curated colors from a given base color and the number of regions.
        /// </summary>
        public static RegionColors GetRandomCuratedRegionColors(Color baseColor, int regionCount)
        {
            RegionColors regionColors;

            if (IsGrayscale(baseColor))
            {
                // RULE: GRAYSCALE
                regionColors = Grayscale(baseColor);
            }
            else
            {
                // RULE: ALTERNATE COMPLEMENTARY (20% chances) & REVERSED ALTERNATE COMPLEMENTARY (80% chances)
                if (Random.value <= 0.80f)
                {
                    regionColors = ReversedAlternateComplementary(baseColor);
                }
                else
                {
                    regionColors = AlternateComplementary(baseColor);
                }
            }

            // BOOST ACCENT
            regionColors = new RegionColors
            {
                Color1 = regionColors.Color1,
                Color2 = AccentBooster(regionColors.Color1, regionColors.Color2),
                Color3 = regionColors.Color3,
                Color4 = regionColors.Color4,
            };

            // REMOVE ACCENT (make accent similar to base color)
            regionColors = AccentChances(regionColors.Color1, regionColors.Color2, regionColors.Color3, regionColors.Color4);
            // ADD OVERLAY
            regionColors = ColorOverlay(regionColors.Color1, regionColors.Color2, regionColors.Color3, regionColors.Color4);
            // SHUFFLE COLORS: ACCENT TO LAST REGION
            regionColors = MoveAccentToLastRegion(regionColors.Color1, regionColors.Color2, regionColors.Color3, regionColors.Color4, regionCount);

            return regionColors;
        }

        private static Color GenerateRandomColor()
        {
            return new Color
            {
                r = Random.Range(0f, 1f),
                g = Random.Range(0f, 1f),
                b = Random.Range(0f, 1f),
            };
        }

        private static Color DarkenColor(Color color)
        {
            // 35% chance to darken the color by 0.5
            if (Random.value > 0.35f)
            {
                return color;
            }

            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Max(0.1f, v - 0.5f);

            return Color.HSVToRGB(h, s, v);
        }

        private static Color CurateColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            // Desaturate all 10%
            s -= 0.1f;

            // Desaturate all 10%
            if (Random.Range(0f, 1f) < 0.5)
            {
                s = s / 2.0f;
            }

            // Clamp v from 0.1 to 0.9
            v = Mathf.Clamp(v, 0.1f, 0.9f);

            // If s > 0.7, then s = 0.7
            if (s > 0.7f)
            {
                s = 0.7f;
            }

            // If s < 0.2, s = 0
            if (s < 0.2f)
            {
                s = 0f;
            }

            // Adjust S and V based on H
            float maxS = 0.65f, maxV = 0.65f; // default for GREEN
            switch (h)
            {
                case >= 1 / 6f and < 1 / 3f:
                    // YELLOW
                    maxS = 0.60f;
                    maxV = 0.90f;
                    break;
                case >= 1 / 3f and < 0.5f:
                    // RED
                    maxS = 0.70f;
                    maxV = 0.75f;
                    break;
                case >= 0.5f and < 2 / 3f:
                    // MAGENTA
                    maxS = 0.70f;
                    maxV = 0.75f;
                    break;
                case >= 2 / 3f and < 5 / 6f:
                    // BLUE
                    maxS = 0.80f;
                    maxV = 0.80f;
                    break;
                case >= 5 / 6f:
                    // CYAN
                    maxS = 0.75f;
                    maxV = 0.70f;
                    break;
            }

            s = Mathf.Min(s, maxS);
            v = Mathf.Min(v, maxV);

            // Ensure that s + v is not higher than 1.45
            if (s + v > 1.45f)
            {
                float excess = s + v - 1.45f;
                s -= excess / 2;
                v -= excess / 2;

                // Ensure s and v are not less than 0
                s = Mathf.Max(0, s);
                v = Mathf.Max(0, v);
            }

            return Color.HSVToRGB(h, s, v);
        }

        private static RegionColors Grayscale(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            Color colorA = color;
            Color colorB, colorC, accent;

            // Set colorB and colorC
            if (v > 0.7f)
            {
                colorB = Color.HSVToRGB(h, s, Mathf.Clamp01(v - 0.2f));
                colorC = Color.HSVToRGB(h, s, Mathf.Clamp01(v - 0.4f));
            }
            else if (v < 0.3f)
            {
                colorB = Color.HSVToRGB(h, s, Mathf.Clamp01(v + 0.2f));
                colorC = Color.HSVToRGB(h, s, Mathf.Clamp01(v + 0.4f));
            }
            else
            {
                colorB = Color.HSVToRGB(h, s, Mathf.Clamp01(v + 0.2f));
                colorC = Color.HSVToRGB(h, s, Mathf.Clamp01(v - 0.4f));
            }

            // Set accent color
            if (v > 0.7f)
            {
                accent = Random.value < 0.5f ? Color.HSVToRGB(0f, 0f, 0.1f) : Color.HSVToRGB(Random.value, 0.8f, 0.2f);
            }
            else if (v < 0.3f)
            {
                accent = Random.value < 0.5f ? Color.HSVToRGB(0f, 0f, 0.9f) : Color.HSVToRGB(Random.value, 0.8f, 0.8f);
            }
            else
            {
                int randomOption = Random.Range(0, 4);
                if (randomOption == 0)
                {
                    accent = Color.HSVToRGB(0f, 0f, 0.1f);
                }
                else if (randomOption == 1)
                {
                    accent = Color.HSVToRGB(0f, 0f, 0.9f);
                }
                else if (randomOption == 2)
                {
                    accent = Color.HSVToRGB(Random.value, 0.8f, 0.2f);
                }
                else
                {
                    accent = Color.HSVToRGB(Random.value, 0.8f, 0.8f);
                }
            }

            return new RegionColors(colorA, accent, colorB, colorC);
        }

        private static RegionColors AlternateComplementary(Color color)
        {
            Color.RGBToHSV(color, out float baseH, out float baseS, out float baseV);

            float complementaryH = (baseH + 0.5f) % 1f;
            Color complementary = Color.HSVToRGB(complementaryH, baseS, baseV);

            float leftAdjacentH = (complementaryH + 1f / 12f) % 1f;
            Color leftAdjacent = Color.HSVToRGB(leftAdjacentH, baseS, baseV);

            float rightAdjacentH = (complementaryH - 1f / 12f + 1f) % 1f;
            Color rightAdjacent = Color.HSVToRGB(rightAdjacentH, baseS, baseV);

            Color complementaryRand = CuratedRandomizeColor(complementary);
            Color leftAdjacentRand = CuratedRandomizeColor(leftAdjacent);
            Color rightAdjacentRand = CuratedRandomizeColor(rightAdjacent);

            return new RegionColors(color, complementaryRand, leftAdjacentRand, rightAdjacentRand);
        }

        private static RegionColors ReversedAlternateComplementary(Color color)
        {
            Color.RGBToHSV(color, out float baseH, out float baseS, out float baseV);

            float complementaryH = (baseH + 0.5f) % 1f;
            Color complementary = Color.HSVToRGB(complementaryH, baseS, baseV);

            float leftAdjacentH = (baseH + 1f / 12f) % 1f;
            Color leftAdjacent = Color.HSVToRGB(leftAdjacentH, baseS, baseV);

            float rightAdjacentH = (baseH - 1f / 12f + 1f) % 1f;
            Color rightAdjacent = Color.HSVToRGB(rightAdjacentH, baseS, baseV);

            Color complementaryRand = CuratedRandomizeColor(complementary);
            Color leftAdjacentRand = CuratedRandomizeColor(leftAdjacent);
            Color rightAdjacentRand = CuratedRandomizeColor(rightAdjacent);

            return new RegionColors(color, complementaryRand, leftAdjacentRand, rightAdjacentRand);
        }

        private static Color AccentBooster(Color color, Color accent)
        {
            // Convert color and accent to HSV
            Color.RGBToHSV(color, out float baseH, out float baseS, out float baseV);
            Color.RGBToHSV(accent, out float accentH, out float accentS, out float accentV);


            // 50% chance to be Color, or Black or White
            if (Random.value <= 0.5f)
            {
                // 50% chance to invert accent's V if color's V is bigger than 0.7 or smaller than 0.3
                if (Random.value <= 0.5f)
                {
                    if (baseV > 0.7f || baseV < 0.3f)
                    {
                        accentV = 1f - accentV;
                        // Ensure V is at least 0.35
                        accentV = Mathf.Max(accentV, 0.35f);
                    }
                    else
                    {
                        // If V is fairly neutral, it will have 0.3 added or subtracted, all clamped between 0.1 and 0.9
                        accentV = Mathf.Clamp(accentV + (Random.value < 0.5f ? 0.3f : -0.3f), 0.1f, 0.9f);
                    }
                }
                else
                {
                    accentS = 0.9f;
                }
            }
            else
            {
                // BLACK OR WHITE
                accentS = 0f;
                accentV = (Random.value <= 0.5f) ? 0.9f : 0.1f;
            }

            // Convert the modified HSV values back to a new color and return it
            Color boostedAccent = Color.HSVToRGB(accentH, accentS, accentV);
            return boostedAccent;
        }

        private static RegionColors AccentChances(Color color, Color accent, Color adjA, Color adjB)
        {
            // 50% chance for complementary to become the same color as the base
            if (Random.value <= 0.5f)
            {
                accent = CuratedRandomizeColor(color);
            }

            return new RegionColors(color, accent, adjA, adjB);
        }

        private static RegionColors ColorOverlay(Color color, Color accent, Color adjA, Color adjB)
        {
            // Convert the color to HSV
            Color.RGBToHSV(color, out float h, out float s, out float v);

            if (s == 0.0f)
            {
                return new RegionColors(color, accent, adjA, adjB);
            }

            // Rotate the hue by a random value between -15 and 15 degrees
            h += Random.Range(-15, 16) / 360.0f;
            h = Mathf.Clamp01(h);

            // Set the S and V values of the overlay color to 0.75
            s = 0.75f;
            v = 0.75f;

            // Convert the modified HSV values back to a new color
            Color overlayColor = Color.HSVToRGB(h, s, v);

            // Lerp the overlay color on top of each input color by 20%
            float lerpAmount = 0.2f;
            Color colorOverlay = Color.Lerp(color, overlayColor, lerpAmount);
            Color accentOverlay = Color.Lerp(accent, overlayColor, lerpAmount);
            Color adjAOverlay = Color.Lerp(adjA, overlayColor, lerpAmount);
            Color adjBOverlay = Color.Lerp(adjB, overlayColor, lerpAmount);

            return new RegionColors(colorOverlay, accentOverlay, adjAOverlay, adjBOverlay);
        }

        private static RegionColors MoveAccentToLastRegion(Color color, Color accent, Color adjA, Color adjB, int regionCount)
        {
            return regionCount switch
            {
                1 => new RegionColors(color, accent, adjA, adjB),
                2 => new RegionColors(color, accent, adjA, adjB),
                3 => new RegionColors(color, adjA, accent, adjB),
                4 => new RegionColors(color, adjA, adjB, accent),
                _ => new RegionColors(color, accent, adjA, adjB),
            };
        }

        /**
         * Randomizes the given color in a curated way. It shifts the hue (not too much) and also modifies
         * its brightness and saturation within some limits.
         */
        private static Color CuratedRandomizeColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            h += Random.Range(-15, 16) / 360.0f;
            h = Mathf.Clamp01(h);

            s += Random.Range(-0.5f, 0.2f);
            s = Mathf.Clamp(s, 0.2f, 0.7f);

            v += Random.Range(-0.5f, 0.2f);
            v = Mathf.Clamp(v, 0.2f, 0.7f);

            return Color.HSVToRGB(h, s, v);
        }

        private static bool IsGrayscale(Color color)
        {
            return Mathf.Approximately(color.r, color.g) && Mathf.Approximately(color.g, color.b);
        }
    }
}
