using System;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a target for our IK retargeting system. All we need from a target is to know its world-space position
    /// and rotation as well as its current weight. Free targets can also have a <see cref="Transform"/> associated so
    /// it can be manipulated.
    /// </summary>
    public interface IIkrTarget : IDisposable
    {
        /// <summary>
        /// Unique key of the target.
        /// </summary>
        public string Key { get; }
        
        /// <summary>
        /// Current weight for this target in the range [0, 1].
        /// </summary>
        public float Weight { get; }
        
        /// <summary>
        /// Current world-space position of the target.
        /// </summary>
        public Vector3 Position { get; }
        
        /// <summary>
        /// Current world-space rotation of the target.
        /// </summary>
        public Quaternion Rotation { get; }
        
        /// <summary>
        /// Whether or not this target has a position. If it doesn't then it shouldn't be used for position blending.
        /// </summary>
        public bool HasPosition { get; }
        
        /// <summary>
        /// Whether or not this target has a rotation. If it doesn't then it shouldn't be used for rotation blending.
        /// </summary>
        public bool HasRotation { get; }
        
        /// <summary>
        /// Whether or not this target is meant to be controlled by the animator or manually (freely). All free targets
        /// should provide a transform.
        /// </summary>
        public bool IsFree { get; }
        
        /// <summary>
        /// The target transform. It is not mandatory to provide a transform for non-free targets, so this could be null.
        /// </summary>
        public Transform Transform { get; }
    }
}