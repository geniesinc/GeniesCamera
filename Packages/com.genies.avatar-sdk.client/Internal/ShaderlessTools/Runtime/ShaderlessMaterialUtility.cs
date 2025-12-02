using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    public class ShaderlessMaterialUtility
    {
        /// <summary>
        /// Sets shader, fields and properties on a MaterialProps.Material using templateMaterial
        /// </summary>
        public static void SetShaderProps(MaterialProps materialProp, Material templateMaterial)
        {
            // set shader
            materialProp.material.shader = templateMaterial.shader;
            // re-apply all properties to the material
            SetShaderPropsData(materialProp.data, materialProp.material);
        }

        /// <summary>
        /// Sets shader, fields and properties on a given material or creates a new material if the provided material is null.
        /// </summary>
        public static Material SetShaderPropsData(ShaderPropertiesData propertiesData, Material material = null)
        {
            var shaderName = propertiesData.shaderName;
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
            }

            if (material.shader == null)
            {
                material.shader = Shader.Find(shaderName);
            }

            // set fields
            var savedFields = propertiesData.serializedFields;
            material = SetFields(material, savedFields);

            // set properties
            var savedProperties = propertiesData.serializedProperties;
            foreach (var property in savedProperties)
            {
                SetProperty(property, material);
            }

            return material;
        }

        private static Material SetFields(Material mat, List<ShaderField> fields)
        {
            // set keywords
            var allKeyWords = new List<string>();

            var validKeys = fields.First(x => x.name == "ValidKeywords");
            allKeyWords.AddRange(validKeys.GetAsStringList());

            var invalidKeywords = fields.First(x => x.name == "InvalidKeywords");
            allKeyWords.AddRange(invalidKeywords.GetAsStringList());

            mat.shaderKeywords = allKeyWords.ToArray();

            var renderQ = fields.First(x => x.name == "CustomRenderQueue");
            mat.renderQueue = renderQ.GetAsInt();

            return mat;
        }

        /// <summary>
        /// Will only set properties on a material that matches the shader
        /// </summary>
        public static Material SetOnlyProperties(List<ShaderProperty> properties, Material material)
        {
            foreach (var property in properties)
            {
                SetProperty(property, material);
            }

            return material;
        }

        private static void SetProperty(ShaderProperty property, Material mat)
        {
            switch (property.type)
            {
                case ShaderPropertyType.Int:
                    mat.SetInteger(property.name, property.GetAsInt());
                    break;
                case ShaderPropertyType.Color:
                    mat.SetColor(property.name, property.GetAsColor());
                    break;
                case ShaderPropertyType.Float:
                    mat.SetFloat(property.name, property.GetAsFloat());
                    break;
                case ShaderPropertyType.TexEnv:
                case ShaderPropertyType.Texture:
                    TextureProp tex = property.GetAsTextureProp();
                    mat.SetTexture(property.name, tex.texture);
                    mat.SetTextureOffset(property.name, tex.offset);
                    mat.SetTextureScale(property.name, tex.scale);
                    break;
                case ShaderPropertyType.Range:
                    mat.SetFloat(property.name, property.GetAsFloat());
                    break;
                case ShaderPropertyType.Vector:
                    mat.SetVector(property.name, property.GetAsVector());
                    break;
            }
        }
    }
}
