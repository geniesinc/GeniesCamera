using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
    public sealed class JointModifier
    {
        // parameters
        [SerializeField]
        private Transform targetJoint;
        
        [Tooltip("Scale will be evaluated in the LateUpdate rather than in the animation threads")]
        public bool scaleLateUpdate = false;
        [Space(16)]
        public Operation positionOperation = Operation.None;
        public Vector3   position          = Vector3.zero;
        public Operation rotationOperation = Operation.None;
        public Vector3   rotation          = Vector3.zero;
        public Operation scaleOperation    = Operation.None;
        public Vector3   scale             = Vector3.one;
        
        public Transform TargetJoint     => targetJoint;

        public JointModifier(Transform targetJoint)
        {
            this.targetJoint = targetJoint;
        }
        
        public void Reset()
        {
            positionOperation = Operation.None;
            position          = Vector3.zero;
            rotationOperation = Operation.None;
            rotation          = Vector3.zero;
            scaleOperation    = Operation.None;
            scale             = Vector3.one;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EvaluatePosition(ref Vector3 currentPosition)
        {
            currentPosition = positionOperation switch
            {
                Operation.None   => currentPosition,
                Operation.Offset => currentPosition + position,
                Operation.Set    => position,
                _                => currentPosition,
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EvaluateRotation(ref Quaternion currentRotation)
        {
            currentRotation = rotationOperation switch
            {
                Operation.None   => currentRotation,
                Operation.Offset => currentRotation * Quaternion.Euler(rotation),
                Operation.Set    => Quaternion.Euler(rotation),
                _                => currentRotation,
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EvaluateScale(ref Vector3 currentScale)
        {
            currentScale = scaleOperation switch
            {
                Operation.None   => currentScale,
                Operation.Offset => Vector3.Scale(currentScale, scale),
                Operation.Set    => scale,
                _                => currentScale,
            };
        }
        
        public enum Operation
        {
            /// <summary>
            /// No operation is performed for this channel.
            /// </summary>
            None = 0,
            
            /// <summary>
            /// The value is added to the current value of the target joint.
            /// </summary>
            Offset = 1,
            
            /// <summary>
            /// The value is set to the target joint.
            /// </summary>
            Set = 2,
        }
    }
}