using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Genies.Shaders.Editor
{
    [System.Serializable]
    public class MegaStylizerShaderProperties
    {
        public List<string> INPUT;
        public List<string> ENVIRONMENT_REFLECTION;
        public List<string> SIMPLE_LIGHTING;
        public List<string> CARTOON;
        public List<string> CARTOON_TEXTURE;
        public List<string> LINE;
        public List<string> LINE_TEXTURE;
        public List<string> LINE_ADVANCED;
        public List<string> NORMAL_LIGHTS;
        public List<string> EXTRA;
    }

    public class MegaStylizerPropertyTransfer : EditorWindow
    {
        private Material masterMaterial;
        private Dictionary<string, bool> materialProperties = new Dictionary<string, bool>();
        private Dictionary<string, List<string>> propertyCategories = new Dictionary<string, List<string>>();
        private Vector2 scrollPos;

#if GENIES_INTERNAL
        [MenuItem("Window/Genies/Shaders/SDK/Tools/MegaStylizer Property Transfer")]
#endif
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MegaStylizerPropertyTransfer), false, "M.Sty Property Transfer");
        }

        private void OnEnable()
        {
            LoadPropertyCategoriesFromEditorFolder();
        }

        // GUI Methods
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Master Material", EditorStyles.boldLabel);
            masterMaterial = (Material)EditorGUILayout.ObjectField(masterMaterial, typeof(Material), false);

            DisplayCategoriesAndProperties();

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

        private void DisplayCategoriesAndProperties()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var category in propertyCategories)
            {
                string categoryName = category.Key.ToUpper();

                // Toggle all properties within this category
                bool allSelected = category.Value.All(prop => materialProperties[prop]);
                bool categoryToggle = EditorGUILayout.ToggleLeft(categoryName, allSelected);
                if (categoryToggle != allSelected)
                {
                    foreach (var prop in category.Value)
                    {
                        materialProperties[prop] = categoryToggle;
                    }
                }

                EditorGUI.indentLevel++;
                foreach (var prop in category.Value)
                {
                    materialProperties[prop] = EditorGUILayout.ToggleLeft(prop, materialProperties[prop]);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
        }

        // Data Loading Methods
        private void LoadPropertyCategoriesFromEditorFolder()
        {
            var categoriesAsset = Resources.Load<TextAsset>("MegaStylizerCategories");
            if (!categoriesAsset)
            {
                Debug.LogWarning("MegaStylizerCategories text asset was not found");
                return;
            }

            MegaStylizerShaderProperties loadedData = JsonUtility.FromJson<MegaStylizerShaderProperties>(categoriesAsset.text);
            if (loadedData != null)
            {
                PopulateCategoriesFromLoadedData(loadedData);
                Debug.Log("JSON file loaded successfully.");
            }
            else
            {
                Debug.LogWarning("Failed to load data from the JSON file.");
            }
        }

        private void PopulateCategoriesFromLoadedData(MegaStylizerShaderProperties loadedData)
        {
            foreach (var field in typeof(MegaStylizerShaderProperties).GetFields())
            {
                List<string> properties = field.GetValue(loadedData) as List<string>;
                if (properties != null)
                {
                    propertyCategories[field.Name] = properties;
                    foreach (var prop in properties)
                    {
                        if (!materialProperties.ContainsKey(prop))
                        {
                            materialProperties[prop] = false;
                        }
                    }
                }
            }
        }

        // Transfer Methods
        private void TransferMaterialParameters()
        {
            foreach (GameObject selected in Selection.gameObjects)
            {
                Renderer renderer = selected.GetComponent<Renderer>();
                if (renderer != null)
                {
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
                        // Optionally add more cases for other shader property types if necessary.
                    }
                }
            }
        }

        private void TransferPropertiesToScene()
        {
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Transfer Material Properties to Scene");
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        TransferPropertiesToMaterial(mat);
                    }
                }
            }
        }
    }
}
