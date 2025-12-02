using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;

namespace Genies.Avatars
{
    /// <summary>
    /// Some utility methods to check the combinability of materials. Shaders are treated as equal shaders if they have
    /// the same name. This is so duplicated shaders coming from the Addressables content build are properly checked.
    /// </summary>
    public static class MaterialCombinerUtility
    {
        private static readonly Dictionary<string, ShaderProperties> ShaderPropertiesCache = new();

        public static CombinableTextureProperty[] GetCombinableTextureProperties(Shader shader)
        {
            return GetShaderProperties(shader).CombinableTextureProperties;
        }
        
        public static NonCombinableProperty[] GetNonCombinableProperties(Shader shader)
        {
            return GetShaderProperties(shader).NonCombinableProperties;
        }

        public static bool CanCombine(Material left, Material right)
        {

            if (left == right)
            {
                return true;
            }

            if (!left || !right)
            {
                return false;
            }

            if (!left.shader || !right.shader)
            {
                return false;
            }

            if (left.shader.name != right.shader.name)
            {
                return false;
            }

            ShaderProperties properties = GetShaderProperties(left.shader);
            return HaveSameNonCombinablePropertyValues(left, right, properties.NonCombinableProperties);
        }

        public static bool CanCombine(Material left, Material right, NonCombinableProperty[] nonCombinableProperties)
        {
            if (left == right)
            {
                return true;
            }

            if (!left || !right)
            {
                return false;
            }

            if (left.name != right.name)
            {
                return false;
            }

            if (!left.shader || !right.shader)
            {
                return false;
            }

            if (left.shader.name != right.shader.name)
            {
                return false;
            }

            return HaveSameNonCombinablePropertyValues(left, right, nonCombinableProperties);
        }

