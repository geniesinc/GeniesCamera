using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Genies.Shaders.Editor
{
    public class MaterialPropertiesTransfer : EditorWindow
    {
        private Material masterMaterial;
        private Material lastCheckedMaterial;
        private Dictionary<string, bool> materialProperties = new Dictionary<string, bool>();
        private Vector2 scrollPos;

#if GENIES_INTERNAL
        [MenuItem("Window/Genies/Shaders/SDK/Tools/Material Property Transfer")]
#endif
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MaterialPropertiesTransfer), false, "Mat Property Transfer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Master Material", EditorStyles.boldLabel);
            masterMaterial = (Material)EditorGUILayout.ObjectField(masterMaterial, typeof(Material), false);

            if (masterMaterial != lastCheckedMaterial)
            {
                UpdateMaterialProperties();
                lastCheckedMaterial = masterMaterial;
            }

            if (masterMaterial != null)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                foreach (var prop in materialProperties.Keys.ToList())
                {
                    materialProperties[prop] = EditorGUILayout.ToggleLeft(prop, materialProperties[prop]);
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();  // Pushes the following UI elements to the bottom

            // Select All Button
            if (GUILayout.Button("Select All"))
            {
                bool allSelected = !materialProperties.Values.All(val => val);
                foreach (var key in materialProperties.Keys.ToList())
                {
                    materialProperties[key] = allSelected;
                }
            }

            // Transfer to Selection Button
            if (GUILayout.Button("Transfer to Selection"))
            {
                TransferMaterialParameters();
            }

            // Transfer to Scene Button
            if (GUILayout.Button("Transfer to Scene"))
            {
                TransferPropertiesToScene();
            }
        }

        private void UpdateMaterialProperties()
        {
            materialProperties.Clear();
            Shader shader = masterMaterial.shader;

            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                string propName = shader.GetPropertyName(i);
                materialProperties[propName] = false;
            }
        }

        private void TransferMaterialParameters()
        {
            foreach (GameObject selected in Selection.gameObjects)
            {
                Renderer renderer = selected.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Record the renderer's state for undoing
                    Undo.RecordObject(renderer, "Transfer Material Properties");

                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        TransferPropertiesToMaterial(mat);
                    }
                }
            }
        }

        private void TransferPropertiesToMaterial(Material target)
        {
            if (target == null || masterMaterial == null)
            {
                return;
            }

            // Record the material's state for undoing
            Undo.RecordObject(target, "Modify Material Properties");

            Shader shader = masterMaterial.shader;
            int propertyCount = shader.GetPropertyCount();

            for (int i = 0; i < propertyCount; i++)
            {
                string propName = shader.GetPropertyName(i);

                if (materialProperties.ContainsKey(propName) && materialProperties[propName] && target.HasProperty(propName))
                {
                    switch (shader.GetPropertyType(i))
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color:
                            target.SetColor(propName, masterMaterial.GetColor(propName));
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                        case UnityEngine.Rendering.ShaderPropertyType.Range:
                            target.SetFloat(propName, masterMaterial.GetFloat(propName));
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector:
                            target.SetVector(propName, masterMaterial.GetVector(propName));
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            target.SetTexture(propName, masterMaterial.GetTexture(propName));
                            break;
                        // Handle other types as needed
                    }
                }
            }
        }

        private void TransferPropertiesToScene()
        {
            // Getting all objects in the scene with a MeshRenderer
            MeshRenderer[] renderers = GameObject.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (MeshRenderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    TransferPropertiesToMaterial(mat);
                }
            }
        }
    }
}
