using Genies.Components.Dynamics;
using UnityEditor;
using UnityEngine;

namespace Genies.Components.Dynamics
{
    [CustomEditor(typeof(DynamicsLink))]
    public class DynamicsLinkEditor : Editor
    {
        private void OnSceneGUI()
        {
            var link = target as DynamicsLink;
    
            if (!link || !link.StartParticle || !link.EndParticle)
            {
                return;
            }

            Vector3 start = link.StartParticle.transform.position;
            Vector3 end = link.EndParticle.transform.position;

            // Draws a thicker line to indicate that this link is selected.
            Handles.DrawBezier(start, end, start, end, Color.green, null, 5f);
        }
    }
}
