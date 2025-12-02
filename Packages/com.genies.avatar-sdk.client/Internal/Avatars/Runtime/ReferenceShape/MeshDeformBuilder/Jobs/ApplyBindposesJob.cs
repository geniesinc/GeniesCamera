using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    [BurstCompile]
    public struct ApplyBindposesJob : IJobParallelFor
    {
        private NativeArray<Vector3> _vertices;
        
        [ReadOnly] private readonly VertexSkinBuffer       _skinBuffer;
        [ReadOnly] private readonly NativeArray<Matrix4x4> _bindposeOffsets;
        
        public ApplyBindposesJob(NativeArray<Vector3> vertices, VertexSkinBuffer skinBuffer, NativeArray<Matrix4x4> bindposeOffsets)
        {
            _vertices        = vertices;
            _skinBuffer      = skinBuffer;
            _bindposeOffsets = bindposeOffsets;
        }

        public void Execute(int vertexIndex)
        {
            int weightCount = _skinBuffer.GetBoneWeightCount(vertexIndex);
            if (weightCount <= 0)
            {
                return;
            }

            Vector3 vertex = Vector3.zero;
            for (int weightIndex = 0; weightIndex < weightCount; ++weightIndex)
            {
                BoneWeight1 boneWeight = _skinBuffer.GetBoneWeight(vertexIndex, weightIndex);
                Matrix4x4   offset     = _bindposeOffsets[boneWeight.boneIndex];
                
                vertex += boneWeight.weight * offset.MultiplyPoint3x4(_vertices[vertexIndex]);
            }
            
            _vertices[vertexIndex] = vertex;
        }
        
        /// <summary>
        /// Gets the bindpose offset matrices from the original bindposes to the target bindposes. Original bindposes
        /// are expected to be the inverted bindposes of the original mesh, while the target bindposes are expected to
        /// be the local to world matrices of the bones.
        /// </summary>
        public static NativeArray<Matrix4x4> GetBindposeOffsets(NativeArray<Matrix4x4> bindposes, NativeArray<Matrix4x4> targetBindposes, Allocator allocator)
        {
            // calculate the bindpose offset matrices for all bindposes (the TRS from the original bindpose to the target)
            var bindposeOffsets = new NativeArray<Matrix4x4>(bindposes.Length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < bindposes.Length; i++)
            {
                bindposeOffsets[i] = targetBindposes[i] * bindposes[i];
            }

            return bindposeOffsets;
        }
    }
}