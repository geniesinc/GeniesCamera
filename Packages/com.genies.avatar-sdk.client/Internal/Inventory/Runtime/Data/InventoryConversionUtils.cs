using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory
{
    public static class InventoryConversionUtils
    {
        public static Color ToUnityColor(this HexColor colorHex)
        {
            if (!string.IsNullOrEmpty(colorHex.Hex))
            {
                if (ColorUtility.TryParseHtmlString(colorHex.Hex, out var parsed))
                {
                    return parsed;
                }
            }

            // fallback to r/g/b/a
            float r = colorHex.R ?? 0f;
            float g = colorHex.G ?? 0f;
            float b = colorHex.B ?? 0f;
            float a = colorHex.A ?? 1f;

            // if API uses 0–255, normalize to 0–1
            if (r > 1f || g > 1f || b > 1f || a > 1f)
            {
                r /= 255f;
                g /= 255f;
                b /= 255f;
                a /= 255f;
            }

            return new Color(r, g, b, a);
        }
    }
}
