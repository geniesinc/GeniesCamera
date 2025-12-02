using UnityEditor;

namespace Genies.Components.Dynamics
{
    [CustomEditor(typeof(DynamicsRecipe))]
    public class DynamicsRecipeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("StructureName"));

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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ParticleRecipes"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LinkRecipes"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SphereColliderRecipes"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CapsuleColliderRecipes"), true);
            
            // These properties inherited from Bonus Components (TODO: Update capitalization if the Avatar's package changes)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parentName"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedChildName"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
