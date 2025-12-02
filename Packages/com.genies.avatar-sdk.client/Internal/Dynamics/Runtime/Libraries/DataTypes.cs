using UnityEngine;

namespace Genies.Components.Dynamics
{
    public struct ParticleData
    {
        public int CollisionEnabled;
        public Vector3 CurrentCollisionCenter;
        public float CollisionRadius;
    }
    
    public struct ParticleDataJobs
    {
        public bool CollisionEnabled;
        public Vector3 CurrentCollisionCenter;
        public Vector3 WorldSpaceCollisionCenter;
        public float ScaledCollisionRadius;
        public Vector3 CurrentPosition;
        public Quaternion Rotation;
        public Vector3 CollisionOffset;
        public Quaternion ModelSpaceRotation;
    }

    public struct SphereColliderData
    {
        public Vector3 Position;
        public float CollisionRadius;
    }

    public struct CapsuleColliderData
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public float CollisionRadius;
    }

    public struct ColliderDataJobs
    {
        public bool IsSphere;
        public Vector3 Center;
        
        public bool IsCapsule;
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        
        public float ScaledCollisionRadius;
    }
}
