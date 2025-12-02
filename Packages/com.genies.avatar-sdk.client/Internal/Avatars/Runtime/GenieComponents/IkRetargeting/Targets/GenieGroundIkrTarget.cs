using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IIkrTarget"/> implementation for grounding IK goals relative to an <see cref="IGenie"/> ground.
    /// </summary>
    public sealed class GenieGroundIkrTarget : IIkrTarget
    {
        public string     Key         { get; }
        public float      Weight      => _animator.GetFloat(_weightPropertyId);
        public Vector3    Position    => GetPosition();
        public Quaternion Rotation    => _ikJoint.rotation;
        public bool       HasPosition => true;
        public bool       HasRotation => true;
        public bool       IsFree      => false;
        public Transform  Transform   => null;
        
        private readonly Animator  _animator;
        private readonly Transform _genieRoot;
        private readonly Transform _goalBone;
        private readonly Transform _ikJoint;
        private readonly int       _weightPropertyId;
        private readonly bool      _destroyJointOnDispose;

        public GenieGroundIkrTarget(string key, IGenie genie, Transform goalBone, Transform ikJoint, int weightPropertyId, bool destroyJointOnDispose)
        {
            Key = key;
            _animator = genie.Animator;
            _genieRoot = genie.Root.transform;
            _goalBone = goalBone;
            _ikJoint = ikJoint;
            _weightPropertyId = weightPropertyId;
            _destroyJointOnDispose = destroyJointOnDispose;
        }

        public void Dispose()
        {
            // use DestroyImmediate, otherwise when doing animator.Rebind() on the same frame will fail sometimes
            if (_destroyJointOnDispose && _ikJoint)
            {
                Object.DestroyImmediate(_ikJoint.gameObject);
            }
        }

        private Vector3 GetPosition()
        {
            // calculate the current ground offset of the genie
            Transform genieParent = _genieRoot.parent;
            float groundOffset = genieParent is null ? _genieRoot.position.y : _genieRoot.position.y - genieParent.position.y;
            
            // calculate the target position applying the IK joint height minus the ground offset
            Vector3 position = _goalBone.position;
            position.y = _ikJoint.position.y - groundOffset;
            
            return position;
        }
        
#region Config
        /// <summary>
        /// Serializable config that can be used to create <see cref="GenieGroundIkrTarget"/>s from.
        /// </summary>
        [Serializable]
        public struct Config
        {
            public AvatarIKGoal goal;
            public string       parent;
            public string       child;
            public string       weightProperty;
        }
        
        public static GenieGroundIkrTarget CreateFromConfig(Config config, IGenie genie)
        {
            Transform genieRoot = genie.Root.transform;
            
            // get goal bone transform
            HumanBodyBones bone = config.goal switch
            {
                AvatarIKGoal.LeftFoot  => HumanBodyBones.LeftFoot,
                AvatarIKGoal.RightFoot => HumanBodyBones.RightFoot,
                AvatarIKGoal.LeftHand  => HumanBodyBones.LeftHand,
                AvatarIKGoal.RightHand => HumanBodyBones.RightHand,
                _ => throw new ArgumentOutOfRangeException(nameof(config.goal), config.goal, null)
            };
            Transform goalBone = genie.Animator.GetBoneTransform(bone);
            
            // get parent
            Transform parent = genieRoot.Find(config.parent);
            if (!parent)
            {
                throw new Exception($"[{nameof(GenieGroundIkrTarget)}] failed to create target: parent not found \"{config.parent}\"");
            }

            // create ik joint transform
            Transform ikJoint = new GameObject(config.child).transform;
            ikJoint.SetParent(parent, worldPositionStays: false);
            
            return new GenieGroundIkrTarget
            (
                key:                   $"{config.parent}/{config.child}",
                genie:                 genie,
                goalBone:              goalBone,
                ikJoint:               ikJoint,
                weightPropertyId:      Animator.StringToHash(config.weightProperty),
                destroyJointOnDispose: true
            );
        }
#endregion
    }
}