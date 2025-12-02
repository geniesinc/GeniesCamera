using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Can convert materials between different shaders.
    /// </summary>
    public abstract class MaterialConverter : ScriptableObject
    {
        /// <summary>
        /// Whether or not this converter can convert the given material.
        /// </summary>
        public abstract bool CanConvert(Material material);
        
        /// <summary>
        /// Converts the given material without checking if the material can be converted.
        /// </summary>
        /// <param name="textureCopier">Optional texture copier to be used if texture copying or name validation is performed</param>
        public abstract Ref<Material> ConvertWithoutCheck(Material material, TextureCopier textureCopier = null);

        /// <summary>
        /// Converts the given material without checking if the material can be converted.
        /// </summary>
        /// <param name="textureCopier">Optional texture copier to be used if texture copying or name validation is performed</param>
        public abstract UniTask<Ref<Material>> ConvertWithoutCheckAsync(Material material, TextureCopier textureCopier = null);

        /// <summary>
        /// Tries to convert the given <see cref="Material"/>. It will return a dead reference if it cannot be converted.
        /// </summary>
        /// <param name="textureCopier">Optional texture copier to be used if texture copying or name validation is performed</param>
        public virtual Ref<Material> Convert(Material material, TextureCopier textureCopier = null)
        {
            if (!material)
            {
                return default;
            }

            if (CanConvert(material))
            {
                return ConvertWithoutCheck(material, textureCopier);
            }

            Debug.LogError($"[{nameof(MaterialConverter)}] couldn't convert the given material. Material: {material.name} | Shader: {material.shader.name}");
            return default;
        }
        
        /// <summary>
        /// Tries to convert the given <see cref="Material"/>. It will return a dead reference if it cannot be converted.
        /// </summary>
        /// <param name="textureCopier">Optional texture copier to be used if texture copying or name validation is performed</param>
        public virtual UniTask<Ref<Material>> ConvertAsync(Material material, TextureCopier textureCopier = null)
        {
            if (!material)
            {
                return UniTask.FromResult(default(Ref<Material>));
            }

            if (CanConvert(material))
            {
                return ConvertWithoutCheckAsync(material, textureCopier);
            }

            Debug.LogError($"[{nameof(MaterialConverter)}] couldn't convert the given material. Material: {material.name} | Shader: {material.shader.name}");
            return UniTask.FromResult(default(Ref<Material>));
        }
    }
}
