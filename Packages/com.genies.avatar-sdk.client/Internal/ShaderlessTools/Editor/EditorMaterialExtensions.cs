using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    public static class EditorMaterialExtensions
    {
        public static List<ShaderField> GetShaderFields(Material mat)
        {
            var fields = new List<ShaderField>();

            var key = "ShaderGuid";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat.shader, out var guid, out long localId))
            {
                fields.Add(new ShaderField(key, new StringField(guid), ShaderFieldType.Guid));
            }

            key = "ValidKeywords";
            var validKeywords = mat.GetValidKeyWords();
            fields.Add(new ShaderField(key, new StringListField(validKeywords.ToList()), ShaderFieldType.StringList));

            key = "InvalidKeywords";
            var invalidKeywords = mat.GetInvalidKeyWords();
            fields.Add(new ShaderField(key, new StringListField(invalidKeywords.ToList()), ShaderFieldType.StringList));

            var customRenderQ = mat.renderQueue;
            key = "CustomRenderQueue";
            fields.Add(new ShaderField(key, new IntField(customRenderQ), ShaderFieldType.Int));

            return fields;
        }

        private static string[] GetInvalidKeyWords(this Material mat)
        {
            var allKeyWords = mat.shaderKeywords;
            return allKeyWords.Where(keyWord => !mat.IsKeywordEnabled(keyWord)).ToArray();
        }

        private static string[] GetValidKeyWords(this Material mat)
        {
            var allKeyWords = mat.shaderKeywords;
            return allKeyWords.Where(keyWord => mat.IsKeywordEnabled(keyWord)).ToArray();
        }

        public static List<ShaderProperty> GetShaderProperties(Material mat)
        {
            var shader = mat.shader;
            var propertyCount = shader.GetPropertyCount();
            var shaderProps = new List<ShaderProperty>();
            
            for (var i = 0; i < propertyCount; i++)
            {
                var propertyName = shader.GetPropertyName(i);
                UnityEngine.Rendering.ShaderPropertyType type = shader.GetPropertyType(i);
                Enum.TryParse(type.ToString(), out ShaderPropertyType propertyType);

                object data = propertyType switch
                {
                    ShaderPropertyType.Texture or ShaderPropertyType.TexEnv => mat.GetTextureProp(propertyName),
                    ShaderPropertyType.Float => mat.GetFloatProp(propertyName),
                    ShaderPropertyType.Color => mat.GetColor(propertyName),
                    ShaderPropertyType.Int => mat.GetIntProp(propertyName),
                    ShaderPropertyType.Range => mat.GetFloatProp(propertyName),
                    ShaderPropertyType.Vector => mat.GetVector(propertyName),
                    _ => null,
                };

                shaderProps.Add(new ShaderProperty(propertyName, data, propertyType));
            }
            return shaderProps;
        }

        private static TextureProp GetTextureProp(this Material mat, string propertyName)
        {
            var tex = mat.GetTexture(propertyName);
            var offs = mat.GetTextureOffset(propertyName);
            var scale = mat.GetTextureScale(propertyName);

            var guid = string.Empty;
            if (tex != null)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tex, out guid, out long localId);
            }

            return new TextureProp(tex, offs, scale, guid);
        }

        private static IntProp GetIntProp(this Material mat, string propertyName)
        {
            var value = mat.GetInteger(propertyName);
            return new IntProp(value);
        }

        private static FloatProp GetFloatProp(this Material mat, string propertyName)
        {
            var value = mat.GetFloat(propertyName);
            return new FloatProp(value);
        }

        /// <summary>
        /// Returns a serialized Json copy of material data
        /// </summary>
        public static string ToJson(this Material mat)
        {
            return EditorJsonUtility.ToJson(mat);
        }

        /// <summary>
        /// Writes a json version of the material data at path
        /// using shader name as the name.json
        /// </summary>
        public static void WriteToFile(this Material mat, string path)
        {
            var jsonName = mat.shader.name.Replace('/', '_');
            File.WriteAllText($"{path}/{jsonName}.json", mat.ToJson());
        }

        /// <summary>
        /// Prints all properties for this material
        /// </summary>
        public static void PrintProperties(this Material mat)
        {
            var shader = mat.shader;
            var propertyCount = shader.GetPropertyCount();

            for (var i = 0; i < propertyCount; i++)
            {
                string propertyName = shader.GetPropertyName(i);
                var propertyType = shader.GetPropertyType(i);
                Debug.Log("Property Name: " + propertyName + ", Property Type: " + propertyType);
            }
        }
    }
}
