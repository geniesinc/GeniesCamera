using UnityEngine;

namespace Genies.Shaders
{
    /// <summary>
    /// Used to make our shaders available from code and to be automatically included on builds.
    /// <br/><br/>
    /// The reason why we reference the shader through a <see cref="Material"/> instance instead of a <see cref="Shader"/>
    /// is because this way we can have a default material configuration as well as properly including the default textures.
    /// Default textures in shader graph assets will not be included when having the asset in a Resources folder.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ShaderInfo", menuName = "Genies/Shader Info")]
#endif
    public sealed class GeniesShaderInfo : ScriptableObject
    {
        [SerializeField]
        private Material material;
        [SerializeField]
        private string version;

        public Shader Shader => material.shader;
        public string Version => version;

        /// <summary>
        /// Creates a new material with the default shader configuration.
        /// </summary>
        public Material NewMaterial()
        {
            return new Material(material);
        }
    }
}
