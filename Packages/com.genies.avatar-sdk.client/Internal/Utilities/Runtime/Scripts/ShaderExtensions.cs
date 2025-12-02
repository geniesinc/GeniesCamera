using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    public static class ShaderExtensions
    {
        /// <summary>
        /// Gets all the shader property names and IDs.
        /// </summary>
        public static (string name, int id, ShaderPropertyType type)[] GetAllProperties(this Shader shader)
        {
            int propertyCount = shader.GetPropertyCount();
            var properties = new (string, int, ShaderPropertyType)[propertyCount];

            for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
            {
                properties[propertyIndex] = (
                    shader.GetPropertyName(propertyIndex),
                    shader.GetPropertyNameId(propertyIndex),
                    shader.GetPropertyType(propertyIndex)
                );
            }

            return properties;
        }

        /// <summary>
        /// Gets all the shader property IDs.
        /// </summary>
        public static int[] GetAllPropertyIds(this Shader shader)
        {
            int propertyCount = shader.GetPropertyCount();
            var propertyIds = new int[propertyCount];

            for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
            {
                propertyIds[propertyIndex] = shader.GetPropertyNameId(propertyIndex);
            }

            return propertyIds;
        }

        /// <summary>
        /// Gets all the shader property names.
        /// </summary>
        public static string[] GetAllPropertyNames(this Shader shader)
        {
            int propertyCount = shader.GetPropertyCount();
            var propertyNames = new string[propertyCount];

            for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
            {
                propertyNames[propertyIndex] = shader.GetPropertyName(propertyIndex);
            }

            return propertyNames;
        }

        /// <summary>
        /// Gets all the shader property names and IDs that are from the specified type.
        /// </summary>
        public static (string name, int id)[] GetPropertiesForType(this Shader shader, ShaderPropertyType propertyType)
        {
            int propertyCount = shader.GetPropertyCount();
            var properties = new List<(string name, int id)>(propertyCount);

            for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
            {
                if (shader.GetPropertyType(propertyIndex) == propertyType)
                {
                    properties.Add((shader.GetPropertyName(propertyIndex), shader.GetPropertyNameId(propertyIndex)));
                }
            }

            return properties.ToArray();
        }

        /// <summary>
        /// Gets all the shader property IDs that are from the specified type.
        /// </summary>
        public static int[] GetPropertyIdsForType(this Shader shader, ShaderPropertyType propertyType)
        {
            int propertyCount = shader.GetPropertyCount();
            var ids = new List<int>(propertyCount);

            for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
            {
                if (shader.GetPropertyType(propertyIndex) == propertyType)
                {
                    ids.Add(shader.GetPropertyNameId(propertyIndex));
                }
            }

            return ids.ToArray();
        }

        /// <summary>
        /// Gets all the shader property names that are from the specified type.
        /// </summary>
        public static string[] GetPropertyNamesForType(this Shader shader, ShaderPropertyType propertyType)
        {
            int propertyCount = shader.GetPropertyCount();
            var names = new List<string>(propertyCount);

            for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
            {
                if (shader.GetPropertyType(propertyIndex) == propertyType)
                {
                    names.Add(shader.GetPropertyName(propertyIndex));
                }
            }

            return names.ToArray();
        }

        public static Color GetTextureDefaultColor(this Shader shader, int propertyIndex)
        {
            /**
             * These are all the default textures allowed by ShaderLab for texture properties. These values where
             * extracted directly from the values in the Texture2D static default texture objects. I have also checked
             * them in ShaderLab and can confirm they are correct. Unity's documentation is wrong about the exact values
             * for the black, gray and red textures.
             *
             * https://docs.unity3d.com/Manual/SL-Properties.html
             */
            return shader.GetPropertyTextureDefaultName(propertyIndex) switch
            {
                "white" => Color.black,
                "black" => Color.clear,
                "gray" => new Color(0.4980392f, 0.4980392f, 0.4980392f, 0.4980392f),
                "bump" => new Color(0.4980392f, 0.4980392f, 1.0f, 1.0f),
                "red" => new Color(1.0f, 0.0f, 0.0f, 0.0f),
                _ => new Color(0.4980392f, 0.4980392f, 0.4980392f, 0.4980392f), // ShaderLab defaults to gray
            };
        }

        public sealed class PropertyComparer : IEqualityComparer<(string name, int id, ShaderPropertyType type)>
        {
            public bool Equals((string name, int id, ShaderPropertyType type) x, (string name, int id, ShaderPropertyType type) y)
                => x.id == y.id;

            public int GetHashCode((string name, int id, ShaderPropertyType type) obj)
                => obj.id;
        }
    }
}
