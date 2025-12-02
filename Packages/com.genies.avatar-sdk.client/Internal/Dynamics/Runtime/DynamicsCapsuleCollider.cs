using UnityEngine;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// A capsule collider that can affect particles.
    /// </summary>
    public class DynamicsCapsuleCollider : DynamicsCollider
    {
        [Range(0f, 1f)]
        public float Height = 0.2f;

        public float ScaledHeight => Height * transform.lossyScale.x;

        [Range(0f, 1f)]
        public float CollisionRadius = 0.1f;

        public float ScaledCollisionRadius => CollisionRadius * transform.lossyScale.x;

        public Vector3 Offset;

        public Vector3 ScaledOffset => Offset * transform.lossyScale.x;

        public Vector3 Rotation;
        private Quaternion _QRotation => Quaternion.Euler(Rotation);

        public Vector3 StartPosition => transform.position - 0.5f * ScaledHeight * (transform.rotation * _QRotation * Quaternion.Inverse(transform.rotation) * transform.up) + transform.TransformVector(Offset);
        public Vector3 MidPosition => (StartPosition + EndPosition) * 0.5f;
        public Vector3 EndPosition => transform.position + 0.5f * ScaledHeight * (transform.rotation * _QRotation * Quaternion.Inverse(transform.rotation) * transform.up) + transform.TransformVector(Offset);

#if UNITY_EDITOR
        private Mesh previewMesh;

        private float previewHeight = 0f;
        private float previewRadius = 0f;

        private void OnDrawGizmos()
        {
            var rotation = Quaternion.FromToRotation(Vector3.up, EndPosition - StartPosition);
            DynamicsGizmos.DrawWireCapsule(MidPosition, rotation, ScaledCollisionRadius, ScaledHeight);

            if (previewHeight != ScaledHeight || previewRadius != ScaledCollisionRadius)
            {
                DestroyImmediate(previewMesh);
                previewMesh = null;
            }

            if (previewMesh == null)
            {
                previewMesh = DynamicsGizmos.CreateCapsuleMesh(ScaledCollisionRadius, ScaledHeight);
                previewRadius = ScaledCollisionRadius;
                previewHeight = ScaledHeight;
            }

            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawMesh(previewMesh, MidPosition, transform.rotation * _QRotation);
        }
#endif
    }
}
