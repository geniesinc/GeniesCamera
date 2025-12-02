using System;
using System.Collections.Generic;
using Genies.Components.ShaderlessTools;
using UMA;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SubSpecies Container", menuName = "_GAPAvatarTesting/Avatars/Containers")]
#endif
    public class SubSpeciesContainer : ScriptableObject, IShaderlessAsset, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        public string Id;
        public string Guid;
        public GameObject BodyPrefab;
        public List<MeshGroup> MeshGroups = new();
        public Avatar Avatar;
        public Mesh UtilityMesh;
        public ScriptableObject[] Components;
        [SerializeField] private ShaderlessMaterials shaderlessMaterials;
        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }

        [Serializable]
        public struct MeshGroup
        {
            public string                    id;
            public List<SkinnedMeshRenderer> renderers;
        }
    }
}
