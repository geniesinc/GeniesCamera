using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace Genies.Components.ShaderlessTools
{
    [Preserve]
    public static class MaterialDataExtensions
    {
        public static List<ShaderProperty> ToList(this MaterialData data)
        {
            var savedProps = new List<ShaderProperty>();
            var serializedProps = data.m_SavedProperties;

            // texture properties
            foreach (var texProp in serializedProps.m_TexEnvs)
            {
                var prop = new ShaderProperty(texProp.first, texProp.second, ShaderPropertyType.TexEnv);
                savedProps.Add(prop);
            }

            // Color properties
            foreach (var colorProp in serializedProps.m_Colors)
            {
                var prop = new ShaderProperty(colorProp.first, colorProp.second, ShaderPropertyType.Color);
                savedProps.Add(prop);
            }

            // Float properties
            foreach (var floatProp in serializedProps.m_Floats)
            {
                var prop = new ShaderProperty(floatProp.first, floatProp.second, ShaderPropertyType.Float);
                savedProps.Add(prop);
            }

            // Integer properties
            foreach (var intProps in serializedProps.m_Ints)
            {
                var prop = new ShaderProperty(intProps.first, intProps.second, ShaderPropertyType.Int);
                savedProps.Add(prop);
            }

            return savedProps;
        }

        /// <summary>
        /// Generates a dictionary out of Material Data m_SavedProperties.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Dictionary<string, ShaderProperty> ToDict(this MaterialData data)
        {
            var lis = ToList(data);
            return lis.ToDictionary(prop => prop.name);
        }
    }
}
