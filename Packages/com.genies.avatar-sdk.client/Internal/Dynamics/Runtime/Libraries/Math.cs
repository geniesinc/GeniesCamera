using UnityEngine;

namespace Genies.Components.Dynamics
{
    public static class Math
    {
        /// <summary>
        /// The length of the smallest vector that can be reliably normalized.
        /// </summary>
        public const float MinVectorLength = 0.000011f;

        /// <summary>
        /// The corresponding square of <see cref="MinVectorLength"/>
        /// </summary>
        public const float MinVectorLengthSqr = 0.000000000121f;

        /// <summary>
        /// The maximum error in length of a normalized vector within the system.
        /// </summary>
        public const float MaxNormalizationLengthError = 0.000001f;

        /// <summary>
        /// The maximum error in the dot product of a normalized vector with its ideal normalization.
        /// This is a measure of the ability of the system to retain reliable directions of velocities and forces.
        /// </summary>
        public const float MaxNormalizationDotProductError = 0.000001f;

        public static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector3 startToEnd = segmentEnd - segmentStart;
            var segmentLengthSqr = startToEnd.sqrMagnitude;

            // The segment is effectively a point.
            if (segmentLengthSqr < MinVectorLengthSqr)
            {
                return (segmentStart + segmentEnd) * 0.5f;
            }

            Vector3 pointToStart = point - segmentStart;
            var pointDotSegment = Vector3.Dot(pointToStart, startToEnd);

            // Normalized position of the point's projection onto the segment.
            var proportion = Mathf.Clamp(pointDotSegment / segmentLengthSqr, 0f, 1f);

            return segmentStart + startToEnd * proportion;
        }
    }
}
