using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Shaders
{
    /// <summary>
    /// Handy <see cref="Material"/> extensions that allows us to easily get/set properties from the mega shader.
    /// </summary>
    public static class MegaShaderMaterialExtensions
    {
        public const int MaxRegions = 4;

        // maps from the property enums to the shader ID so we don't have to create string allocations every time we request the id to the Shader class
        private static readonly Dictionary<MegaShaderProperty, int> PropertyNameMap;
        private static readonly Dictionary<MegaShaderRegionProperty, int>[] RegionPropertyNameMap;

        static MegaShaderMaterialExtensions()
        {
            PropertyNameMap = new Dictionary<MegaShaderProperty, int>();
            RegionPropertyNameMap = new Dictionary<MegaShaderRegionProperty, int>[MaxRegions];

            for (int regionIndex = 0; regionIndex < RegionPropertyNameMap.Length; ++regionIndex)
            {
                RegionPropertyNameMap[regionIndex] = new Dictionary<MegaShaderRegionProperty, int>();
            }
        }

#region SETTERS
        public static void SetFloat(this Material material, MegaShaderProperty property, float value)
        {
            int propertyID = PropertyToID(property);
            material.SetFloat(propertyID, value);
        }

        public static void SetInt(this Material material, MegaShaderProperty property, int value)
        {
            int propertyID = PropertyToID(property);
            material.SetInt(propertyID, value);
        }

        public static void SetColor(this Material material, MegaShaderProperty property, Color value)
        {
            int propertyID = PropertyToID(property);
            material.SetColor(propertyID, value);
        }

        public static void SetVector(this Material material, MegaShaderProperty property, Vector4 value)
        {
            int propertyID = PropertyToID(property);
            material.SetVector(propertyID, value);
        }

        public static void SetMatrix(this Material material, MegaShaderProperty property, Matrix4x4 value)
        {
            int propertyID = PropertyToID(property);
            material.SetMatrix(propertyID, value);
        }

        public static void SetTexture(this Material material, MegaShaderProperty property, Texture value)
        {
            int propertyID = PropertyToID(property);
            material.SetTexture(propertyID, value);
        }

        public static void SetTexture(this Material material, MegaShaderProperty property, RenderTexture value, RenderTextureSubElement element)
        {
            int propertyID = PropertyToID(property);
            material.SetTexture(propertyID, value, element);
        }

        public static void SetBuffer(this Material material, MegaShaderProperty property, ComputeBuffer value)
        {
            int propertyID = PropertyToID(property);
            material.SetBuffer(propertyID, value);
        }

        public static void SetBuffer(this Material material, MegaShaderProperty property, GraphicsBuffer value)
        {
            int propertyID = PropertyToID(property);
            material.SetBuffer(propertyID, value);
        }

        public static void SetConstantBuffer(this Material material, MegaShaderProperty property, ComputeBuffer value, int offset, int size)
        {
            int propertyID = PropertyToID(property);
            material.SetConstantBuffer(propertyID, value, offset, size);
        }

        public static void SetConstantBuffer(this Material material, MegaShaderProperty property, GraphicsBuffer value, int offset, int size)
        {
            int propertyID = PropertyToID(property);
            material.SetConstantBuffer(propertyID, value, offset, size);
        }

        public static void SetFloatArray(this Material material, MegaShaderProperty property, List<float> values)
        {
            int propertyID = PropertyToID(property);
            material.SetFloatArray(propertyID, values);
        }

        public static void SetFloatArray(this Material material, MegaShaderProperty property, float[] values)
        {
            int propertyID = PropertyToID(property);
            material.SetFloatArray(propertyID, values);
        }

        public static void SetColorArray(this Material material, MegaShaderProperty property, List<Color> values)
        {
            int propertyID = PropertyToID(property);
            material.SetColorArray(propertyID, values);
        }

        public static void SetColorArray(this Material material, MegaShaderProperty property, Color[] values)
        {
            int propertyID = PropertyToID(property);
            material.SetColorArray(propertyID, values);
        }

        public static void SetVectorArray(this Material material, MegaShaderProperty property, List<Vector4> values)
        {
            int propertyID = PropertyToID(property);
            material.SetVectorArray(propertyID, values);
        }

        public static void SetVectorArray(this Material material, MegaShaderProperty property, Vector4[] values)
        {
            int propertyID = PropertyToID(property);
            material.SetVectorArray(propertyID, values);
        }

        public static void SetMatrixArray(this Material material, MegaShaderProperty property, List<Matrix4x4> values)
        {
            int propertyID = PropertyToID(property);
            material.SetMatrixArray(propertyID, values);
        }

        public static void SetMatrixArray(this Material material, MegaShaderProperty property, Matrix4x4[] values)
        {
            int propertyID = PropertyToID(property);
            material.SetMatrixArray(propertyID, values);
        }

        public static void SetTextureOffset(this Material material, MegaShaderProperty property, Vector2 value)
        {
            int propertyID = PropertyToID(property);
            material.SetTextureOffset(propertyID, value);
        }

        public static void SetTextureScale(this Material material, MegaShaderProperty property, Vector2 value)
        {
            int propertyID = PropertyToID(property);
            material.SetTextureScale(propertyID, value);
        }
#endregion

#region GETTERS
        public static float GetFloat(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetFloat(propertyID);
        }

        public static int GetInt(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetInt(propertyID);
        }

        public static Color GetColor(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetColor(propertyID);
        }

        public static Vector4 GetVector(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetVector(propertyID);
        }

        public static Matrix4x4 GetMatrix(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetMatrix(propertyID);
        }

        public static Texture GetTexture(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetTexture(propertyID);
        }

        public static float[] GetFloatArray(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetFloatArray(propertyID);
        }

        public static Color[] GetColorArray(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetColorArray(propertyID);
        }

        public static Vector4[] GetVectorArray(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetVectorArray(propertyID);
        }

        public static Matrix4x4[] GetMatrixArray(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetMatrixArray(propertyID);
        }

        public static void GetFloatArray(this Material material, MegaShaderProperty property, List<float> values)
        {
            int propertyID = PropertyToID(property);
            material.GetFloatArray(propertyID, values);
        }

        public static void GetColorArray(this Material material, MegaShaderProperty property, List<Color> values)
        {
            int propertyID = PropertyToID(property);
            material.GetColorArray(propertyID, values);
        }

        public static void GetVectorArray(this Material material, MegaShaderProperty property, List<Vector4> values)
        {
            int propertyID = PropertyToID(property);
            material.GetVectorArray(propertyID, values);
        }

        public static void GetMatrixArray(this Material material, MegaShaderProperty property, List<Matrix4x4> values)
        {
            int propertyID = PropertyToID(property);
            material.GetMatrixArray(propertyID, values);
        }

        public static Vector2 GetTextureOffset(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetTextureOffset(propertyID);
        }

        public static Vector2 GetTextureScale(this Material material, MegaShaderProperty property)
        {
            int propertyID = PropertyToID(property);
            return material.GetTextureScale(propertyID);
        }
#endregion

#region REGION_SETTERS
        public static void SetFloat(this Material material, MegaShaderRegionProperty property, int regionIndex, float value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetFloat(propertyID, value);
        }

        public static void SetInt(this Material material, MegaShaderRegionProperty property, int regionIndex, int value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetInt(propertyID, value);
        }

        public static void SetColor(this Material material, MegaShaderRegionProperty property, int regionIndex, Color value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetColor(propertyID, value);
        }

        public static void SetVector(this Material material, MegaShaderRegionProperty property, int regionIndex, Vector4 value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetVector(propertyID, value);
        }

        public static void SetMatrix(this Material material, MegaShaderRegionProperty property, int regionIndex, Matrix4x4 value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetMatrix(propertyID, value);
        }

        public static void SetTexture(this Material material, MegaShaderRegionProperty property, int regionIndex, Texture value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetTexture(propertyID, value);
        }

        public static void SetTexture(this Material material, MegaShaderRegionProperty property, int regionIndex, RenderTexture value, RenderTextureSubElement element)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetTexture(propertyID, value, element);
        }

        public static void SetBuffer(this Material material, MegaShaderRegionProperty property, int regionIndex, ComputeBuffer value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetBuffer(propertyID, value);
        }

        public static void SetBuffer(this Material material, MegaShaderRegionProperty property, int regionIndex, GraphicsBuffer value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetBuffer(propertyID, value);
        }

        public static void SetConstantBuffer(this Material material, MegaShaderRegionProperty property, int regionIndex, ComputeBuffer value, int offset, int size)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetConstantBuffer(propertyID, value, offset, size);
        }

        public static void SetConstantBuffer(this Material material, MegaShaderRegionProperty property, int regionIndex, GraphicsBuffer value, int offset, int size)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetConstantBuffer(propertyID, value, offset, size);
        }

        public static void SetFloatArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<float> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetFloatArray(propertyID, values);
        }

        public static void SetFloatArray(this Material material, MegaShaderRegionProperty property, int regionIndex, float[] values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetFloatArray(propertyID, values);
        }

        public static void SetColorArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<Color> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetColorArray(propertyID, values);
        }

        public static void SetColorArray(this Material material, MegaShaderRegionProperty property, int regionIndex, Color[] values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetColorArray(propertyID, values);
        }

        public static void SetVectorArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<Vector4> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetVectorArray(propertyID, values);
        }

        public static void SetVectorArray(this Material material, MegaShaderRegionProperty property, int regionIndex, Vector4[] values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetVectorArray(propertyID, values);
        }

        public static void SetMatrixArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<Matrix4x4> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetMatrixArray(propertyID, values);
        }

        public static void SetMatrixArray(this Material material, MegaShaderRegionProperty property, int regionIndex, Matrix4x4[] values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetMatrixArray(propertyID, values);
        }

        public static void SetTextureOffset(this Material material, MegaShaderRegionProperty property, int regionIndex, Vector2 value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetTextureOffset(propertyID, value);
        }

        public static void SetTextureScale(this Material material, MegaShaderRegionProperty property, int regionIndex, Vector2 value)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.SetTextureScale(propertyID, value);
        }
