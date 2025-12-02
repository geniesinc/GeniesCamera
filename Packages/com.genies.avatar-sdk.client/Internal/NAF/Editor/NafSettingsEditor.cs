using UnityEditor;
using UnityEngine;

namespace Genies.Naf.Editor
{
    [CustomEditor(typeof(NafSettings))]
    public class NafSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target is not NafSettings settings)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (GUILayout.Button("Apply"))
            {
                settings.Apply();
            }
        }
    }
}