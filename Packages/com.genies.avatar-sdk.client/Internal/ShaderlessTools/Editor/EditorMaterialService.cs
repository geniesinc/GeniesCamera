using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    public class EditorMaterialService
    {
        public static string HashMaterialData(ShaderPropertiesData data)
        {
            var jsonObject = JsonConvert.DeserializeObject<JObject>(data.materialJson);
            var materialObject = jsonObject["Material"] as JObject;
            materialObject["m_Shader"] = data.shaderName;
            materialObject["m_Name"] = "";

            // Iterate through properties and set "m_Scale" to the specified value
            foreach (var property in jsonObject.DescendantsAndSelf().OfType<JProperty>().Where(p => p.Name == "m_Scale"))
            {
                property.Value = new JObject
                {
                    ["x"] = 1.0,
                    ["y"] = 1.0
                };
            }

            // Iterate through properties and set "m_Scale" to the specified value
            foreach (var property in jsonObject.DescendantsAndSelf().OfType<JProperty>().Where(p => p.Name == "m_Offset"))
            {
                property.Value = new JObject
                {
                    ["x"] = 0.0,
                    ["y"] = 0.0
                };
            }

            // Find and update the 'guid' properties inside "m_TexEnvs"
            var guidProperties = jsonObject.Descendants()
                .OfType<JProperty>()
                .Where(p => p.Name == "m_TexEnvs")
                .Descendants()
                .OfType<JProperty>()
                .Where(p => p.Name == "guid");

            foreach (var guidProp in guidProperties)
            {
                guidProp.Value = "0000000000";
            }

            return GenerateSHA128Hash(jsonObject.ToString());
        }

        /// <summary>
        /// Get the hash for the catalog
        /// </summary>
        /// <param name="input"></param>
        private static string GenerateSHA128Hash(string input)
        {
            using SHA256 sha256Hash = SHA256.Create();
            // Compute full SHA-256 hash from the input string
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] fullHashBytes = sha256Hash.ComputeHash(inputBytes);

            // Truncate the hash to 128 bits (16 bytes)
            byte[] truncatedHashBytes = new byte[16];
            System.Array.Copy(fullHashBytes, truncatedHashBytes, 16);

            // Convert truncated hash byte array to a string representation (hexadecimal)
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < truncatedHashBytes.Length; i++)
            {
                builder.Append(truncatedHashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static ShaderlessMaterials GetShaderlessMaterials(Object asset)
        {
            var shaderlessMaterials = new ShaderlessMaterials()
            {
                materials = GetMaterialProps(asset)
            };
            return shaderlessMaterials;
        }

        private static List<MaterialProps> GetMaterialProps(Object asset)
        {
            var materials = FindAllMaterials(asset);
            var listOfProps = new List<MaterialProps>();
            foreach (var material in materials)
            {
                var materialProps = GetShaderPropertiesData(material);
                var matProps = new MaterialProps()
                {
                    data = materialProps,
                    material = material,
                };
                listOfProps.Add(matProps);
            }

            return listOfProps;
        }


        public static List<Material> FindAllMaterials(Object asset)
        {
            var materialGuids = GetAllMaterialGuids(asset);
            var materialPaths = materialGuids.Select(AssetDatabase.GUIDToAssetPath);
            return materialPaths.Select(AssetDatabase.LoadAssetAtPath<Material>).ToList();
        }

        public static List<string> GetAllMaterialGuids(Object asset)
        {
            var guids = new List<string>();

            if (asset is null)
            {
                return guids;
            }

            var dependencies = EditorUtility.CollectDependencies(new Object[] {asset});
            var materials = dependencies.Where(d => d.GetType() == typeof(UnityEngine.Material)).ToList();
            foreach (var mat in materials)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat, out var guid, out long localId))
                {
                    guids.Add(guid);
                }
            }

            return guids;
        }

        public static ShaderPropertiesData GetShaderPropertiesData(Material mat)
        {
            var json = EditorJsonUtility.ToJson(mat);
            var data = UnityDeserializer.Deserialize(json);

            var shaderProps = new ShaderPropertiesData();
            shaderProps.materialName = mat.name;
            shaderProps.materialJson = json;
            shaderProps.shaderName = mat.shader.name;

            shaderProps.serializedFieldsVersion = data.serializedVersion;
            shaderProps.serializedFields = EditorMaterialExtensions.GetShaderFields(mat);

            shaderProps.serializedPropsVersion = data.m_SavedProperties.serializedVersion;
            shaderProps.serializedProperties = EditorMaterialExtensions.GetShaderProperties(mat);
            // todo null out mat once release of v3
            // null out material will break any subsequent processing due to all assets using the same mat.
            // mat.shader = null;
            return shaderProps;
        }

        public static List<string> ValidateShaders(List<MaterialProps> materials)
        {
            var invalidList = new List<string>();

            foreach (var materialProp in materials)
            {
                var shaderGroup = materialProp.data.shaderName.Split('/')[0];
                if (!SupportedShaders.GroupList.Contains(shaderGroup))
                {
                    if (!invalidList.Contains(materialProp.data.shaderName))
                    {
                        invalidList.Add(materialProp.data.shaderName);
                    }
                }
            }

            return invalidList;
        }

        public static List<string> ValidateShaders(List<Material> materials)
        {
            var invalidList = new List<string>();

            foreach (var material in materials)
            {
                var shaderGroup = material.shader.name.Split('/')[0];
                if (!SupportedShaders.GroupList.Contains(shaderGroup))
                {
                    if (!invalidList.Contains(material.shader.name))
                    {
                        invalidList.Add(material.shader.name);
                    }
                }
            }

            return invalidList;
        }

        public static ShaderPropertyContainer ExportToPropsContainer(Material mat)
        {
            var propsData = GetShaderPropertiesData(mat);
            var container = ScriptableObject.CreateInstance<ShaderPropertyContainer>();
            container.shaderProperties = propsData;
            mat.shader = null;
            return container;
        }

        public static Material ImportFromPropsContainer(ShaderPropertyContainer shaderPropContainer, Material material = null)
        {
            var shaderName = shaderPropContainer.shaderName;
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
            }

            material.shader = Shader.Find(shaderName);
            EditorJsonUtility.FromJsonOverwrite(shaderPropContainer.materialData, material);
            return material;
        }
    }
}
