// Summary: Mathematics library for use in Genies Dynamics
// Copyright: Genies, Inc.

#ifndef GENIES_DYNAMICS_MATH
#define GENIES_DYNAMICS_MATH

// The length of the smallest vector that can be reliably normalized.
#define MinVectorLength    (0.000011f)
// Squared value of MinVectorLength
#define MinVectorLengthSqr (0.000000000121f)

inline float3 ClosestPointOnSegment (float3 pointer, float3 segmentStart, float3 segmentEnd)
{
    float3 startToEnd = segmentEnd - segmentStart;
    float segmentLengthSqr = dot(startToEnd, startToEnd);

    if (segmentLengthSqr < MinVectorLengthSqr) return (segmentStart + segmentEnd) * 0.5f;

    float3 pointToStart = pointer - segmentStart;
    float pointDotSegment = dot(pointToStart, startToEnd);

    // Normalized position of the point's projection onto the segment.
    float proportion = saturate(pointDotSegment / segmentLengthSqr);

    return segmentStart + startToEnd * proportion;
}

#endif
