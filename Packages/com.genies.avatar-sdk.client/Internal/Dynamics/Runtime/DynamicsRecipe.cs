using UnityEngine;
using System.Collections.Generic;
using System;
using static Genies.Components.Dynamics.DynamicsStructure;

namespace Genies.Components.Dynamics
{
    [Serializable]
    public struct ParticleRecipe
    {
        [Tooltip(DynamicsTooltips.TargetObjectName)]
        public string TargetObjectName;

        [Tooltip(DynamicsTooltips.CollisionEnabled)]
        public bool CollisionEnabled;

        [Tooltip(DynamicsTooltips.ParticleCollisionRadius)]
        public float CollisionRadius;

        [Tooltip(DynamicsTooltips.ParticleCollisionOffset)]
        public Vector3 CollisionOffset;

        [Range(0f, 1f)]
        [Tooltip(DynamicsTooltips.PositionAnchor)]
        public float PositionAnchor;

        [Range(0f, 1f)]
        [Tooltip(DynamicsTooltips.RotationAnchor)]
        public float RotationAnchor;

        [Tooltip(DynamicsTooltips.AffectsTransform)]
        public bool AffectsTransform;
    }

    [Serializable]
    public struct LinkRecipe
    {
        [Tooltip(DynamicsTooltips.LinkStartParticleObjectName)]
        public string StartParticleObjectName;

        [Tooltip(DynamicsTooltips.LinkEndParticleObjectName)]
        public string EndParticleObjectName;

        [Tooltip(DynamicsTooltips.LinkMaintainStartRotation)]
        public bool MaintainStartParticleRotation;

        [Tooltip(DynamicsTooltips.LinkMaintainEndRotation)]
        public bool MaintainEndParticleRotation;

        [Range(0f, 1f)]
        [Tooltip(DynamicsTooltips.LinkStretchiness)]
        public float Stretchiness;

        [Range(0f, 1f)]
        [Tooltip(DynamicsTooltips.AnchorToStartParticleRotation)]
        public float AnchorToStartParticleRotation;

        [Range(0f, 1f)]
        [Tooltip(DynamicsTooltips.LinkStretchiness)]
        public float AngleLimiting;
    }

    [Serializable]
    public struct SphereColliderRecipe
    {
        [Tooltip(DynamicsTooltips.TargetObjectName)]
        public string TargetObjectName;

        public float CollisionRadius;

        public Vector3 Offset;
    }

    [Serializable]
    public struct CapsuleColliderRecipe
    {
        [Tooltip(DynamicsTooltips.TargetObjectName)]
        public string TargetObjectName;

        public float CollisionRadius;

        public float Height;

        public Vector3 Offset;

        public Vector3 Rotation;
    }

    /// <summary>
    /// Provides the data necessary to construct a dynamic structure at runtime as well as
    /// a method used to hook into Unity events.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DynamicsRecipe", menuName = "Genies/Dynamics/Dynamics Recipe")]
#endif
    public class DynamicsRecipe : ScriptableObject
    {
        // TODO: Determine if these fields are needed in dynamics recipes generated going forward.
        // These fields are holdovers from the Legacy Bonus Component system.
        public string parentName;
        public string generatedChildName;

        [Tooltip(DynamicsTooltips.StructureName)]
        public string StructureName;

        [Tooltip(DynamicsTooltips.UpdateMethod)]
        public UpdateMethod DynamicsUpdateMethod;

        [Tooltip(DynamicsTooltips.SfpsDynamicsFPS)]
        [Range(1, 500)]
        public float SfpsDynamicsFPS = 60;

        [Tooltip(DynamicsTooltips.SfpsUpdateFPS)]
        [Range(1, 500)]
        public float SfpsUpdateFPS = 15;

        [Range(1, 8)]
        [Tooltip(DynamicsTooltips.Iterations)]
        public int Iterations = 1;

        [Range(0, 5)]
        [Tooltip(DynamicsTooltips.PreWarmTime)]
        public float PreWarmTime = DefaultPreWarmTime;

        public Vector3 Gravity = new(0f, -9.8f, 0f);

        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.Friction)]
        public float Friction;

        public bool ParticleToParticleCollision;

        public ComputeMethod CollisionComputeMethod;

        public List<ParticleRecipe> ParticleRecipes;

        public List<LinkRecipe> LinkRecipes;

        public List<SphereColliderRecipe> SphereColliderRecipes;

        public List<CapsuleColliderRecipe> CapsuleColliderRecipes;
    }
}
