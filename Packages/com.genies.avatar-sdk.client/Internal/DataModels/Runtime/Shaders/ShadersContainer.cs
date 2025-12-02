using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for Shaders.
    /// It holds are the data that the thing will use in the addressable bundles
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ShadersContainer", menuName = "Genies/Shaders/ShadersContainer", order = 0)]
#endif
    public class ShadersContainer : OrderedScriptableObject, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        [SerializeField] private string assetId;
        [SerializeField] private string sName;
        [SerializeField] private string sGroup;
        [SerializeField] private string serializedPropsVersion;
        [SerializeField] private string serializedFieldsVersion;
        [SerializeField] private string[] validKeywords;
        [SerializeField] private string[] invalidKeywords;
        [SerializeField] private Material material;

        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public string Name
        {
            get => sName;
            set => sName = value;
        }

        public string Group
        {
            get => sGroup;
            set => sGroup = value;
        }

        public string SerializedPropsVersion
        {
            get => serializedPropsVersion;
            set => serializedPropsVersion = value;
        }

        public string SerializedFieldsVersion
        {
            get => serializedFieldsVersion;
            set => serializedFieldsVersion = value;
        }

        public string[] ValidKeywords
        {
            get => validKeywords;
            set => validKeywords = value;
        }

        public string[] InvalidKeywords
        {
            get => invalidKeywords;
            set => invalidKeywords = value;
        }

        public Material Material
        {
            get => material;
            set => material = value;
        }

        public Shader Shader => material.shader;
    }
}
