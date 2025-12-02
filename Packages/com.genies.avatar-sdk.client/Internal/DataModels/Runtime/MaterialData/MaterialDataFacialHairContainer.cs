using System.Collections.Generic;
using UMA;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Genies.Models {
    public class MaterialDataFacialHairContainer : MaterialDataContainer {
        [Header("Extra Facial Hair Options")]
        public Color IconColor;

        public void Reset() {
            materialPrefix = "FacialHairMaterial";
            dataPrefix = "FacialHairMaterialData";
        }

        public override int SelectMaterial(Material[] materials) {
            return base.SelectMaterial(materials);
        }

        public override int SelectUmaMaterial(List<UMAData.GeneratedMaterial> materials, string UmaMaterialIdentifier) {
            int materialIndex = base.SelectUmaMaterial(materials, UmaMaterialIdentifier);

            //if material exists and the shader of the material is correct
            if (materialIndex >= 0 && materials[materialIndex].material.shader == targetShader) {
                targetMaterial.SetTexture("_NormalMapTexture", materials[materialIndex].material.GetTexture("_NormalMapTexture"));
                targetMaterial.SetTexture("_FlowMapTexture", materials[materialIndex].material.GetTexture("_FlowMapTexture"));
                targetMaterial.SetTexture("_AlbedoControlTexture", materials[materialIndex].material.GetTexture("_AlbedoControlTexture"));
            }

            return materialIndex;
        }

        #if UNITY_EDITOR
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/MaterialsPresets/Facial Hair Material")]
#endif
        public static new MaterialDataContainer CreateMaterialContainer() {
            var asset = ScriptableObject.CreateInstance<MaterialDataFacialHairContainer>();
            asset.materialPrefix = "FacialHairMaterial";
            asset.dataPrefix = "FacialHairMaterialData";

            return CreateDataBase(asset, "Assets/Genies/Resources/MaterialPresets/FacialHair/", asset.dataPrefix, asset.materialPrefix);
        }
        #endif
    }
}
