using System;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Some value snapping methods.
    /// </summary>
    public static class ValueSnapping
    {
        public enum Method
        {
            None = 0,
            NearestPowerOfTwo = 1,
            LowerPowerOfTwo = 2,
            HigherPowerOfTwo = 3,
            MultipleOf2 = 4,
            MultipleOf4 = 5,
            MultipleOf8 = 6,
            MultipleOf16 = 7,
            MultipleOf32 = 8,
            MultipleOf64 = 9,
            MultipleOf128 = 10,
            MultipleOf256 = 11,
            MultipleOf512 = 12,
        }
        
        public static float SnapTo(float value, Method method)
        {
            return method switch
            {
                Method.None => value,
                Method.NearestPowerOfTwo => SnapToNearestPowerOfTwo(value),
                Method.LowerPowerOfTwo => SnapToLowerPowerOfTwo(value),
                Method.HigherPowerOfTwo => SnapToHigherPowerOfTwo(value),
                Method.MultipleOf2 => SnapToMultipleOf(value, 2.0f),
                Method.MultipleOf4 => SnapToMultipleOf(value, 4.0f),
                Method.MultipleOf8 => SnapToMultipleOf(value, 8.0f),
                Method.MultipleOf16 => SnapToMultipleOf(value, 16.0f),
                Method.MultipleOf32 => SnapToMultipleOf(value, 32.0f),
                Method.MultipleOf64 => SnapToMultipleOf(value, 64.0f),
                Method.MultipleOf128 => SnapToMultipleOf(value, 128.0f),
                Method.MultipleOf256 => SnapToMultipleOf(value, 256.0f),
                Method.MultipleOf512 => SnapToMultipleOf(value, 512.0f),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
            };
        }

        public static float SnapToNearestPowerOfTwo(float value)
        {
            float result = 2.0f;
            while (value > result)
            {
                result *= 2.0f;
            }

            float previous = 0.5f * result;
            return result - value <= value - previous ? result : previous;
        }
        
        public static float SnapToLowerPowerOfTwo(float value)
        {
            float result = 2.0f;
            while (value > result)
            {
                result *= 2.0f;
            }

            return 0.5f * result;
        }
        
        public static float SnapToHigherPowerOfTwo(float value)
        {
            float result = 2.0f;
            while (value > result)
            {
                result *= 2.0f;
            }

            return result;
        }

        public static float SnapToMultipleOf(float value, float divisor)
        {
            return divisor * Mathf.Round(value / divisor);
        }
    }
}
