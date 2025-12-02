using System;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    /// <summary>
    ///  Runtime version of ShaderUtil.ShaderPropertyType
    /// </summary>
    [Preserve]
    public enum ShaderPropertyType
    {
        Color,
        Vector,
        Float,
        Range,
        TexEnv,
        Int,
        Texture,
    }

    [Serializable][Preserve]
    public class ShaderProperty
    {
        public string name;
        public ShaderPropertyType type;
        [SerializeReference] public object Data;

        public ShaderProperty()
        {
        }

        public ShaderProperty(string name, object data, ShaderPropertyType type)
        {
            this.name = name;
            this.Data = data;
            this.type = type;
        }
    }

    [Serializable][Preserve]
    public class TextureProp
    {
        public Texture texture;
        public Vector2 offset;
        public Vector2 scale;
        public string guid;

        public TextureProp()
        {
        }

        public TextureProp(Texture texture, Vector2 offset, Vector2 scale, string guid = null)
        {
            this.texture = texture;
            this.offset = offset;
            this.scale = scale;
            this.guid = guid;
        }
    }

    /// <summary>
    /// Needed because SerializedReference does not support primitive types
    /// </summary>
    [Serializable][Preserve]
    public class IntProp
    {
        public int value;

        public IntProp()
        {
        }

        public IntProp(int value)
        {
            this.value = value;
        }
    }

    /// <summary>
    /// Needed because SerializedReference does not support primitive types
    /// </summary>
    [Serializable][Preserve]
    public class FloatProp
    {
        public float value;

        public FloatProp()
        {
        }

        public FloatProp(float value)
        {
            this.value = value;
        }
    }

    [Preserve]
    public static class ShaderPropertyExtensions
    {
        public static int GetAsInt(this ShaderProperty prop)
        {
            var data = prop.Data as IntProp;
            return data?.value ?? 0;
        }

        public static float GetAsFloat(this ShaderProperty prop)
        {
            var data = prop.Data as FloatProp;
            return data?.value ?? 0f;
        }

        public static Color GetAsColor(this ShaderProperty prop)
        {
            return prop.Data is Color data ? data : default;
        }

        public static Vector4 GetAsVector(this ShaderProperty prop)
        {
            return prop.Data is Vector4 data ? data : default;
        }

        public static TextureProp GetAsTextureProp(this ShaderProperty prop)
        {
            return prop.Data as TextureProp;
        }
    }
}
