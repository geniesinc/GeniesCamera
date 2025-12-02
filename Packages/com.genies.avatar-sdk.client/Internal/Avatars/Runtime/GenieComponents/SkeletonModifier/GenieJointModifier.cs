using System;
using Newtonsoft.Json;
using UnityEngine;
using Operation = Genies.Avatars.JointModifier.Operation;

namespace Genies.Avatars
{
    [Serializable]
    public sealed class GenieJointModifier
    {
        // parameters
        [SerializeField, JsonProperty, Tooltip("Defines the target joint by name or path within the Genie instance")]
        private string targetJoint;
        [SerializeField, JsonProperty, Tooltip("Scale will be evaluated in the LateUpdate rather than in the animation threads")]
        private bool scaleLateUpdate = false;
        
        [Space(16)]
        [SerializeField, JsonProperty]
        private Operation positionOperation = Operation.None;
        [SerializeField, JsonProperty]
        private Vector3 position = Vector3.zero;
        [SerializeField, JsonProperty]
        private Operation rotationOperation = Operation.None;
        [SerializeField, JsonProperty]
        private Vector3 rotation = Vector3.zero;
        [SerializeField, JsonProperty]
        private Operation scaleOperation = Operation.None;
        [SerializeField, JsonProperty]
        private Vector3 scale = Vector3.one;
        
        [JsonIgnore] public string TargetJoint => targetJoint;
        
        [JsonIgnore]
        public bool ScaleLateUpdate
        {
            get => scaleLateUpdate;
            set => scaleLateUpdate = WrappedModifier.scaleLateUpdate = value;
        }
        
        [JsonIgnore]
        public Operation PositionOperation
        {
            get => positionOperation;
            set => positionOperation = WrappedModifier.positionOperation = value;
        }
        
        [JsonIgnore]
        public Vector3 Position
        {
            get => position;
            set => position = WrappedModifier.position = value;
        }
        
        [JsonIgnore]
        public Operation RotationOperation
        {
            get => rotationOperation;
            set => rotationOperation = WrappedModifier.rotationOperation = value;
        }
        
        [JsonIgnore]
        public Vector3 Rotation
        {
            get => rotation;
            set => rotation = WrappedModifier.rotation = value;
        }
        
        [JsonIgnore]
        public Operation ScaleOperation
        {
            get => scaleOperation;
            set => scaleOperation = WrappedModifier.scaleOperation = value;
        }
        
        [JsonIgnore]
        public Vector3 Scale
        {
            get => scale;
            set => scale = WrappedModifier.scale = value;
        }
        
        internal JointModifier WrappedModifier { get; private set; }

        // this parameterless constructor is required for json serialization
        public GenieJointModifier()
        {
            targetJoint = string.Empty;
            WrappedModifier = new JointModifier(null);
        }

        public GenieJointModifier(string targetJoint, GenieJointModifier copyFrom = null)
        {
            this.targetJoint = targetJoint;
            WrappedModifier = new JointModifier(null);

            if (copyFrom is null)
            {
                return;
            }

            scaleLateUpdate   = copyFrom.scaleLateUpdate;
            positionOperation = copyFrom.positionOperation;
            position          = copyFrom.position;
            rotationOperation = copyFrom.rotationOperation;
            rotation          = copyFrom.rotation;
            scaleOperation    = copyFrom.scaleOperation;
            scale             = copyFrom.scale;
        }

        public GenieJointModifier(GenieJointModifier copyFrom)
        {
            targetJoint       = copyFrom.targetJoint;
            scaleLateUpdate   = copyFrom.scaleLateUpdate;
            positionOperation = copyFrom.positionOperation;
            position          = copyFrom.position;
            rotationOperation = copyFrom.rotationOperation;
            rotation          = copyFrom.rotation;
            scaleOperation    = copyFrom.scaleOperation;
            scale             = copyFrom.scale;

            WrappedModifier = new JointModifier(null);
        }

        public void Reset()
        {
            scaleLateUpdate   = false;
            PositionOperation = Operation.None;
            Position          = Vector3.zero;
            RotationOperation = Operation.None;
            Rotation          = Vector3.zero;
            ScaleOperation    = Operation.None;
            Scale             = Vector3.one;
        }
        
        internal void RebuildWrappedModifier(Transform targetJoint)
        {
            WrappedModifier = new JointModifier(targetJoint)
            {
                scaleLateUpdate   = scaleLateUpdate,
                positionOperation = positionOperation,
                position          = position,
                rotationOperation = rotationOperation,
                rotation          = rotation,
                scaleOperation    = scaleOperation,
                scale             = scale,
            };
        }
    }
}