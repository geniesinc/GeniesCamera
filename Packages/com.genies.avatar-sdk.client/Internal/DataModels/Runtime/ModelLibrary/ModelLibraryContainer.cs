using Genies.Components.ShaderlessTools;
using UnityEngine;

namespace Genies.Models
{
    public class ModelLibraryContainer: OrderedScriptableObject, IShaderlessAsset, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;
        
        public bool IsEditable => EditableRegionCount > 0 && EditableRegionsMap;

        public string Guid;
        public ModelAssetType AssetType;
        public GameObject Prefab;
        public Texture2D EditableRegionsMap;
        public int EditableRegionCount;

        [SerializeField] private ShaderlessMaterials shaderlessMaterials;
        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }
    }
}
