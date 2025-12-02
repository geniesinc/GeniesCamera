using System;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Represents a pixel density for a surface, with min/max values and snapping options.
    /// </summary>
    [Serializable]
    public struct SurfacePixelDensity
    {
        [Tooltip("The target pixel area density. Represents how many square pixels per world-space square meters a surface should have. I.e.: a value of 1024 for a 2 square meter surface would result in 2048 square pixels")]
        public float targetDensity;
        public int minPixelArea;
        public int maxPixelArea;
        public ValueSnapping.Method snappingMethod;

        /// <summary>
        /// Given a mapped mesh world-space area by UV space area gets back the texture size required for a texture map
        /// to make the mesh surface meet this pixel density. The texture should apply the returned size for
        /// its width and heigh.
        /// </summary>
        public int CalculateTextureSize(float squareMetersPerSquareUVs)
        {
            int size = Mathf.RoundToInt(ValueSnapping.SnapTo(squareMetersPerSquareUVs * targetDensity, snappingMethod));
            return Mathf.Clamp(size, minPixelArea, maxPixelArea);
        }
        
        /// <summary>
        /// Given a mapped mesh world-space area by UV space area gets back the required pixel area required for a
        /// texture map to make the mesh surface meet this pixel density. The texture should apply a width and height
        /// so that its area equals the returned value.
        /// </summary>
        public float CalculateTexturePixelArea(float squareMetersPerSquareUVs)
        {
            float size = ValueSnapping.SnapTo(squareMetersPerSquareUVs * targetDensity, snappingMethod);
            size = Mathf.Clamp(size, minPixelArea, maxPixelArea);
            return size * size;
        }
    }
}
