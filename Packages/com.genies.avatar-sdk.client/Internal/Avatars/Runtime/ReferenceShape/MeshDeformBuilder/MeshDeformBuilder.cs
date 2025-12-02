using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Performant and fast implementation for building mesh deformations using blend shapes and skinning.
    /// </summary>
    public sealed class MeshDeformBuilder : IDisposable
    {
        public readonly int VertexCount;
        public readonly int MorphTargetCount;
        public readonly int BindposeCount;
        
        // vertices and morph targets
        private NativeArray<Vector3> _vertices;
        private NativeArray<Vector3> _morphTargetDeltas;
        
        // skinning
        private VertexSkinBuffer       _skinBuffer;
        private NativeArray<Matrix4x4> _bindposes;

        public MeshDeformBuilder(Mesh mesh, Allocator allocator)
        {
            VertexCount      = mesh.vertexCount;
            MorphTargetCount = mesh.blendShapeCount;
            BindposeCount    = mesh.bindposeCount;
            
            // initialize native vertices
            using (Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                Mesh.MeshData meshData = meshDataArray[0];
                _vertices = new NativeArray<Vector3>(meshData.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                meshData.GetVertices(_vertices);
            }
            
            // initialize native morph target deltas (blend shape position deltas)
            _morphTargetDeltas = new NativeArray<Vector3>(mesh.vertexCount * mesh.blendShapeCount, allocator, NativeArrayOptions.UninitializedMemory);
            var deltaVertices = new Vector3[mesh.vertexCount];
            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; ++shapeIndex)
            {
                mesh.GetBlendShapeFrameVertices(shapeIndex, 0, deltaVertices, null, null);
                NativeArray<Vector3>.Copy(deltaVertices, 0, _morphTargetDeltas, shapeIndex * deltaVertices.Length, deltaVertices.Length);
            }
            
            // initialize skinning data
            _skinBuffer = new VertexSkinBuffer(mesh.GetBonesPerVertex(), mesh.GetAllBoneWeights(), allocator);
            _bindposes  = new NativeArray<Matrix4x4>(mesh.bindposes, allocator);
        }
        
        public MeshDeformBuilder(NativeArray<Vector3> vertices, NativeArray<Vector3> morphTargetDeltas,
            VertexSkinBuffer skinBuffer, NativeArray<Matrix4x4> bindposes)
        {
            if (morphTargetDeltas.Length % vertices.Length != 0)
            {
                throw new ArgumentException("The length of morph target deltas must be a multiple of the length of vertices");
            }

            if (skinBuffer.VertexCount != vertices.Length)
            {
                throw new ArgumentException("The length of the vertex skin buffer must match the length of vertices");
            }

            VertexCount      = _vertices.Length;
            MorphTargetCount = _morphTargetDeltas.Length / _vertices.Length;
            BindposeCount    = _bindposes.Length;
            
            _vertices          = vertices;
            _morphTargetDeltas = morphTargetDeltas;
            _skinBuffer        = skinBuffer;
            _bindposes         = bindposes;
        }
        
        public MeshDeformBuilder(NativeArray<Vector3> vertices, NativeArray<Vector3> morphTargetDeltas,
            NativeArray<byte> bonesPerVertex, NativeArray<BoneWeight1> boneWeights, NativeArray<Matrix4x4> bindposes, Allocator allocator)
        {
            if (morphTargetDeltas.Length % vertices.Length != 0)
            {
                throw new ArgumentException("The length of morph target deltas must be a multiple of the length of vertices");
            }

            if (bonesPerVertex.Length != vertices.Length)
            {
                throw new ArgumentException("The length of bones per vertex must match the length of vertices");
            }

            VertexCount      = _vertices.Length;
            MorphTargetCount = _morphTargetDeltas.Length / _vertices.Length;
            BindposeCount    = _bindposes.Length;
            
            _vertices          = vertices;
            _morphTargetDeltas = morphTargetDeltas;
            _skinBuffer        = new VertexSkinBuffer(bonesPerVertex, boneWeights, allocator);
            _bindposes         = bindposes;
        }
        
        public NativeArray<Vector3> BuildDeform(MeshDeformationDescriptor descriptor, Allocator resultAllocator)
        {
            AssertDeformationDescriptorIsValid(descriptor);
            var resultVertices = new NativeArray<Vector3>(VertexCount, resultAllocator, NativeArrayOptions.UninitializedMemory);
            BuildDeformationWithoutValidation(descriptor, resultVertices);
            return resultVertices;
        }
        
        public void BuildDeform(MeshDeformationDescriptor descriptor, NativeArray<Vector3> resultVertices)
        {
            if (resultVertices.Length != VertexCount)
            {
                throw new ArgumentException("The length of the result vertices must match the length of the mesh vertices");
            }

            AssertDeformationDescriptorIsValid(descriptor);
            BuildDeformationWithoutValidation(descriptor, resultVertices);
        }
        
        public void Dispose()
        {
            _vertices.Dispose();
            _morphTargetDeltas.Dispose();
            _skinBuffer.Dispose();
            _bindposes.Dispose();
            
            _vertices          = default;
            _morphTargetDeltas = default;
            _skinBuffer        = default;
            _bindposes         = default;
        }
        
        private void BuildDeformationWithoutValidation(MeshDeformationDescriptor descriptor, NativeArray<Vector3> resultVertices)
        {
            resultVertices.CopyFrom(_vertices);
            JobHandle morphTargetsHandle = ScheduleMorphTargetsApplication(descriptor.MortphTargetWeights, resultVertices);
            ApplyTargetBindposes(descriptor.TargetBindposes, resultVertices, morphTargetsHandle);
        }
        
        private JobHandle ScheduleMorphTargetsApplication(NativeArray<float> weights, NativeArray<Vector3> resultVertices)
        {
            if (weights.Length == 0)
            {
                return default;
            }

            JobHandle handle = default;
            for (int morphTargetIndex = 0; morphTargetIndex < weights.Length; ++morphTargetIndex)
            {
                handle = ScheduleMorphTargetApplication(morphTargetIndex, weights[morphTargetIndex], resultVertices, handle);
            }

            return handle;
        }

        private JobHandle ScheduleMorphTargetApplication(int index, float weight, NativeArray<Vector3> resultVertices, JobHandle dependsOn)
        {
            if (weight == 0.0f)
            {
                return dependsOn;
            }

            var job = new ApplyMorphTargetsJob(resultVertices, _morphTargetDeltas, weight, index * VertexCount);
            JobHandle handle = job.Schedule(VertexCount, VertexCount / JobsUtility.MaxJobThreadCount, dependsOn);
            
            return handle;
        }

        private void ApplyTargetBindposes(NativeArray<Matrix4x4> targetBindposes, NativeArray<Vector3> resultVertices, JobHandle dependsOn)
        {
            if (targetBindposes.Length == 0)
            {
                dependsOn.Complete();
                return;
            }
            
            using NativeArray<Matrix4x4> bindposeOffsets = ApplyBindposesJob.GetBindposeOffsets(_bindposes, targetBindposes, Allocator.TempJob);
            var job = new ApplyBindposesJob(resultVertices, _skinBuffer, bindposeOffsets);
            JobHandle handle = job.Schedule(VertexCount, VertexCount / JobsUtility.MaxJobThreadCount, dependsOn);
            
            handle.Complete();
        }

        private void AssertDeformationDescriptorIsValid(MeshDeformationDescriptor descriptor)
        {
            if (descriptor.MortphTargetWeights.Length != 0 && descriptor.MortphTargetWeights.Length != MorphTargetCount)
            {
                throw new ArgumentException("The length of morph target weights must match the number of morph targets");
            }

            if (descriptor.TargetBindposes.Length != 0 && descriptor.TargetBindposes.Length != BindposeCount)
            {
                throw new ArgumentException("The length of bindposes must match the number of bindposes.");
            }
        }
    }
}