#endregion

#region REGION_GETTERS
        public static float GetFloat(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetFloat(propertyID);
        }

        public static int GetInt(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetInt(propertyID);
        }

        public static Color GetColor(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetColor(propertyID);
        }

        public static Vector4 GetVector(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetVector(propertyID);
        }

        public static Matrix4x4 GetMatrix(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetMatrix(propertyID);
        }

        public static Texture GetTexture(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetTexture(propertyID);
        }

        public static float[] GetFloatArray(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetFloatArray(propertyID);
        }

        public static Color[] GetColorArray(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetColorArray(propertyID);
        }

        public static Vector4[] GetVectorArray(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetVectorArray(propertyID);
        }

        public static Matrix4x4[] GetMatrixArray(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetMatrixArray(propertyID);
        }

        public static void GetFloatArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<float> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.GetFloatArray(propertyID, values);
        }

        public static void GetColorArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<Color> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.GetColorArray(propertyID, values);
        }

        public static void GetVectorArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<Vector4> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.GetVectorArray(propertyID, values);
        }

        public static void GetMatrixArray(this Material material, MegaShaderRegionProperty property, int regionIndex, List<Matrix4x4> values)
        {
            int propertyID = PropertyToID(property, regionIndex);
            material.GetMatrixArray(propertyID, values);
        }

        public static Vector2 GetTextureOffset(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetTextureOffset(propertyID);
        }

        public static Vector2 GetTextureScale(this Material material, MegaShaderRegionProperty property, int regionIndex)
        {
            int propertyID = PropertyToID(property, regionIndex);
            return material.GetTextureScale(propertyID);
        }
#endregion

        private static int PropertyToID(MegaShaderProperty property)
        {
            if (PropertyNameMap.TryGetValue(property, out int id))
            {
                return id;
            }

            return PropertyNameMap[property] = Shader.PropertyToID($"_{property}");
        }

        private static int PropertyToID(MegaShaderRegionProperty property, int regionIndex)
        {
            if (regionIndex < 0 || regionIndex > RegionPropertyNameMap.Length)
            {
                return 0;
            }

            if (RegionPropertyNameMap[regionIndex].TryGetValue(property, out int id))
            {
                return id;
            }

            return RegionPropertyNameMap[regionIndex][property] = Shader.PropertyToID($"_Area{regionIndex + 1}{property}");
        }
    }
}
