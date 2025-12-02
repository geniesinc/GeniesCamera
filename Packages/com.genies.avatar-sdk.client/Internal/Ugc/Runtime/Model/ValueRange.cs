using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Represents a range of float values with a default value, minimum, and maximum.
    /// This struct provides utility methods for clamping values within the range
    /// and checking if values are within bounds.
    /// </summary>
    public struct ValueRange
    {
        /// <summary>
        /// The default value within this range.
        /// </summary>
        public readonly float Default;
        
        /// <summary>
        /// The minimum value allowed in this range.
        /// </summary>
        public readonly float Min;
        
        /// <summary>
        /// The maximum value allowed in this range.
        /// </summary>
        public readonly float Max;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueRange"/> struct with the specified values.
        /// </summary>
        /// <param name="defaultValue">The default value within the range.</param>
        /// <param name="minValue">The minimum allowed value.</param>
        /// <param name="maxValue">The maximum allowed value.</param>
        public ValueRange(float defaultValue, float minValue, float maxValue)
        {
            Default = defaultValue;
            Min = minValue;
            Max = maxValue;
        }

        /// <summary>
        /// Clamps the specified value to be within the range [Min, Max].
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The clamped value that falls within the range.</returns>
        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Min, Max);
        }

        /// <summary>
        /// Determines whether the specified value is within the range [Min, Max].
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is within the range; otherwise, false.</returns>
        public bool IsInRange(float value)
        {
            return Min <= value && value <= Max;
        }
    }

    /// <summary>
    /// Represents a range of Vector2 values with a default value, minimum, and maximum for each component.
    /// This struct provides utility methods for clamping Vector2 values within the range
    /// and checking if values are within bounds for both X and Y components.
    /// </summary>
    public struct Vector2ValueRange
    {
        /// <summary>
        /// The default Vector2 value within this range.
        /// </summary>
        public readonly Vector2 Default;
        
        /// <summary>
        /// The minimum Vector2 values allowed in this range for each component.
        /// </summary>
        public readonly Vector2 Min;
        
        /// <summary>
        /// The maximum Vector2 values allowed in this range for each component.
        /// </summary>
        public readonly Vector2 Max;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector2ValueRange"/> struct with the specified Vector2 values.
        /// </summary>
        /// <param name="defaultValue">The default Vector2 value within the range.</param>
        /// <param name="minValue">The minimum allowed Vector2 values for each component.</param>
        /// <param name="maxValue">The maximum allowed Vector2 values for each component.</param>
        public Vector2ValueRange(Vector2 defaultValue, Vector2 minValue, Vector2 maxValue)
        {
            Default = defaultValue;
            Min = minValue;
            Max = maxValue;
        }

        /// <summary>
        /// Clamps the specified Vector2 value so that each component is within its respective range.
        /// </summary>
        /// <param name="value">The Vector2 value to clamp.</param>
        /// <returns>A new Vector2 with each component clamped within its respective range.</returns>
        public Vector2 Clamp(Vector2 value)
        {
            return new Vector2(Mathf.Clamp(value.x, Min.x, Max.x), Mathf.Clamp(value.y, Min.y, Max.y));
        }

        /// <summary>
        /// Determines whether the specified Vector2 value has both components within their respective ranges.
        /// </summary>
        /// <param name="value">The Vector2 value to check.</param>
        /// <returns>True if both X and Y components are within their respective ranges; otherwise, false.</returns>
        public bool IsInRange(Vector2 value)
        {
            return Min.x <= value.x && value.x <= Max.x
                && Min.y <= value.y && value.y <= Max.y;
        }
    }

}
