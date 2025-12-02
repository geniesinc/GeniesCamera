using System.Collections.Generic;
using UnityEngine;

namespace Genies.Components.Dynamics
{
    public static class ColliderConfiguration
    {
        /// <summary>
        /// The location of the collider on the humanoid model.
        /// Some space has been left as this list will likely become more granular and refined.
        /// </summary>
        public enum HumanoidColliderLocation
        {
            // Head, Neck, and Face
            Head = 0,
            Neck = 1,

            // Torso
            Torso = 16,
            Hips = 24,

            // Arms
            LeftUpperArm = 32,
            LeftLowerArm = 33,
            LeftHand = 34,
            RightUpperArm = 48,
            RightLowerArm = 49,
            RightHand = 50,

            // Legs
            LeftUpperLeg = 64,
            LeftLowerLeg = 65,
            RightUpperLeg = 80,
            RightLowerLeg = 81,
        }

        public static Dictionary<HumanoidColliderLocation, string> HumanoidColliderLocationToJointName = new Dictionary<HumanoidColliderLocation, string>
        {
            { HumanoidColliderLocation.Head, "Face" },
            { HumanoidColliderLocation.Neck, "NeckBind" },
            { HumanoidColliderLocation.Torso, "Spine2Bind" },
            { HumanoidColliderLocation.Hips, "HipsBind" },
            { HumanoidColliderLocation.LeftUpperArm, "LeftArmTwist1Bind" },
            { HumanoidColliderLocation.LeftLowerArm, "LeftForeArmTwist1Bind" },
            { HumanoidColliderLocation.LeftHand, "LeftHandBind" },
            { HumanoidColliderLocation.RightUpperArm, "RightArmTwist1Bind" },
            { HumanoidColliderLocation.RightLowerArm, "RightForeArmTwist1Bind" },
            { HumanoidColliderLocation.RightHand, "RightHandBind" },
            { HumanoidColliderLocation.LeftUpperLeg, "LeftUpLegTwist1Bind" },
            { HumanoidColliderLocation.LeftLowerLeg, "LeftLegTwist1Bind" },
            { HumanoidColliderLocation.RightUpperLeg, "RightUpLegTwist1Bind" },
            { HumanoidColliderLocation.RightLowerLeg, "RightLegTwist1Bind" },
        };

        public delegate void ColliderSetupDelegate(DynamicsStructure dynamicStructure, GameObject target);

        public static Dictionary<HumanoidColliderLocation, ColliderSetupDelegate> HumanoidColliderLocationToColliderSetupDelegate = new Dictionary<HumanoidColliderLocation, ColliderSetupDelegate>
        {
            { HumanoidColliderLocation.Head, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.12f, 0.105f, new Vector3(0f, 0.04f, 0f), new Vector3(-23f, 0f, 0f)) },
            { HumanoidColliderLocation.Neck, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.075f, 0.0525f, new Vector3(0f, 0.06f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.Torso, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.15f, 0.125f, new Vector3(0f, 0.04f, 0f), new Vector3(-12f, 0f, 0f)) },
            { HumanoidColliderLocation.Hips, (structure, target) => DynamicsSetup.AddSphereCollider(structure, target, 0.115f, new Vector3(0f, -0.0125f, 0f)) },
            { HumanoidColliderLocation.LeftUpperArm, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.2f, 0.045f, new Vector3(0f, 0.07f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.LeftLowerArm, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.2f, 0.04f, new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.LeftHand, (structure, target) => DynamicsSetup.AddSphereCollider(structure, target, 0.07f, new Vector3(0f, 0.09f, 0.03f)) },
            { HumanoidColliderLocation.RightUpperArm, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.2f, 0.045f, new Vector3(0f, 0.07f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.RightLowerArm, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.2f, 0.04f, new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.RightHand, (structure, target) => DynamicsSetup.AddSphereCollider(structure, target, 0.07f, new Vector3(0f, 0.09f, 0.03f)) },
            { HumanoidColliderLocation.LeftUpperLeg, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.25f, 0.06f, new Vector3(0f, 0.125f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.LeftLowerLeg, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.25f, 0.06f, new Vector3(0f, 0.125f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.RightUpperLeg, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.25f, 0.06f, new Vector3(0f, 0.125f, 0f), new Vector3(0f, 0f, 0f)) },
            { HumanoidColliderLocation.RightLowerLeg, (structure, target) => DynamicsSetup.AddCapsuleCollider(structure, target, 0.25f, 0.06f, new Vector3(0f, 0.125f, 0f), new Vector3(0f, 0f, 0f)) }
        };
    }
}