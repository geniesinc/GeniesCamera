using UnityEngine;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// A sphere collider that can affect particles.
    /// </summary>
    public class DynamicsSphereCollider : DynamicsCollider
    {
        [Range(0,1)]
        public float CollisionRadius = 0.25f;
        
        public float ScaledCollisionRadius => CollisionRadius * transform.lossyScale.x;

        public Vector3 Offset;

        public Vector3 Center => transform.TransformPoint(Offset);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(Center, ScaledCollisionRadius);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(Center, ScaledCollisionRadius);
        }
#endif
    }
}
