using System.Collections.Generic;
using Genies.Editor.MaterialPresetEditors;
using UMA;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Genies.Models {
    public class MaterialDataContainer : OrderedScriptableObject
    {
        [HideInInspector] public string materialName = "New";
        [HideInInspector] public string materialPrefix = "Material";
        [HideInInspector] public string dataPrefix = "MaterialData";
        [DrawMaterialInspector] public Material targetMaterial;

        //set this default shader on the .cs file in the inspector on the sub classes
        [HideInInspector] public Shader targetShader;
        [HideInInspector] public string assetId;

        [SerializeField] private string _guid; // The unique identifier for the FlairContainer instance.

        public string Guid
        {
            get => _guid;
            set => _guid = value;
        }

        public virtual int SelectMaterial(Material[] materials) {
            int i = 0;
            foreach (Material material in materials) {
                if (material.name.StartsWith(materialPrefix))
                {
                    return i;
                }

                i++;
            }

            Debug.LogWarning("No Material with specified Prefix");
            return -1;
        }

        public virtual int SelectUmaMaterial(List<UMAData.GeneratedMaterial> materials, string UmaMaterialIdentifier) {
            if (UmaMaterialIdentifier != "") {
                int i = 0;
                foreach (UMAData.GeneratedMaterial material in materials) {
                    if (material.umaMaterial.ToString().Contains(UmaMaterialIdentifier))
                    {
                        return i;
                    }

                    i++;
                }

                Debug.LogWarning(UmaMaterialIdentifier + " Uma Material Not found");
                return -1;
            }

            Debug.LogError("Uma Material Not Specified");
            return -1;
        }

        #if UNITY_EDITOR
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/MaterialsPresets/Generic Material")]
#endif
        public static MaterialDataContainer CreateMaterialContainer() {
            MaterialDataContainer asset = ScriptableObject.CreateInstance<MaterialDataContainer>();
            return CreateDataBase(asset, "Assets/Genies_Content/MaterialsPresets/Generic/", asset.dataPrefix, asset.materialPrefix);
        }

        public static MaterialDataContainer CreateDataBase(MaterialDataContainer asset, string path, string assetName, string materialName) {
            AssetDatabase.CreateAsset(asset, path + assetName + "_New.asset");
            if (asset.targetMaterial == null) {
                if (asset.targetShader != null) asset.targetMaterial = new Material(asset.targetShader);
                else asset.targetMaterial = new Material(Shader.Find("Standard"));
            }

            asset.targetMaterial.name = materialName + "_New";

            AssetDatabase.AddObjectToAsset(asset.targetMaterial, asset);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            return asset;
        }
        #endif
    }
}
