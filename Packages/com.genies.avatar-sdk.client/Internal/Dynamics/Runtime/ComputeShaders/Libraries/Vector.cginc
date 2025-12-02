// Summary: Vector arithmetic library for use in Genies Dynamics
// Copyright: Genies, Inc.

#ifndef GENIES_DYNAMICS_VECTOR
#define GENIES_DYNAMICS_VECTOR

#include "Math.cginc"

// Constants
inline float3 unit_x() { return float3(1.0f, 0.0f, 0.0f); }
inline float3 unit_y() { return float3(0.0f, 1.0f, 0.0f); }
inline float3 unit_z() { return float3(0.0f, 0.0f, 1.0f); }

inline float3 Normalize(float3 v, float3 fallback)
{
	float vv = dot(v, v);
	return vv > MinVectorLengthSqr ? v / sqrt(vv) : fallback;
}

inline float3 Normalize(float3 v)
{
	return Normalize(v, unit_z());
}

#endif
