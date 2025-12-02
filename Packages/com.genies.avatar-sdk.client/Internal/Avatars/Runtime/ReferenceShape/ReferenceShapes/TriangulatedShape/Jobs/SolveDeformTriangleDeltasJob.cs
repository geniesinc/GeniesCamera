using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Given a set of vertices and a set of deformed vertices, along with the common triangle indices, this job will
    /// output the delta matrices for each triangle.
    /// </summary>
    [BurstCompile]
    public struct SolveDeformTriangleDeltasJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<Matrix4x4> TriangleDeltas;
        
        [ReadOnly] private readonly NativeArray<Vector3> _vertices;
        [ReadOnly] private readonly NativeArray<Vector3> _deformedVertices;
        [ReadOnly] private readonly NativeArray<int>     _indices;
        
        public SolveDeformTriangleDeltasJob(NativeArray<Vector3> vertices, NativeArray<Vector3> deformedVertices, NativeArray<int> indices, Allocator allocator)
        {
            if (indices.Length % 3 != 0)
            {
                throw new Exception("The indices array must have a length that is a multiple of 3");
            }

            if (vertices.Length != deformedVertices.Length)
            {
                throw new Exception("The vertices and deformed vertices arrays must have the same length");
            }

            TriangleDeltas = new NativeArray<Matrix4x4>(indices.Length / 3, allocator, NativeArrayOptions.UninitializedMemory);
            
            _vertices         = vertices;
            _deformedVertices = deformedVertices;
            _indices          = indices;
        }
        
        public void Execute(int index)
        {
            int baseIndex = index * 3;
            int vertexIndex0 = _indices[baseIndex];
            int vertexIndex1 = _indices[baseIndex + 1];
            int vertexIndex2 = _indices[baseIndex + 2];
            
            Matrix4x4 referenceTransform = MeshTriangles.GetLocalToWorldMatrix(
                _vertices[vertexIndex0],
                _vertices[vertexIndex1],
                _vertices[vertexIndex2]
            );
            
            Matrix4x4 deformTransform = MeshTriangles.GetLocalToWorldMatrix(
                _deformedVertices[vertexIndex0],
                _deformedVertices[vertexIndex1],
                _deformedVertices[vertexIndex2]
            );
            
            TriangleDeltas[index] = deformTransform * referenceTransform.inverse;
        }
    }
}