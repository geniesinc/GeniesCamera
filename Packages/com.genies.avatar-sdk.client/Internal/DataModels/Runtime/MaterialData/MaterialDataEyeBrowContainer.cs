using System.Collections.Generic;
using UMA;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Genies.Models {
    public class MaterialDataEyeBrowContainer : MaterialDataContainer {
        public void Reset() {
            materialPrefix = "EyeBrowMaterial";
            dataPrefix = "EyeBrowMaterialData";
        }

        [Header("Extra EyeBrow Options")] public Color IconColor;

        public override int SelectMaterial(Material[] materials) {
            return base.SelectMaterial(materials);
        }

        public override int SelectUmaMaterial(List<UMAData.GeneratedMaterial> materials, string UmaMaterialIdentifier) {
            return base.SelectUmaMaterial(materials, UmaMaterialIdentifier);
        }

#if UNITY_EDITOR
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/Material/EyeBrow Material")]
#endif
        public static new MaterialDataContainer CreateMaterialContainer() {
            var asset = ScriptableObject.CreateInstance<MaterialDataEyeBrowContainer>();
            asset.materialPrefix = "EyeBrowMaterial";
            asset.dataPrefix = "EyeBrowMaterialData";

            return CreateDataBase(asset, "Assets/Genies_Content/Materials/EyeBrow/", asset.dataPrefix, asset.materialPrefix);
        }
#endif
    }
}
