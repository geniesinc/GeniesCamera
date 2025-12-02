using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    public struct VertexSkinBuffer : IDisposable
    {
        public readonly int VertexCount;
        
        [ReadOnly] private NativeArray<BoneWeight1> _boneWeights;
        [ReadOnly] private NativeArray<byte>        _bonesPerVertex;
        [ReadOnly] private NativeArray<int>         _boneWeightOffsetsPerVertex;

        public VertexSkinBuffer(NativeArray<byte> bonesPerVertex, NativeArray<BoneWeight1> boneWeights, Allocator allocator)
        {
            VertexCount = bonesPerVertex.Length;
            
            _bonesPerVertex = bonesPerVertex;
            _boneWeights    = boneWeights;
            
            // initialize the bone weight offests per vertex
            _boneWeightOffsetsPerVertex = new NativeArray<int>(VertexCount, allocator, NativeArrayOptions.UninitializedMemory);
            
            int weightIndex = 0;
            for (int vertexIndex = 0; vertexIndex < VertexCount; ++vertexIndex)
            {
                _boneWeightOffsetsPerVertex[vertexIndex] = weightIndex;
                weightIndex += _bonesPerVertex[vertexIndex];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BoneWeight1 GetBoneWeight(int vertexIndex, int weightIndex)
            => _boneWeights[_boneWeightOffsetsPerVertex[vertexIndex] + weightIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetBoneWeightCount(int vertexIndex)
            => _bonesPerVertex[vertexIndex];

        public void Dispose()
        {
            _boneWeights.Dispose();
            _bonesPerVertex.Dispose();
            _boneWeightOffsetsPerVertex.Dispose();
        }
    }
}