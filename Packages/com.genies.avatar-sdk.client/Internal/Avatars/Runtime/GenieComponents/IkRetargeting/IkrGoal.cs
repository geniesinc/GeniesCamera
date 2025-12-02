using System;
using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents an IK goal from our IK retargeting system. It maps to one of the Unity supported <see cref="AvatarIKGoal"/>
    /// and can have an arbitrary number of targets.
    /// </summary>
    public sealed class IkrGoal
    {
        public readonly AvatarIKGoal     Goal;
        public readonly Animator         Animator;
        public readonly List<IIkrTarget> Targets;
        
        private readonly AvatarIKHint _hint;
        private readonly Transform    _transform;
        private readonly Transform    _hintTransform;
        
        public IkrGoal(AvatarIKGoal goal, Animator animator, IEnumerable<IIkrTarget> targets = null)
        {
            Goal = goal;
            Animator = animator;
            Targets = new List<IIkrTarget>();
            if (targets is not null)
            {
                Targets.AddRange(targets);
            }

            switch (goal)
            {
                case AvatarIKGoal.LeftFoot:
                    _hint = AvatarIKHint.LeftKnee;
                    _transform = Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    _hintTransform = Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                    break;
                
                case AvatarIKGoal.RightFoot:
                    _hint = AvatarIKHint.RightKnee;
                    _transform = Animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    _hintTransform = Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                    break;
                
                case AvatarIKGoal.LeftHand:
                    _hint = AvatarIKHint.LeftElbow;
                    _transform = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    _hintTransform = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                    break;
                
                case AvatarIKGoal.RightHand:
                    _hint = AvatarIKHint.RightElbow;
                    _transform = Animator.GetBoneTransform(HumanBodyBones.RightHand);
                    _hintTransform = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(goal), goal, null);
            }
        }

        public void OnAnimatorIK(bool setIkHints = false)
        {
            if (!_transform)
            {
                return;
            }

            // perform target blending
            Vector3 position = _transform.position;
            Quaternion rotation = _transform.rotation;
            IkrTargetBlending.Result blendingResult = IkrTargetBlending.BlendTargets(position, rotation, Targets);
            
            /**
             * We need this offset because even though the Unity docs say that animator.SetIKRotation() applies a rotation in world-space
             * this is not true. Depending on the character rig there may be a constant offset applied to whatever rotation you give
             * to the method, so we have to calculate it so we can offset our final rotation.
             *
             * Context: https://forum.unity.com/threads/how-to-aim-animator-setikrotation-to-direction-wrt-hand-bone-rotation-offset.355941/#post-2950678
             */
            Quaternion unityWeirdAndAnnoyingIkRotationOffsetLol = Quaternion.Inverse(rotation) * Animator.GetIKRotation(Goal);
            
            // calculate final IK position and rotation
            position = position + blendingResult.PositionOffset;
            rotation = rotation * blendingResult.RotationOffset * unityWeirdAndAnnoyingIkRotationOffsetLol;
            
            // optionally set hint position to the current hint transform position. Based on some experimentation this doesn't seem to yield good results
            if (setIkHints)
            {
                Animator.SetIKHintPosition      (_hint, _hintTransform.position);
                Animator.SetIKHintPositionWeight(_hint, blendingResult.PositionWeight); // not sure if we should always put a weight of 1.0 here
            }
            
            // set the blending result transform to the IK system
            Animator.SetIKPosition      (Goal, position);
            Animator.SetIKPositionWeight(Goal, blendingResult.PositionWeight);
            Animator.SetIKRotation      (Goal, rotation);
            Animator.SetIKRotationWeight(Goal, blendingResult.RotationWeight);
        }
        
#region Creation from config
        public static void CreateGoals(Animator animator, AnimatorParameters parameters, IkrConfig config, ICollection<IkrGoal> results,
            IDictionary<string, IIkrTarget> targets = null)
        {
            if (!config)
            {
                return;
            }

            CreateGoals(animator, parameters, config.goals, results, targets);
        }
        
        public static void CreateGoals(Animator animator, AnimatorParameters parameters, IEnumerable<IkrConfig.Goal> configs, ICollection<IkrGoal> results,
            IDictionary<string, IIkrTarget> targets = null)
        {
            if (results is null)
            {
                throw new NullReferenceException($"[{nameof(IkrGoal)}] IKR goal results collection is null");
            }

            foreach (IkrConfig.Goal config in configs)
            {
                try
                {
                    IkrGoal goal = CreateGoal(animator, parameters, config, targets);
                    results.Add(goal);
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
            }
        }
        
        public static IkrGoal CreateGoal(Animator animator, AnimatorParameters parameters, IkrConfig.Goal config, IDictionary<string, IIkrTarget> targets = null)
        {
            // create targets
            var createdTargets = new List<IIkrTarget>(config.transformTargets.Count);
            TransformIkrTarget.GetOrCreateFromConfigs(config.transformTargets, animator, parameters, createdTargets, targets);
            
            return new IkrGoal
            (
                goal:     config.goal,
                animator: animator,
                targets:  createdTargets
            );
        }
#endregion
    }
}