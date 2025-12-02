using System.Collections.Generic;
using UMA;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Genies.Models {
    public class MaterialDataSkinContainer : MaterialDataContainer {
        [HideInInspector] public Texture2D metallicSmoothness;
        [HideInInspector] public Texture2D normalMap;
        [HideInInspector] public Texture2D translusencyMap;
        [HideInInspector] public Color skinColor;

        [Header("Extra Skin Options")]
        public Color IconColor;

        public void Reset() {
            materialPrefix = "SkinMaterial";
            dataPrefix = "SkinMaterialData";
        }

        public override int SelectMaterial(Material[] materials) {
            return base.SelectMaterial(materials);
        }

        public override int SelectUmaMaterial(List<UMAData.GeneratedMaterial> materials, string UmaMaterialIdentifier) {
            return base.SelectUmaMaterial(materials, UmaMaterialIdentifier);
        }

        #if UNITY_EDITOR
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/MaterialsPresets/Skin Material")]
#endif
        public static new MaterialDataContainer CreateMaterialContainer() {
            var asset = ScriptableObject.CreateInstance<MaterialDataSkinContainer>();
            asset.materialPrefix = "SkinMaterial";
            asset.dataPrefix = "SkinMaterialData";
            var skinAsset = CreateDataBase(asset, "Assets/Genies_Content/MaterialsPresets/Skin/", asset.dataPrefix, asset.materialPrefix);

            //instead of doing this by hand we should be sampling the properties of the provided shader and saving them to a list to make this more modular
            skinAsset.targetMaterial.SetTexture("_MetallicSmoothness", asset.metallicSmoothness);
            skinAsset.targetMaterial.SetTexture("_Normal", asset.normalMap);
            skinAsset.targetMaterial.SetTexture("_Translucency", asset.translusencyMap);
            skinAsset.targetMaterial.SetColor("_SkinColor", asset.skinColor);


            return skinAsset;
        }
        #endif
    }
}