        public static bool HaveSamePropertyValues(Material left, Material right,
            int propertyId, ShaderPropertyType propertyType)
        {
            return propertyType switch
            {
                ShaderPropertyType.Color   => left.GetColor(propertyId)   == right.GetColor(propertyId),
                ShaderPropertyType.Vector  => left.GetVector(propertyId)  == right.GetVector(propertyId),
                ShaderPropertyType.Float   => left.GetFloat(propertyId)   == right.GetFloat(propertyId),
                ShaderPropertyType.Range   => left.GetFloat(propertyId)   == right.GetFloat(propertyId),
                ShaderPropertyType.Texture => left.GetTexture(propertyId) == right.GetTexture(propertyId),
                ShaderPropertyType.Int     => left.GetInt(propertyId)     == right.GetInt(propertyId),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public static bool HaveSameCombinableTextures(Material left, Material right)
        {
            if (left.shader.name != right.shader.name)
            {
                return false;
            }

            ShaderProperties properties = GetShaderProperties(left.shader);
            return HaveSameCombinableTextures(left, right, properties.CombinableTextureProperties);
        }

        public static bool HaveSameCombinableTextures(Material left, Material right,
            CombinableTextureProperty[] combinableTextureProperties)
        {
            foreach (CombinableTextureProperty property in combinableTextureProperties)
            {
                if (left.GetTexture(property.Id) != right.GetTexture(property.Id))
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Whether the given materials all have the same texture assigned to the given property ID.
        /// </summary>
        public static bool HaveSameTexture(IEnumerable<Material> materials, int texturePropertyId)
        {
            using IEnumerator<Material> enumerator = materials.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return true;
            }

            Texture texture = enumerator.Current.GetTexture(texturePropertyId);

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.GetTexture(texturePropertyId) != texture)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Gets the best texture size to fit all combinable textures from this material as a single texture.
        /// </summary>
        public static Vector2Int GetCombinableTextureSize(Material material)
        {
            CombinableTextureProperty[] combinableTextureProperties = GetCombinableTextureProperties(material.shader);
            return GetCombinableTextureSize(material, combinableTextureProperties);
        }
        
        /// <summary>
        /// Gets the best texture size to fit all combinable textures from this material as a single texture.
        /// </summary>
        public static Vector2Int GetCombinableTextureSize(Material material, IList<CombinableTextureProperty> combinableTextureProperties)
        {
            /**
             * Most of the times textures will be squares, but in case that textures come in different aspect ratios on
             * the different channels, we will pick the aspect ratio of the most "squared" texture and the biggest width
             * or height in pixels.
             */
            int width = 0; // highest width found on a texture
            int height = 0; // highest height
            float squareDeviation = float.PositiveInfinity; // the smallest square deviation found on a texture
            float aspectRatio = 1.0f; // the aspect ratio from the texture with the smallest square deviation
            
            foreach (CombinableTextureProperty property in combinableTextureProperties)
            {
                Texture texture = material.GetTexture(property.Id);
                if (!texture)
                {
                    continue;
                }

                if (texture.width > width)
                {
                    width = texture.width;
                }

                if (texture.height > height)
                {
                    height = texture.height;
                }

                float deviation = GetSquareDeviation(texture.width, texture.height);
                if (deviation >= squareDeviation)
                {
                    continue;
                }

                squareDeviation = deviation;
                aspectRatio = (float)texture.width / texture.height;
            }
            
            if (width >= height)
            {
                height = Mathf.RoundToInt(width / aspectRatio);
            }
            else
            {
                width = Mathf.RoundToInt(aspectRatio * height);
            }

            return new Vector2Int(width, height);
        }
        
        /// <summary>
        /// Returns a number >= 0 representing how much the given rectangle (width and height) deviates from a perfect
        /// square, being 0 a perfect square.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSquareDeviation(in float width, in float height)
            => width > height ? width / height - 1.0f : height / width - 1.0f;
        
        private static bool HaveSameNonCombinablePropertyValues(Material left, Material right, NonCombinableProperty[] nonCombinableProperties)
        {
            // check that all non-combinable properties (non-texture properties and non-two-dimensional textures) have the same values
            foreach (NonCombinableProperty property in nonCombinableProperties)
            {
                if (!HaveSamePropertyValues(left, right, property.Id, property.Type))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private static ShaderProperties GetShaderProperties(Shader shader)
        {
            if (ShaderPropertiesCache.TryGetValue(shader.name, out ShaderProperties properties))
            {
                return properties;
            }

            properties = new ShaderProperties(shader);
            ShaderPropertiesCache.Add(shader.name, properties);
            return properties;
        }
        
        private readonly struct ShaderProperties
        {
            public readonly CombinableTextureProperty[] CombinableTextureProperties;
            public readonly NonCombinableProperty[]         NonCombinableProperties;

            public ShaderProperties(Shader shader)
            {
                int propertyCount = shader.GetPropertyCount();
                var combinableTextureProperties = new List<CombinableTextureProperty>(propertyCount);
                var nonCombinableProperties = new List<NonCombinableProperty>(propertyCount);

                for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
                {
                    ShaderPropertyType type = shader.GetPropertyType(propertyIndex);
                    int id = shader.GetPropertyNameId(propertyIndex);

                    if (type is ShaderPropertyType.Texture && shader.GetPropertyTextureDimension(propertyIndex) is TextureDimension.Tex2D)
                    {
                        string name = shader.GetPropertyName(propertyIndex);
                        Color defaultColor = shader.GetTextureDefaultColor(propertyIndex);
                        combinableTextureProperties.Add(new CombinableTextureProperty(id, name, defaultColor));
                    }
                    else
                    {
                        nonCombinableProperties.Add(new NonCombinableProperty(id, type));
                    }
                }
                
                CombinableTextureProperties = combinableTextureProperties.ToArray();
                NonCombinableProperties = nonCombinableProperties.ToArray();
            }
        }
    }

    /// <summary>
    /// Data from a shader's property that cannot be combined.
    /// </summary>
    public struct NonCombinableProperty
    {
        public int                Id;
        public ShaderPropertyType Type;

        public NonCombinableProperty(int id, ShaderPropertyType type)
        {
            Id = id;
            Type = type;
        }
    }

    /// <summary>
    /// Data from a shader's texture property that can be combined.
    /// </summary>
    public struct CombinableTextureProperty
    {
        public int    Id;
        public string Name;
        public Color  DefaultColor;

        public CombinableTextureProperty(int id, string name, Color defaultColor)
        {
            Id = id;
            Name = name;
            DefaultColor = defaultColor;
        }
    }
}