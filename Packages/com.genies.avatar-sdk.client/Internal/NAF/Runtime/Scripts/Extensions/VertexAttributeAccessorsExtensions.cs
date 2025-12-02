using System;
using GnWrappers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

using VertexAttribute = GnWrappers.VertexAttribute;

namespace Genies.Naf
{
    public static class VertexAttributeAccessorsExtensions
    {
        public delegate void AttributeProcessor<T>(in NativeArray<T> attributeArray) where T : struct;

        public static void Process<T>(this VertexAttributeAccessors attributeAccessors, uint entryIndex, AttributeProcessor<T> processor, Action onEmpty = null)
            where T : struct
        {
            using DynamicAccessor accessor = attributeAccessors.Get(entryIndex);
            if (accessor.Empty())
            {
                onEmpty?.Invoke();
                return;
            }

            using NativeArray<T> attributeArray = accessor.AsNativeArray<T>();
            processor(in attributeArray);
        }

        public static void Process<T>(this VertexAttributeAccessors attributeAccessors, string entryName, AttributeProcessor<T> processor, Action onEmpty = null)
            where T : struct
        {
            using DynamicAccessor accessor = attributeAccessors.Get(entryName);
            if (accessor.Empty())
            {
                onEmpty?.Invoke();
                return;
            }

            using NativeArray<T> attributeArray = accessor.AsNativeArray<T>();
            processor(in attributeArray);
        }

        public static void Process<T>(this VertexAttributeAccessors attributeAccessors, StandardVertexAttribute attribute, uint channelIndex, AttributeProcessor<T> processor, Action onEmpty = null)
            where T : struct
        {
            using DynamicAccessor accessor = attributeAccessors.Get(attribute, channelIndex);
            if (accessor.Empty())
            {
                onEmpty?.Invoke();
                return;
            }

            using NativeArray<T> attributeArray = accessor.AsNativeArray<T>();
            processor(in attributeArray);
        }

        public static void Process<T>(this VertexAttributeAccessors attributeAccessors, StandardVertexAttribute attribute, AttributeProcessor<T> processor, Action onEmpty = null)
            where T : struct
        {
            using DynamicAccessor accessor = attributeAccessors.Get(attribute);
            if (accessor.Empty())
            {
                onEmpty?.Invoke();
                return;
            }

            using NativeArray<T> attributeArray = accessor.AsNativeArray<T>();
            processor(in attributeArray);
        }

        public static void Process<T>(this VertexAttributeAccessors attributeAccessors, VertexAttribute attribute, uint channelIndex, AttributeProcessor<T> processor, Action onEmpty = null)
            where T : struct
        {
            using DynamicAccessor accessor = attributeAccessors.Get(attribute, channelIndex);
            if (accessor.Empty())
            {
                onEmpty?.Invoke();
                return;
            }

            using NativeArray<T> attributeArray = accessor.AsNativeArray<T>();
            processor(in attributeArray);
        }

        public static void Process<T>(this VertexAttributeAccessors attributeAccessors, VertexAttribute attribute, AttributeProcessor<T> processor, Action onEmpty = null)
            where T : struct
        {
            using DynamicAccessor accessor = attributeAccessors.Get(attribute);
            if (accessor.Empty())
            {
                onEmpty?.Invoke();
                return;
            }

            using NativeArray<T> attributeArray = accessor.AsNativeArray<T>();
            processor(in attributeArray);
        }

        public static (bool isMissingNormals, bool isMissingTangents) ApplyRegularAttributesToMesh(this VertexAttributeAccessors vertexAttributes, Mesh mesh, MeshUpdateFlags meshUpdateFlags = 0)
        {
            (bool isMissingNormals, bool isMissingTangents) result = (false, false);

            vertexAttributes.Process(
                StandardVertexAttribute.Position,
                (in NativeArray<Vector3> array) => mesh.SetVertices(array, 0, array.Length, meshUpdateFlags),
                () => mesh.SetVertices(Array.Empty<Vector3>(), 0, 0, meshUpdateFlags)
            );

            vertexAttributes.Process(
                StandardVertexAttribute.Normal,
                (in NativeArray<Vector3> array) => mesh.SetNormals(array, 0, array.Length, meshUpdateFlags),
                () =>
                {
                    mesh.SetNormals(Array.Empty<Vector3>(), 0, 0, meshUpdateFlags);
                    result.isMissingNormals = true;
                }
            );

            vertexAttributes.Process(
                StandardVertexAttribute.Tangent,
                (in NativeArray<Vector4> array) => mesh.SetTangents(array, 0, array.Length, meshUpdateFlags),
                () =>
                {
                    mesh.SetTangents(Array.Empty<Vector4>(), 0, 0, meshUpdateFlags);
                    result.isMissingTangents = true;
                }
            );

            vertexAttributes.Process(
                StandardVertexAttribute.Color,
                (in NativeArray<Color> array) => mesh.SetColors(array, 0, array.Length, meshUpdateFlags),
                () => mesh.SetColors(Array.Empty<Color>(), 0, 0, meshUpdateFlags)
            );

            // Unity supports up to 8 UV channels
            for (uint i = 0; i < 8; ++i)
            {
                vertexAttributes.Process(
                    StandardVertexAttribute.TexCoord, i,
                    (in NativeArray<Vector2> array) => mesh.SetUVs((int)i, array, 0, array.Length, meshUpdateFlags),
                    () => mesh.SetUVs((int)i, Array.Empty<Vector2>(), 0, 0, meshUpdateFlags)
                );
            }

            return result;
        }

        public static void ApplyBlendAttributesToMesh(this VertexAttributeAccessors vertexAttributes, Mesh mesh)
        {
            // create a native UnityBoneWeights and build the Unity specific bone weight arrays from our vertex attributes
            using var boneWeights = new UnityBoneWeights();
            boneWeights.Update(vertexAttributes);

            // extract the native arrays and set them to the mesh
            using DynamicAccessor bonesPerVertexAccessor = boneWeights.BonesPerVertex();
            using DynamicAccessor weightsAccessor        = boneWeights.Weights();

            using NativeArray<byte>        bonesPerVertex = bonesPerVertexAccessor.AsNativeArray<byte>();
            using NativeArray<BoneWeight1> weights        = weightsAccessor.AsNativeArray<BoneWeight1>();

            mesh.SetBoneWeights(bonesPerVertex, weights);
        }
    }
}
