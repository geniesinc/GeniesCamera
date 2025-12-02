using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed partial class BodyAttributesConfig
    {
        /// <summary>
        /// For those joints mapped to unified genie bones with scale enabled, this will automatically set their
        /// scaleLateUpdate setting enabled.
        /// </summary>
        [ContextMenu("Enable LateScaleUpdate for unified human bones")]
        public void EnableLateScaleUpdateForUnifiedHumanBones()
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                for (int j = 0; j < attributes[i].joints.Count; ++j)
                {
                    Joint joint = attributes[i].joints[j];
                    if (!joint.enableScale || (!UnifiedHumanBones.Contains(joint.name) && !UnifiedHumanPaths.Contains(joint.name)))
                    {
                        continue;
                    }

                    Debug.Log($"Enabling ScaleLateUpdate for the <color=orange>{joint.name}</color> joint in the <color=yellow>{attributes[i].name}</color> attribute");
                    joint.scaleLateUpdate = true;
                    attributes[i].joints[j] = joint;
                }
            }
        }
        
        /// <summary>
        /// For those joints with scale enabled, this will automatically set their scaleLateUpdate setting enabled.
        /// </summary>
        [ContextMenu("Enable LateScaleUpdate for all")]
        public void EnableLateScaleUpdateForAll()
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                for (int j = 0; j < attributes[i].joints.Count; ++j)
                {
                    Joint joint = attributes[i].joints[j];
                    if (!joint.enableScale)
                    {
                        continue;
                    }

                    Debug.Log($"Enabling ScaleLateUpdate for the <color=orange>{joint.name}</color> joint in the <color=yellow>{attributes[i].name}</color> attribute");
                    joint.scaleLateUpdate = true;
                    attributes[i].joints[j] = joint;
                }
            }
        }
        
        /// <summary>
        /// For those joints with scale enabled, this will automatically set their scaleLateUpdate setting disabled.
        /// </summary>
        [ContextMenu("Disable LateScaleUpdate for all")]
        public void DisableLateScaleUpdateForAll()
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                for (int j = 0; j < attributes[i].joints.Count; ++j)
                {
                    Joint joint = attributes[i].joints[j];
                    if (!joint.enableScale)
                    {
                        continue;
                    }

                    Debug.Log($"Disabling ScaleLateUpdate for the <color=orange>{joint.name}</color> joint in the <color=yellow>{attributes[i].name}</color> attribute");
                    joint.scaleLateUpdate = false;
                    attributes[i].joints[j] = joint;
                }
            }
        }
        
        private static readonly HashSet<string> UnifiedHumanBones = new()
        {
            "Hips",
            "LeftUpLeg",
            "RightUpLeg",
            "LeftLeg",
            "RightLeg",
            "LeftFoot",
            "RightFoot",
            "Spine1",
            "Spine2",
            "Neck",
            "Head",
            "LeftShoulder",
            "RightShoulder",
            "LeftArm",
            "RightArm",
            "LeftForeArm",
            "RightForeArm",
            "LeftHand",
            "RightHand",
            "LeftToeBase",
            "RightToeBase",
            "LeftHandThumb1",
            "LeftHandThumb2",
            "LeftHandThumb3",
            "LeftHandIndex1",
            "LeftHandMiddle1",
            "LeftHandMiddle2",
            "LeftHandMiddle3",
            "LeftHandRing1",
            "LeftHandRing2",
            "LeftHandRing3",
            "LeftHandPinky1",
            "LeftHandPinky2",
            "LeftHandPinky3",
            "RightHandThumb1",
            "RightHandThumb2",
            "RightHandThumb3",
            "RightHandIndex1",
            "RightHandMiddle1",
            "RightHandMiddle2",
            "RightHandMiddle3",
            "RightHandRing1",
            "RightHandRing2",
            "RightHandRing3",
            "RightHandPinky1",
            "RightHandPinky2",
            "RightHandPinky3",
            "Chest",
            "LeftHandIndex2",
            "LeftHandIndex3",
            "RightHandIndex2",
            "RightHandIndex3",
        };
        
        private static readonly HashSet<string> UnifiedHumanPaths = new()
        {
            "Root/Global/Position/Hips",
            "Root/Global/Position/Hips/LeftUpLeg",
            "Root/Global/Position/Hips/RightUpLeg",
            "Root/Global/Position/Hips/LeftUpLeg/LeftLeg",
            "Root/Global/Position/Hips/RightUpLeg/RightLeg",
            "Root/Global/Position/Hips/LeftUpLeg/LeftLeg/LeftFoot",
            "Root/Global/Position/Hips/RightUpLeg/RightLeg/RightFoot",
            "Root/Global/Position/Hips/Spine1",
            "Root/Global/Position/Hips/Spine1/Spine2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/Neck",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/Neck/Head",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand",
            "Root/Global/Position/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftFootBind/LeftToeBase",
            "Root/Global/Position/Hips/RightUpLeg/RightLeg/RightFoot/RightFootBind/RightToeBase",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandThumb1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandThumb1/LeftHandThumb2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandThumb1/LeftHandThumb2/LeftHandThumb3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandIndex1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandMiddle1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandMiddle1/LeftHandMiddle2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandMiddle1/LeftHandMiddle2/LeftHandMiddle3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandRing1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandRing1/LeftHandRing2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandRing1/LeftHandRing2/LeftHandRing3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandPinky1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandPinky1/LeftHandPinky2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandPinky1/LeftHandPinky2/LeftHandPinky3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandThumb1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandThumb1/RightHandThumb2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandThumb1/RightHandThumb2/RightHandThumb3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandIndex1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandMiddle1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandMiddle1/RightHandMiddle2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandMiddle1/RightHandMiddle2/RightHandMiddle3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandRing1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandRing1/RightHandRing2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandRing1/RightHandRing2/RightHandRing3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandPinky1",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandPinky1/RightHandPinky2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandPinky1/RightHandPinky2/RightHandPinky3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandIndex1/LeftHandIndex2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHandBind/LeftHandIndex1/LeftHandIndex2/LeftHandIndex3",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandIndex1/RightHandIndex2",
            "Root/Global/Position/Hips/Spine1/Spine2/Chest/RightShoulder/RightArm/RightForeArm/RightHand/RightHandBind/RightHandIndex1/RightHandIndex2/RightHandIndex3",
        };
    }
}