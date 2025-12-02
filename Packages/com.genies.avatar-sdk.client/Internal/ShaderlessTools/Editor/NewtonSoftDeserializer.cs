using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
//using System.Text.Json; // not supported in il2cpp
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;


namespace Genies.Components.ShaderlessTools
{
    public static class NewtonSoftDeserializer
    {
        public static MaterialData DeserializePartial(string json)
        {
            var obj = JObject.Parse(json);
            var matData = new MaterialData();

            // var ver = JsonObjectUtility.FindProperty(obj, "serializedVersion");
            var token = obj.FindToken("serializedVersion");
            matData.serializedVersion = token.Value<string>();

            // m_Shader
            var shaderData = new ShaderData();
            var shaderToken = obj.FindToken("m_Shader");
            shaderData.fileID = shaderToken.FindToken("fileID").Value<long>();
            shaderData.guid = shaderToken.FindToken("guid").Value<string>();
            shaderData.type = shaderToken.FindToken("type").Value<int>();
            matData.m_Shader = shaderData;

            // m_SavedProperties
            var savedProps = new SavedProperties();
            var savedPropsToken = obj.FindToken("m_SavedProperties");
            savedProps.serializedVersion = savedPropsToken.FindToken("serializedVersion").Value<string>();

            var listProp = savedPropsToken.FindToken("m_TexEnvs");
            var texts = listProp.ToObject<List<TexEnv>>();
            savedProps.m_TexEnvs = texts.Select(LoadTexture).ToList();

            listProp = savedPropsToken.FindToken("m_Floats");
            savedProps.m_Floats = listProp.ToObject<List<FloatProperty>>();

            listProp = savedPropsToken.FindToken("m_Colors");
            savedProps.m_Colors = listProp.ToObject<List<ColorProperty>>();

            listProp = savedPropsToken.FindToken("m_Ints");
            savedProps.m_Ints = listProp.ToObject<List<IntegerProperty>>();
            matData.m_SavedProperties = savedProps;

            return matData;
        }

        public static MaterialData Deserialize(string json)
        {
            var data = JsonConvert.DeserializeObject<MaterialDataJson>(json);
            return data.Material;
        }

        private static TexEnv LoadTexture(this TexEnv texEnv)
        {
            var guid = texEnv?.second?.m_Texture?.guid;
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));
                texEnv.second.texture = tex;
            }

            return texEnv;
        }
    }
}
