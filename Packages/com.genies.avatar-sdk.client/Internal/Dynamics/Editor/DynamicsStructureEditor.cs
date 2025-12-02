using UnityEditor;
using UnityEngine;

namespace Genies.Components.Dynamics
{
    [CustomEditor(typeof(DynamicsStructure))]
    public class DynamicsStructureEditor : Editor
    {
        private bool _guiDisplayExpanded;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty updateMethodProperty = serializedObject.FindProperty("DynamicsUpdateMethod");
            EditorGUILayout.PropertyField(updateMethodProperty);

            if (updateMethodProperty.enumValueIndex == (int)DynamicsStructure.UpdateMethod.Specified_FPS)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SfpsDynamicsFPS"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SfpsUpdateFPS"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Iterations"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PreWarmTime"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("CollisionComputeMethod"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Gravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Friction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ParticleToParticleCollision"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Particles"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Links"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Colliders"), true);

            SerializedProperty computeShaderProperty = serializedObject.FindProperty("ComputeShader");
            if (computeShaderProperty.objectReferenceValue == null)
            {
                // Convenience feature to auto-populate the compute shader reference in dynamics structures.
                // TODO: Replace this with a static reference to the compute shader.
                // Currently there is no known way to reference this shader in a build without embedding it within the project "Resources" folder.
                ComputeShader computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.genies.dynamics/Runtime/ComputeShaders/GeniesDynamicsGPUWorker.compute");
                if (computeShader == null)
                {
                    Debug.Log("Unable to load default compute shader for Genies Dynamics.");
                }
                else
                {
                    computeShaderProperty.objectReferenceValue = computeShader;
                }
            }

            EditorGUILayout.PropertyField(computeShaderProperty, true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_paused"), true);

            _guiDisplayExpanded = EditorGUILayout.Foldout(_guiDisplayExpanded, "GUI Display");

            if (_guiDisplayExpanded)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_showHomeTransforms"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_showStatistics"), true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(DynamicsLink))]
    public class DynamicsLinkElementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var linkConstraint = property.objectReferenceValue as DynamicsLink;

            var linkDescription = "";
            if (linkConstraint != null)
            {
                var nullParticleLabel = "( )";
                var startName = linkConstraint.StartParticle ? linkConstraint.StartParticle.name : nullParticleLabel;
                var endName = linkConstraint.EndParticle ? linkConstraint.EndParticle.name : nullParticleLabel;

                linkDescription = startName + " <-> " + endName;
            }

            EditorGUI.PropertyField(position, property, new GUIContent(linkDescription), true);
        }
    }
}
