using UnityEditor;
using UnityEngine;

namespace Genies.Components.Dynamics
{
    [CustomEditor(typeof(DynamicsParticle))]
    public class DynamicsParticleEditor : Editor
    {
        private void OnSceneGUI()
        {
            var particle = target as DynamicsParticle;

            if (!particle)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            // Provide a convenient set of handles for adjusting particle collision radius in the scene view.
            var newRadius = Handles.RadiusHandle(Quaternion.identity, particle.transform.position, particle.CollisionRadius);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(particle, "Collision Radius Change (Particle)");
                particle.CollisionRadius = newRadius;
            }
        }
    }
}
