using System.Collections.Generic;
using System.Runtime.InteropServices;
using GnWrappers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Naf
{
    public static class RuntimeMeshExtensions
    {
        public static Mesh CreateMesh(
            this RuntimeMesh runtimeMesh,
            bool             ignoreSkinning = false,
            bool             recalculateMissingAttributes = false
        ) {
            string id = runtimeMesh.Id();
            var mesh = new Mesh { name = string.IsNullOrEmpty(id) ? "[Unnamed RuntimeMesh]" : id };
            ApplyToMesh(runtimeMesh, mesh, ignoreSkinning, recalculateMissingAttributes, (MeshUpdateFlags)~0);

            return mesh;
        }

        public static void ApplyToMesh(
            this RuntimeMesh runtimeMesh,
            Mesh             mesh,
            bool             ignoreSkinning = false,
            bool             recalculateMissingAttributes = false,
            MeshUpdateFlags  meshUpdateFlags = 0
        ) {
            mesh.Clear();

            if (runtimeMesh.IsNull())
            {
                return;
            }

            // apply all vertex attributes
            using VertexAttributeAccessors vertexAttributes = runtimeMesh.Attributes();
            (bool isMissingNormals, bool isMissingTangents) = vertexAttributes.ApplyRegularAttributesToMesh(mesh, meshUpdateFlags);

            if (!ignoreSkinning)
            {
                vertexAttributes.ApplyBlendAttributesToMesh(mesh);
                ApplyBindposesToMesh(runtimeMesh, mesh);
                ApplyBlendShapesToMesh(runtimeMesh, mesh);
            }

            // apply bounds, indices and submeshes
            ApplyBoundsToMesh(runtimeMesh, mesh);
            ApplyIndicesToMesh(runtimeMesh, mesh);
            ApplySubmeshesToMesh(runtimeMesh, mesh, meshUpdateFlags);

            if (!recalculateMissingAttributes)
            {
                return;
            }

            if (isMissingNormals)
            {
                mesh.RecalculateNormals();
            }

            if (isMissingTangents)
            {
                mesh.RecalculateTangents();
            }
        }

        public static void ApplyBindposesToMesh(this RuntimeMesh runtimeMesh, Mesh mesh)
        {
            using DynamicAccessor bindposesAccessor = runtimeMesh.InverseBindMatrices();
            using NativeArray<Matrix4x4> bindposes = bindposesAccessor.AsNativeArray<Matrix4x4>();
            mesh.bindposes = bindposes.ToArray();
        }

        public static void ApplyBlendShapesToMesh(this RuntimeMesh runtimeMesh, Mesh mesh)
        {
            /**
             * Unfortunatelly, Unity doesn't currently provide any low-level API for blend blendshapes, which means that
             * managed array allocations for the deltas are inevitable. The best we can do is initialize them once and
             * re-use them for every blendshape.
             */
            Vector3[] positionDeltas = null;
            Vector3[] normalDeltas   = null;
            Vector3[] tangentDeltas  = null;

            uint targetCount = runtimeMesh.TargetCount();
            for (uint i = 0; i < targetCount; ++i)
            {
                using MorphTarget              target         = runtimeMesh.GetTarget(i);
                using VertexAttributeAccessors deltaAccessors = target.AttributeDeltas();
                using DynamicAccessor          posAccessor    = deltaAccessors.Get(StandardVertexAttribute.Position);
                using DynamicAccessor          norAccessor    = deltaAccessors.Get(StandardVertexAttribute.Normal);
                using DynamicAccessor          tanAccessor    = deltaAccessors.Get(StandardVertexAttribute.Tangent);

                // create array aliases that can be null if the target doesn't have some of the attributes
                Vector3[] pos = null;
                Vector3[] nor = null;
                Vector3[] tan = null;

                // re-use the upper level arrays (and initialize them if not already) for each attribute delta declared in the target. And copy the data
                if (!posAccessor.Empty())
                {
                    posAccessor.CopyTo(pos = positionDeltas ??= new Vector3[mesh.vertexCount]);
                }

                if (!norAccessor.Empty())
                {
                    norAccessor.CopyTo(nor = normalDeltas ??= new Vector3[mesh.vertexCount]);
                }

                if (!tanAccessor.Empty())
                {
                    tanAccessor.CopyTo(tan = tangentDeltas ??= new Vector3[mesh.vertexCount]);
                }

                mesh.AddBlendShapeFrame(target.Name(), 100.0f, pos, nor, tan);
            }
        }

        public static void ApplyBoundsToMesh(this RuntimeMesh runtimeMesh, Mesh mesh)
        {
            var minBounds = Marshal.PtrToStructure<Vector3>(runtimeMesh.MinBound());
            var maxBounds = Marshal.PtrToStructure<Vector3>(runtimeMesh.MaxBound());
            var bounds = new Bounds();
            bounds.SetMinMax(minBounds, maxBounds);
            mesh.bounds = bounds;
        }

        public static void ApplyIndicesToMesh(this RuntimeMesh runtimeMesh, Mesh mesh)
        {
            using DynamicAccessor indicesAccessor = runtimeMesh.Indices();
            using NativeArray<uint> indices = indicesAccessor.AsNativeArray<uint>();

            // set the index format automatically based on the number of vertices
            mesh.indexFormat = runtimeMesh.VertexCount() - 1 > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetIndices(indices, 0, indices.Length, MeshTopology.Triangles, 0, false);
        }

        public static void ApplySubmeshesToMesh(this RuntimeMesh runtimeMesh, Mesh mesh, MeshUpdateFlags meshUpdateFlags = 0)
        {
            // create submesh descriptors from the runtime mesh primitives
            var descriptors = new SubMeshDescriptor[runtimeMesh.PrimitiveCount()];
            for (int i = 0; i < descriptors.Length; ++i)
            {
                using Primitive primitive = runtimeMesh.GetPrimitive((uint)i);
                descriptors[i] = new SubMeshDescriptor((int)primitive.IndexOffset(), (int)primitive.IndexCount());
            }

            // set the submeshes
            mesh.SetSubMeshes(descriptors, meshUpdateFlags);
        }

        /**
         * Creates new NativeMaterial instances set to the primitive materials and adds them to the given nativeMaterials collection.
         */
        public static void CreatePrimitiveMaterials(this RuntimeMesh runtimeMesh, ICollection<NativeMaterial> nativeMaterials)
        {
            uint primitiveCount = runtimeMesh.PrimitiveCount();
            for (uint i = 0; i < primitiveCount; ++i)
            {
                using Primitive primitive = runtimeMesh.GetPrimitive(i);
                using GnWrappers.Material material = primitive.Material();
                nativeMaterials.Add(new NativeMaterial(material));
            }
        }

        /**
         * Sets the native materials contained in materials to the materials of the primitives. If the given materials
         * list has more materials than the mesh primitives, the extra materials will be disposed and removed from the
         * list. In case the materials list has fewer materials than the mesh primitives, new native materials will be
         * created and added to the list.
         *
         * This method is great for performance since it updates the materials reusing existing instances.
         */
        public static void UpdatePrimitiveMaterials(this RuntimeMesh runtimeMesh, IList<NativeMaterial> materials)
        {
            int primitiveCount = (int)runtimeMesh.PrimitiveCount();

            for (int i = 0; i < primitiveCount; ++i)
            {
                using Primitive primitive = runtimeMesh.GetPrimitive((uint)i);
                using GnWrappers.Material material = primitive.Material();

                if (material == null)
                {
                    Debug.LogError($"Primitive {i} has NULL material!");
                }

                if (materials.Count > i)
                {
                    materials[i].SetMaterial(material);
                }
                else
                {
                    materials.Add(new NativeMaterial(material));
                }
            }

            for (int i = materials.Count - 1; i >= primitiveCount; --i)
            {
                materials[i].Dispose();
                materials.RemoveAt(i);
            }
        }
    }
}
