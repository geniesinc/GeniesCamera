using System;
using UnityEngine;

namespace Genies.Utilities
{
    [Serializable]
    public struct PostProcessedTextureProperty
    {
        public string propertyName;
        public Material postProcessingMaterial;
    }
}
