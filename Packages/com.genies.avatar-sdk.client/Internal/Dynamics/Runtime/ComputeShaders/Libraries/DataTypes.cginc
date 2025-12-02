// Summary: Data structure library for use in Genies Dynamics. These represent 3D objects for use in the simulation.
// Copyright: Genies, Inc.

#ifndef GENIES_DYNAMICS_DATA_TYPES
#define GENIES_DYNAMICS_DATA_TYPES

struct ParticleData
{
	int CollisionEnabled;
	float3 CurrentCollisionCenter;
	float CollisionRadius;
};

struct SphereColliderData
{
	float3 CurrentPosition;
	float CollisionRadius;
};

struct CapsuleColliderData
{
	float3 StartPosition;
	float3 EndPosition;
	float CollisionRadius;
};

#endif
