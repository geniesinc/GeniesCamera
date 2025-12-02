using System;
using Genies.Utilities;
using UMA;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    [Serializable]
    public struct UmaMaterialExportMap
    {
        [FormerlySerializedAs("mapExporter")] public MaterialMapExporter MapExporter;
        [FormerlySerializedAs("splitTextureSettings")] public SplitTextureSettings SplitTextureSettings;
        [FormerlySerializedAs("umaChannel")] public UMAMaterial.MaterialChannel UmaChannel;
        [FormerlySerializedAs("postProcessingMaterial")] public Material PostProcessingMaterial;
    }
}
