using UnityEditor;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    public class UnityDeserializer
    {
        public static MaterialData Deserialize(string jsonString)
        {
            var data = JsonUtility.FromJson<MaterialDataJson>(jsonString);

            var texEnvs = data.Material.m_SavedProperties.m_TexEnvs;
            foreach (var texEnv in texEnvs)
            {
                var prop = texEnv.second;
                prop.texture = LoadTextFrom(prop);
            }

            return data.Material;
        }

        private static Texture LoadTextFrom(TextureProperty prop)
        {
            var guid = prop?.m_Texture?.guid;
            return AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
