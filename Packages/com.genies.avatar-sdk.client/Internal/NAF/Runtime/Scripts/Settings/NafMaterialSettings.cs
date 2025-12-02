using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Naf
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "NativeMaterialSettings", menuName = "Genies/NAF/Native Material Settings")]
#endif
    public sealed class NafMaterialSettings : ScriptableObject
    {
        public List<MaterialModel> materialModels;

        /**
         * These materials and shaders are not used in code, but they are here just to ensure they are included in the
         * build. Materials can be used to ensure certain shader variants.
         */
        [Header("To include in your build"), Space(16)]
        public List<Material> includeMaterials;
        public List<Shader>   includeShaders;

        public Material CreateMaterial(string model)
        {
            if (string.IsNullOrEmpty(model) || string.IsNullOrWhiteSpace(model))
            {
                Debug.LogError("Failed to create NAF material: the model is null or empty");
                return null;
            }

            // try to find a predefined model in the settings
            if (materialModels is not null)
            {
                foreach (MaterialModel materialModel in materialModels)
                {
                    if (materialModel.material && materialModel.models.IndexOf(model) >= 0)
                    {
                        return new Material(materialModel.material);
                    }
                }
            }

            // fallback to FindShader
            Shader shader = FindShader(model);
            return shader ? new Material(shader) : null;
        }

        public Shader GetShader(string model)
        {
            if (string.IsNullOrEmpty(model) || string.IsNullOrWhiteSpace(model))
            {
                Debug.LogError("Failed to get NAF shader: the model is null or empty");
                return null;
            }

            // try to find a predefined model in the settings
            if (materialModels is not null)
            {
                foreach (MaterialModel materialModel in materialModels)
                {
                    if (materialModel.material && materialModel.models.IndexOf(model) >= 0)
                    {
                        return materialModel.material.shader;
                    }
                }
            }

            // fallback to FindShader
            return FindShader(model);
        }

        public Material CreateMaterial(Shader shader)
        {
            if (!shader)
            {
                Debug.LogError("Failed to create NAF material: the shader is null or destroyed");
                return null;
            }

            // if there is a predefined material for this shader, use it
            if (materialModels is not null)
            {
                foreach (MaterialModel materialModel in materialModels)
                {
                    if (materialModel.material && materialModel.material.shader == shader)
                    {
                        return new Material(materialModel.material);
                    }
                }
            }

            return new Material(shader);
        }

        public static NafMaterialSettings Default
        {
            get
            {
                if (_default)
                {
                    return _default;
                }

                _default = CreateInstance<NafMaterialSettings>();
                return _default;
            }

            set
            {
                if (value)
                {
                    _default = value;
                }
            }
        }

        private static NafMaterialSettings _default;

        private static Shader FindShader(string model)
        {
            // we usually store Unity shader models prefixed with "unity/"
            if (model.StartsWith("unity/"))
            {
                model = model[6..];
            }

            // try to find a shader named like the model
            var shader = Shader.Find(model);
            if (shader)
            {
                return shader;
            }

            Debug.LogError($"Failed to get NAF shader: unknown material model \"{model}\". Make sure the model is included within the Native Material Settings or the shader is included within the project build");
            return null;
        }

        [Serializable]
        public struct MaterialModel
        {
            public List<string> models;
            public Material     material;
        }
    }
}
