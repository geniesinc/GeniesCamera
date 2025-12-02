using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Genies.Avatars
{
    public sealed partial class SkeletonModifierBehaviour
    {
        private sealed class RigConstraint :
            RigConstraint<RigConstraintJob, RigConstraintData, RigConstraintJobBinder> { }
        
        // the data for each joint required by the RigConstraintJob
        private sealed class JointJobData
        {
            public Transform                Joint;
            public ReadWriteTransformHandle JointHandle;
            public Vector3                  OriginalScale; // store joint's original scale for when we need to restore the scale manually on LateUpdate
            public List<JointModifier>      Modifiers;
            
            public bool RequiresScaleLateUpdate()
            {
                // if just one modifier has ScaleLateUpdate enabled then the whole joint job will require it
                foreach (JointModifier modifier in Modifiers)
                {
                    if (modifier.scaleLateUpdate)
                    {
                        return true;
                    }
                }
                
                return false;
            }
        }
        
        private struct RigConstraintJob : IWeightedAnimationJob
        {
            public List<JointJobData> Data;
            public List<JointJobData> RestoringData;

            public FloatProperty jobWeight { get; set; }
            
            public void ProcessAnimation(AnimationStream stream)
            {
                // pass-through joints set for restore
                foreach (JointJobData data in RestoringData)
                {
                    AnimationRuntimeUtils.PassThrough(stream, data.JointHandle);
                }

                float weight = jobWeight.Get(stream);
                if (weight <= 0.0f)
                {
                    PassThrough(stream);
                    return;
                }

                foreach (JointJobData data in Data)
                {
                    data.JointHandle.GetLocalTRS(stream, out Vector3 sourcePosition, out Quaternion sourceRotation, out Vector3 sourceScale);
                    
                    Vector3    position = sourcePosition;
                    Quaternion rotation = sourceRotation;
                    Vector3    scale    = sourceScale;

                    if (data.RequiresScaleLateUpdate())
                    {
                        for (int i = 0; i < data.Modifiers.Count; ++i)
                        {
                            data.Modifiers[i].EvaluatePosition(ref position);
                            data.Modifiers[i].EvaluateRotation(ref rotation);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < data.Modifiers.Count; ++i)
                        {
                            data.Modifiers[i].EvaluatePosition(ref position);
                            data.Modifiers[i].EvaluateRotation(ref rotation);
                            data.Modifiers[i].EvaluateScale(ref scale);
                        }
                    }
                    
                    position = Vector3.Lerp(sourcePosition, position, weight);
                    rotation = Quaternion.Lerp(sourceRotation, rotation, weight);
                    scale    = Vector3.Lerp(sourceScale, scale, weight);
                    
                    data.JointHandle.SetLocalTRS(stream, position, rotation, scale);
                }
            }
            
            public void ProcessRootMotion(AnimationStream stream) { }
            
            private void PassThrough(AnimationStream stream)
            {
                foreach (JointJobData data in Data)
                {
                    AnimationRuntimeUtils.PassThrough(stream, data.JointHandle);
                }
            }
        }
        
        [Serializable]
        private struct RigConstraintData : IAnimationJobData
        {
            public SkeletonModifierBehaviour skeletonModifier;
            
            public bool IsValid()
                => skeletonModifier;

            public void SetDefaultValues()
                => skeletonModifier = null;
        }
        
        private class RigConstraintJobBinder : AnimationJobBinder<RigConstraintJob, RigConstraintData>
        {
            public override RigConstraintJob Create(Animator animator, ref RigConstraintData data, Component component)
            {
                // bind joint transforms
                List<JointJobData> jobData = data.skeletonModifier._jobData;
                List<JointJobData> restoreData = data.skeletonModifier._restoringJobData;
                
                foreach (JointJobData jointData in jobData)
                {
                    jointData.JointHandle = ReadWriteTransformHandle.Bind(animator, jointData.Joint);
                }

                foreach (JointJobData jointData in restoreData)
                {
                    jointData.JointHandle = ReadWriteTransformHandle.Bind(animator, jointData.Joint);
                }

                return new RigConstraintJob
                {
                    Data          = jobData,
                    RestoringData = restoreData,
                };
            }

            public override void Destroy(RigConstraintJob job) { }
        }
    }
}