using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Genies.Components.ShaderlessTools
{
    [Serializable][Preserve]
    public class ShaderPropertiesData
    {
        public string materialName;
        public string materialJson;
        public string shaderName;
        public string serializedFieldsVersion;
        public string serializedPropsVersion;

        public List<ShaderField> serializedFields;
        public List<ShaderProperty> serializedProperties;
    }
}
