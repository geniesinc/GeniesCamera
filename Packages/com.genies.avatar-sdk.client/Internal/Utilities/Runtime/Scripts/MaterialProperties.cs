using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities
{
    [Serializable]
    public sealed class MaterialProperties
    {
        private static readonly Dictionary<string, int> _propertyIds = new();

        public List<FloatProperty> floats = new();
        public List<IntegerProperty> integers = new();
        public List<TextureProperty> textures = new();
        public List<ColorProperty> colors = new();
        public List<VectorProperty> vectors = new();

        public void ReadValues(Material material)
        {
            for (int i = 0; i < floats.Count; ++i)
            {
                floats[i] = new FloatProperty(floats[i].name, material.GetFloat(GetPropertyId(floats[i].name)));
            }

            for (int i = 0; i < integers.Count; ++i)
            {
                integers[i] = new IntegerProperty(integers[i].name, material.GetInteger(GetPropertyId(integers[i].name)));
            }

            for (int i = 0; i < textures.Count; ++i)
            {
                textures[i] = new TextureProperty(textures[i].name, material.GetTexture(GetPropertyId(textures[i].name)));
            }

            for (int i = 0; i < colors.Count; ++i)
            {
                colors[i] = new ColorProperty(colors[i].name, material.GetColor(GetPropertyId(colors[i].name)));
            }

            for (int i = 0; i < vectors.Count; ++i)
            {
                vectors[i] = new VectorProperty(vectors[i].name, material.GetVector(GetPropertyId(vectors[i].name)));
            }
        }

        public void WriteValues(Material material)
        {
            foreach (FloatProperty property in floats)
            {
                material.SetFloat(GetPropertyId(property.name), property.value);
            }

            foreach (IntegerProperty property in integers)
            {
                material.SetInteger(GetPropertyId(property.name), property.value);
            }

            foreach (TextureProperty property in textures)
            {
                material.SetTexture(GetPropertyId(property.name), property.value);
            }

            foreach (ColorProperty property in colors)
            {
                material.SetColor(GetPropertyId(property.name), property.value);
            }

            foreach (VectorProperty property in vectors)
            {
                material.SetVector(GetPropertyId(property.name), property.value);
            }
        }

        public void RemoveUnusedProperties(Material material)
        {
            for (int i = 0; i < floats.Count; ++i)
            {
                if (!material.HasProperty(GetPropertyId(floats[i].name)))
                {
                    floats.RemoveAt(i--);
                }
            }

            for (int i = 0; i < integers.Count; ++i)
            {
                if (!material.HasProperty(GetPropertyId(integers[i].name)))
                {
                    integers.RemoveAt(i--);
                }
            }

            for (int i = 0; i < textures.Count; ++i)
            {
                if (!material.HasProperty(GetPropertyId(textures[i].name)))
                {
                    textures.RemoveAt(i--);
                }
            }

            for (int i = 0; i < colors.Count; ++i)
            {
                if (!material.HasProperty(GetPropertyId(colors[i].name)))
                {
                    colors.RemoveAt(i--);
                }
            }

            for (int i = 0; i < vectors.Count; ++i)
            {
                if (!material.HasProperty(GetPropertyId(vectors[i].name)))
                {
                    vectors.RemoveAt(i--);
                }
            }
        }

        public void Set(MaterialProperties other)
        {
            floats.Clear();
            integers.Clear();
            textures.Clear();
            colors.Clear();
            vectors.Clear();
            floats.AddRange(other.floats);
            integers.AddRange(other.integers);
            textures.AddRange(other.textures);
            colors.AddRange(other.colors);
            vectors.AddRange(other.vectors);
        }

        public MaterialProperties Copy()
        {
            var copy = new MaterialProperties();
            copy.floats.AddRange(floats);
            copy.integers.AddRange(integers);
            copy.textures.AddRange(textures);
            copy.colors.AddRange(colors);
            copy.vectors.AddRange(vectors);

            return copy;
        }

        private static int GetPropertyId(string name)
        {
            if (_propertyIds.TryGetValue(name, out int id))
            {
                return id;
            }

            return _propertyIds[name] = Shader.PropertyToID(name);
        }

        [Serializable]
        public struct FloatProperty
        {
            public string name;
            public float value;

            public FloatProperty(string name, float value)
            {
                this.name = name;
                this.value = value;
            }
        }

        [Serializable]
        public struct IntegerProperty
        {
            public string name;
            public int value;

            public IntegerProperty(string name, int value)
            {
                this.name = name;
                this.value = value;
            }
        }

        [Serializable]
        public struct TextureProperty
        {
            public string name;
            public Texture value;

            public TextureProperty(string name, Texture value)
            {
                this.name = name;
                this.value = value;
            }
        }

        [Serializable]
        public struct ColorProperty
        {
            public string name;
            public Color value;

            public ColorProperty(string name, Color value)
            {
                this.name = name;
                this.value = value;
            }
        }

        [Serializable]
        public struct VectorProperty
        {
            public string name;
            public Vector4 value;

            public VectorProperty(string name, Vector4 value)
            {
                this.name = name;
                this.value = value;
            }
        }
    }
}
