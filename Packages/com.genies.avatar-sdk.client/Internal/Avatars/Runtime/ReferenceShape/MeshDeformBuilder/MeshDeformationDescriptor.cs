using System;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Describes a mesh deformation that can be used to build it with the <see cref="MeshDeformBuilder"/>. Any of
    /// the fields can be left uninitialized so their deformations are ignored (i.e.: if bindposes length is 0 then no
    /// skinning deformations will be applied).
    /// </summary>
    public struct MeshDeformationDescriptor : IDisposable
    {
        public NativeArray<float>     MortphTargetWeights;
        public NativeArray<Matrix4x4> TargetBindposes;
        
        public MeshDeformationDescriptor Copy(Allocator allocator)
        {
            return new MeshDeformationDescriptor
            {
                MortphTargetWeights = new NativeArray<float>(MortphTargetWeights, allocator),
                TargetBindposes     = new NativeArray<Matrix4x4>(TargetBindposes, allocator)
            };
        }
        
        public void Dispose()
        {
            MortphTargetWeights.Dispose();
            TargetBindposes.Dispose();
        }

        public static MeshDeformationDescriptor FromRenderer(SkinnedMeshRenderer renderer, Allocator allocator,
            bool includeMorphTargets = true, bool includeBindposes = true)
        {
            var descriptor = new MeshDeformationDescriptor();
            
            if (includeMorphTargets)
            {
                descriptor.MortphTargetWeights = GetMorphTargetWeights(renderer, allocator);
            }
            else
            {
                descriptor.MortphTargetWeights = new NativeArray<float>(0, allocator, NativeArrayOptions.UninitializedMemory);
            }

            if (includeBindposes)
            {
                descriptor.TargetBindposes = GetBindposes(renderer, allocator);
            }
            else
            {
                descriptor.TargetBindposes = new NativeArray<Matrix4x4>(0, allocator, NativeArrayOptions.UninitializedMemory);
            }

            return descriptor;
        }
        
        public static NativeArray<float> GetMorphTargetWeights(SkinnedMeshRenderer renderer, Allocator allocator)
        {
            Mesh mesh = renderer.sharedMesh;
            var weights = new NativeArray<float>(mesh.blendShapeCount, allocator, NativeArrayOptions.UninitializedMemory);
            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; ++shapeIndex)
            {
                weights[shapeIndex] = renderer.GetBlendShapeWeight(shapeIndex) / mesh.GetBlendShapeFrameWeight(shapeIndex, 0);
            }

            return weights;
        }
        
        public static NativeArray<Matrix4x4> GetBindposes(SkinnedMeshRenderer renderer, Allocator allocator)
        {
            Transform[] bones = renderer.bones;
            var bindposes = new NativeArray<Matrix4x4>(bones.Length, allocator, NativeArrayOptions.UninitializedMemory);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                bindposes[boneIndex] = bones[boneIndex].localToWorldMatrix;
            }

            return bindposes;
        }
    }
}