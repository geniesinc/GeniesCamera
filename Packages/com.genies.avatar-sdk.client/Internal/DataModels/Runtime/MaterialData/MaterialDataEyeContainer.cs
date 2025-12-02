using System.Collections.Generic;
using Genies.Components.ShaderlessTools;
using UMA;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Genies.Models {
    public class MaterialDataEyeContainer : MaterialDataContainer, IDynamicAsset, IShaderlessAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        public void Reset() {
            materialPrefix = "EyeMaterial";
            dataPrefix = "EyeMaterialData";
        }

        [Header("Extra Eye Options")]
        public Color InnerIconColor;
        public Color OuterIconColor;

        [SerializeField] private ShaderlessMaterials shaderlessMaterials;

        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }

        public override int SelectMaterial(Material[] materials) {
            return base.SelectMaterial(materials);
        }

        public override int SelectUmaMaterial(List<UMAData.GeneratedMaterial> materials, string UmaMaterialIdentifier) {
            return base.SelectUmaMaterial(materials, UmaMaterialIdentifier);
        }

        #if UNITY_EDITOR
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/DataModels/Editor Utilities/MaterialsPresets/Eye Material")]
#endif
        public new static MaterialDataContainer CreateMaterialContainer() {
            var asset = ScriptableObject.CreateInstance<MaterialDataEyeContainer>();
            asset.materialPrefix = "EyeMaterial";
            asset.dataPrefix = "EyeMaterialData";

            return CreateDataBase(asset, "Assets/Genies_Content/MaterialsPresets/Eye/", asset.dataPrefix, asset.materialPrefix);
        }
        #endif
    }
}
