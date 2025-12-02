using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    // todo remove this container once tested and released on v3
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ShaderPropertyContainer", menuName = "Genies/Shader Prop Container", order = 0)]
#endif
    [Preserve]
    public class ShaderPropertyContainer : ScriptableObject
    {
        public string materialName => shaderProperties.materialName;
        public string materialData => shaderProperties.materialJson;
        public string shaderName => shaderProperties.shaderName;

        // Properties parsed directly from material
        public ShaderPropertiesData shaderProperties;
    }

    [Preserve]
    public static class ShaderPropertyContainerExtensions
    {
        public static Material ToMaterial(this ShaderPropertyContainer spc, Material toOverride = null)
        {
            var savedProperties = spc.shaderProperties?.serializedProperties;
            return ShaderlessMaterialUtility.SetOnlyProperties(savedProperties, toOverride);
        }
    }
}
