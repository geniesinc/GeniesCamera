using System;
using System.Collections.Generic;
using Genies.Utilities;
using UMA;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the data necessary to build a mesh. It uses native array buffers so it should be disposed.
    /// </summary>
    public sealed class MeshData : IDisposable
    {
        // submeshes
        public SubMeshDescriptor[] SubMeshDescriptors;
        public Material[]          Materials;
        
        // index buffer
        public NativeArray<uint> Indices;
        
        // vertex buffers
        public int VertexStart, VertexLength;
        public NativeArray<Vector3> Vertices;
        public NativeArray<Vector3> Normals;
        public NativeArray<Vector4> Tangents;
        public NativeArray<Vector2> Uvs;
        public NativeArray<byte>    BonesPerVertex;
        
        // skinning
        public Dictionary<int, UMATransform> BonesByHash;
        public BindposeData[]                Bindposes;
        public NativeArray<BoneWeight1>      BoneWeights;
        
        // blend shapes
        public BlendShape[] BlendShapes;
        
        public Mesh CreateMesh()
        {
            var mesh = new Mesh();
            ApplyToMesh(mesh);
            return mesh;
        }

        public void ApplyToMesh(Mesh mesh)
        {
            const MeshUpdateFlags dontUpdate = (MeshUpdateFlags)~0;
            
            mesh.Clear();
            
            // build the bindposes matrix array
            var bindposeMatrices = new Matrix4x4[Bindposes.Length];
            for (int i = 0; i < bindposeMatrices.Length; ++i)
            {
                bindposeMatrices[i] = Bindposes[i].Matrix;
            }

            // update vertex buffers
            mesh.SetVertices(            Vertices, VertexStart, VertexLength, dontUpdate);
            mesh.SetNormals (            Normals,  VertexStart, VertexLength, dontUpdate);
            mesh.SetTangents(            Tangents, VertexStart, VertexLength, dontUpdate);
            mesh.SetUVs     (channel: 0, Uvs,      VertexStart, VertexLength, dontUpdate);
            
            // set skinning buffers
            mesh.SetBoneWeights(BonesPerVertex, BoneWeights);
            mesh.bindposes = bindposeMatrices;
            
            // set blend shapes (optional)
            if (BlendShapes is not null)
            {
                foreach (BlendShape blendShape in BlendShapes)
                {
                    mesh.AddBlendShape(blendShape);
                }
            }
            
            // set the index format automatically based on the number of vertices
            mesh.indexFormat = mesh.vertexCount - 1 > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            
            // set index buffer
            mesh.subMeshCount = SubMeshDescriptors.Length;
            for (int submeshIndex = 0; submeshIndex < SubMeshDescriptors.Length; ++submeshIndex)
            {
                SubMeshDescriptor descriptor = SubMeshDescriptors[submeshIndex];
                mesh.SetIndices(
                    Indices,
                    descriptor.indexStart, descriptor.indexCount, descriptor.topology,
                    submeshIndex, calculateBounds: false);
            }
        }

        public void Dispose()
        {
            SubMeshDescriptors = null;
            Materials = null;
            BonesByHash = null;
            Bindposes = null;
            BlendShapes = null;
            
            Indices.Dispose();
            Vertices.Dispose();
            Normals.Dispose();
            Tangents.Dispose();
            Uvs.Dispose();
            BonesPerVertex.Dispose();
            BoneWeights.Dispose();
        }
    }
}