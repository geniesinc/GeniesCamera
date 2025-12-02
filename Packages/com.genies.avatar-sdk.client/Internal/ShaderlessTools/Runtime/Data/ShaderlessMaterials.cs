using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    [Serializable][Preserve]
    public class ShaderlessMaterials
    {
        public List<MaterialProps> materials;

        public ShaderlessMaterials()
        {
            materials = new List<MaterialProps>();
        }
    }

    [Serializable][Preserve]
    public struct MaterialProps
    {
        public Material material;
        public string hash;
        public ShaderPropertiesData data;
    }
}
