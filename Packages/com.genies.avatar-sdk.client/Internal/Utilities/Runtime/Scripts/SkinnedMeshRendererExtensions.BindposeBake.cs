using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    public static partial class SkinnedMeshRendererExtensions
    {
        /// <summary>
        /// Returns a new <see cref="Mesh"/> with the current renderer pose as the bindpose, relative to the given root
        /// <see cref="Transform"/>. If no root is given then the renderer transform will be used.
        /// <br/><br/>
        /// This synchronous version is always faster than <see cref="BakeBindposeAsync(UnityEngine.SkinnedMeshRenderer,UnityEngine.Transform)"/>
        /// but it may stall the main thread specially if the mesh has blend shapes.
        /// </summary>
        public static Mesh BakeBindpose(this SkinnedMeshRenderer renderer, Transform root = null)
            => BakeBindpose(renderer, root ? root.localToWorldMatrix : renderer.transform.localToWorldMatrix);
        
        /// <summary>
        /// Returns a new <see cref="Mesh"/> with the current renderer pose as the bindpose, relative to the given root
        /// transform.
        /// <br/><br/>
        /// This synchronous version is always faster than <see cref="BakeBindposeAsync(UnityEngine.SkinnedMeshRenderer,UnityEngine.Matrix4x4)"/>
        /// but it may stall the main thread specially if the mesh has blend shapes.
        /// </summary>
        public static Mesh BakeBindpose(this SkinnedMeshRenderer renderer, Matrix4x4 root)
            => BakeBindposeAsync(renderer, root, executeAsync: false).GetAwaiter().GetResult();
        
        /// <summary>
        /// Returns a new <see cref="Mesh"/> with the current renderer pose as the bindpose, relative to the given root
        /// <see cref="Transform"/>. If no root is given then the renderer transform will be used.
        /// <br/><br/>
        /// This asynchronous version is always slower than <see cref="BakeBindpose(UnityEngine.SkinnedMeshRenderer,UnityEngine.Transform)"/>
        /// but it should keep a more consistent framerate during the operation.
        /// </summary>
        public static UniTask<Mesh> BakeBindposeAsync(this SkinnedMeshRenderer renderer, Transform root = null)
            => BakeBindposeAsync(renderer, root ? root.localToWorldMatrix : renderer.transform.localToWorldMatrix);
        
        /// <summary>
        /// Returns a new <see cref="Mesh"/> with the current renderer pose as the bindpose, relative to the given root
        /// transform.
        /// <br/><br/>
        /// This asynchronous version is always slower than <see cref="BakeBindpose(UnityEngine.SkinnedMeshRenderer,UnityEngine.Matrix4x4)"/>
        /// but it should keep a more consistent framerate during the operation.
        /// </summary>
        public static UniTask<Mesh> BakeBindposeAsync(this SkinnedMeshRenderer renderer, Matrix4x4 root)
            => BakeBindposeAsync(renderer, root, executeAsync: true);
        
        // get pose bounds versions
        public static Bounds GetPoseBounds(this SkinnedMeshRenderer renderer, Transform root = null)
            => GetPoseBounds(renderer, root ? root.localToWorldMatrix : renderer.transform.localToWorldMatrix);
        public static Bounds GetPoseBounds(this SkinnedMeshRenderer renderer, Matrix4x4 root)
            => GetPoseBoundsAsync(renderer, root, executeAsync: false).GetAwaiter().GetResult();
        public static UniTask<Bounds> GetPoseBoundsAsync(this SkinnedMeshRenderer renderer, Transform root = null)
            => GetPoseBoundsAsync(renderer, root ? root.localToWorldMatrix : renderer.transform.localToWorldMatrix);
        public static UniTask<Bounds> GetPoseBoundsAsync(this SkinnedMeshRenderer renderer, Matrix4x4 root)
            => GetPoseBoundsAsync(renderer, root, executeAsync: true);
        
        private static async UniTask<Mesh> BakeBindposeAsync(this SkinnedMeshRenderer renderer, Matrix4x4 root, bool executeAsync)
        {
            // instantiate a copy of the mesh in which we will apply the results
            Mesh mesh = renderer.sharedMesh;
            Mesh finalMesh = Object.Instantiate(mesh);
            finalMesh.name = $"{mesh.name} (bake)";
            
            // build the data necessary for the job
            Allocator allocator = executeAsync ? Allocator.Persistent : Allocator.TempJob; // temp job allocations are not allowed to persist for more than 4 frames
            using var skinningData = new SkinningData(mesh, root, renderer.bones, allocator);
            
            // initialize the result data native arrays
            using var vertices = new NativeArray<Vector3>(mesh.vertexCount, allocator);
            using var normals  = new NativeArray<Vector3>(mesh.vertexCount, allocator);
            using var tangents = new NativeArray<Vector4>(mesh.vertexCount, allocator);
            
            // initialize the skinning job
            var skinningJob = new SkinningJob
            {
                Parameters = skinningData.JobParameters,
                Vertices   = vertices,
                Normals    = normals,
                Tangents   = tangents,
            };
            
            // calculate the batch size for the operation, dividing the vertex count by the available worker threads
            int batchSize = mesh.vertexCount / JobsUtility.JobWorkerCount;
            
            // execute job to calculate the skinning into the mesh vertex data
            JobHandle handle = skinningJob.Schedule(mesh.vertexCount, batchSize);
            if (executeAsync)
            {
                await UniTask.WaitUntil(() => handle.IsCompleted);
            }

            handle.Complete();
            
            // calculate the skinning into the mesh blend shapes data
            await BakeBlendShapesAsync(mesh, finalMesh, skinningData.JobParameters, executeAsync);
            
            // apply results to the final mesh and return it
            finalMesh.SetVertices(vertices);
            finalMesh.SetNormals(normals);
            finalMesh.SetTangents(tangents);
            finalMesh.bindposes = skinningData.Bindposes;
            finalMesh.RecalculateBounds();
            
            return finalMesh;
        }
        
        private static async UniTask<Bounds> GetPoseBoundsAsync(this SkinnedMeshRenderer renderer, Matrix4x4 root, bool executeAsync)
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh.vertexCount == 0)
            {
                return default;
            }

            // build the data necessary for the job
            Allocator allocator = executeAsync ? Allocator.Persistent : Allocator.TempJob; // temp job allocations are not allowed to persist for more than 4 frames
            using var skinningData = new SkinningData(mesh, root, renderer.bones, allocator, onlyVertices: true);
            
            // initialize the result data native arrays
            using var vertices = new NativeArray<Vector3>(mesh.vertexCount, allocator);
            
            // initialize the skinning job
            var skinningJob = new VertexSkinningJob
            {
                Parameters = skinningData.JobParameters,
                Vertices   = vertices,
            };
            
            // calculate the batch size for the operation, dividing the vertex count by the available worker threads
            int batchSize = mesh.vertexCount / JobsUtility.JobWorkerCount;
            
            // execute job to calculate the skinning into the mesh vertex data
            JobHandle handle = skinningJob.Schedule(mesh.vertexCount, batchSize);
            if (executeAsync)
            {
                await UniTask.WaitUntil(() => handle.IsCompleted);
            }

            handle.Complete();
            
            // calculate the bounds
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];
            
            for (int i = 1; i < vertices.Length; ++i)
            {
                Vector3 vertex = vertices[i];
                if (vertex.x < min.x)
                {
                    min.x = vertex.x;
                }

                if (vertex.y < min.y)
                {
                    min.y = vertex.y;
                }

                if (vertex.z < min.z)
                {
                    min.z = vertex.z;
                }

                if (vertex.x > max.x)
                {
                    max.x = vertex.x;
                }

                if (vertex.y > max.y)
                {
                    max.y = vertex.y;
                }

                if (vertex.z > max.z)
                {
                    max.z = vertex.z;
                }
            }
            
            Vector3 size = max - min;
            Vector3 center = min + size * 0.5f;
            
            return new Bounds(center, size);
        }

        /**
         * We don't use the jobs system here because the Mesh API does not support native containers as it does for
         * vertex, normals and tangents.
         */
        private static async UniTask BakeBlendShapesAsync(Mesh sourceMesh, Mesh finalMesh, SkinningJobParameters parameters, bool executeAsync)
        {
            finalMesh.ClearBlendShapes();
            var shapePool = new BlendShapePool(sourceMesh.vertexCount);
            
            for (int shapeIndex = 0; shapeIndex < sourceMesh.blendShapeCount; ++shapeIndex)
            {
                // get a shape from the pool and fill it with the current shape data from source mesh
                int frameCount = sourceMesh.GetBlendShapeFrameCount(shapeIndex);
                BlendShape shape = shapePool.Get(frameCount);
                shape = sourceMesh.GetBlendShape(shapeIndex, shape);

                // iterate over each frame and bake the new bindpose into the vertex data
                for (int frameIndex = 0; frameIndex < shape.FrameCount; ++frameIndex)
                {
                    if (executeAsync)
                    {
                        await OperationQueue.EnqueueAsync();
                    }

                    BakeBlendShapeFrame(in shape.Frames[frameIndex], in parameters);
                }
                
                // add the baked shape to the final mesh and release the shape object to the pool
                finalMesh.AddBlendShape(shape);
                shapePool.Release(shape);
            }
        }

        private static void BakeBlendShapeFrame(in BlendShapeFrame frame, in SkinningJobParameters parameters)
        {
            for (int vertexIndex = 0; vertexIndex < frame.VertexCount; ++vertexIndex)
            {
                int weightCount = parameters.Weights.GetWeightCount(vertexIndex);

                // skip if this vertex is not weighted to any bones
                if (weightCount <= 0)
                {
                    continue;
                }

                Vector3 deltaVertex  = Vector3.zero;
                Vector3 deltaNormal  = Vector3.zero;
                Vector3 deltaTangent = Vector3.zero;

                for (int weightIndex = 0; weightIndex < weightCount; ++weightIndex)
                {
                    // fetch bone weight and offset matrix
                    BoneWeight1 boneWeight = parameters.Weights.GetWeight(vertexIndex, weightIndex);
                    Matrix4x4   offset     = parameters.BoneOffsets[boneWeight.boneIndex];
                    
                    // apply the weighted offset to vertices, normals and tangents
                    deltaVertex  += boneWeight.weight * offset.MultiplyVector(frame.DeltaVertices[vertexIndex]);
                    deltaNormal  += boneWeight.weight * offset.MultiplyVector(frame.DeltaNormals[vertexIndex]);
                    deltaTangent += boneWeight.weight * offset.MultiplyVector(frame.DeltaTangents[vertexIndex]);
                }
                
                // save results
                frame.DeltaVertices[vertexIndex] = deltaVertex;
                frame.DeltaNormals [vertexIndex] = deltaNormal.normalized;
                frame.DeltaTangents[vertexIndex] = deltaTangent.normalized;
            }
        }
        
        // computes the final skinning vertex data for the given vertex index
        [BurstCompile]
        private struct SkinningJob : IJobParallelFor
        {
            [ReadOnly] public SkinningJobParameters Parameters;
            
            [WriteOnly] public NativeArray<Vector3> Vertices;
            [WriteOnly] public NativeArray<Vector3> Normals;
            [WriteOnly] public NativeArray<Vector4> Tangents;

            public void Execute(int vertexIndex)
            {
                int weightCount = Parameters.Weights.GetWeightCount(vertexIndex);
                
                // if no weights then just set same as source
                if (weightCount <= 0)
                {
                    Vertices[vertexIndex] = Parameters.Vertices[vertexIndex];
                    Normals[vertexIndex]  = Parameters.Normals[vertexIndex];
                    Tangents[vertexIndex] = Parameters.Tangents[vertexIndex];
                    return;
                }
                
                Vector3 vertex         = Vector3.zero;
                Vector3 normal         = Vector3.zero;
                Vector3 currentTangent = Parameters.Tangents[vertexIndex];
                Vector3 tangent        = Vector3.zero;

                for (int weightIndex = 0; weightIndex < weightCount; ++weightIndex)
                {
                    // fetch bone weight and offset matrix
                    BoneWeight1 boneWeight = Parameters.Weights.GetWeight(vertexIndex, weightIndex);
                    Matrix4x4   offset     = Parameters.BoneOffsets[boneWeight.boneIndex];
                    
                    // apply the weighted offset to vertices, normals and tangents
                    vertex  += boneWeight.weight * offset.MultiplyPoint3x4(Parameters.Vertices[vertexIndex]);
                    normal  += boneWeight.weight * offset.MultiplyVector(Parameters.Normals[vertexIndex]);
                    tangent += boneWeight.weight * offset.MultiplyVector(currentTangent);
                }
                
                // save results
                tangent.Normalize();
                Vertices[vertexIndex] = vertex;
                Normals[vertexIndex]  = normal.normalized;
                Tangents[vertexIndex] = new Vector4(tangent.x, tangent.y, tangent.z, Parameters.Tangents[vertexIndex].w);
            }
        }
        
        // computes the final skinning vertex data for the given vertex index (only for vertices)
        [BurstCompile]
        private struct VertexSkinningJob : IJobParallelFor
        {
            [ReadOnly] public SkinningJobParameters Parameters;
            
            [WriteOnly] public NativeArray<Vector3> Vertices;

            public void Execute(int vertexIndex)
            {
                int weightCount = Parameters.Weights.GetWeightCount(vertexIndex);
                
                // if no weights then just set same as source
                if (weightCount <= 0)
                {
                    Vertices[vertexIndex] = Parameters.Vertices[vertexIndex];
                    return;
                }
                
                Vector3 vertex         = Vector3.zero;

                for (int weightIndex = 0; weightIndex < weightCount; ++weightIndex)
                {
                    // fetch bone weight and offset matrix
                    BoneWeight1 boneWeight = Parameters.Weights.GetWeight(vertexIndex, weightIndex);
                    Matrix4x4   offset     = Parameters.BoneOffsets[boneWeight.boneIndex];
                    
                    // apply the weighted offset to vertices
                    vertex  += boneWeight.weight * offset.MultiplyPoint3x4(Parameters.Vertices[vertexIndex]);
                }
                
                // save results
                Vertices[vertexIndex] = vertex;
            }
        }

        private struct SkinningData : IDisposable
        {
            public SkinningJobParameters JobParameters;
            public Matrix4x4[]           Bindposes;
            
            private Mesh.MeshDataArray _meshDataArray;

            public SkinningData(Mesh mesh, Matrix4x4 root, Transform[] bones, Allocator allocator, bool onlyVertices = false)
            {
                // initialize job parameters
                JobParameters = new SkinningJobParameters
                {
                    Vertices    = new NativeArray<Vector3>(mesh.vertexCount, allocator),
                    Normals     = new NativeArray<Vector3>(onlyVertices ? 0 : mesh.vertexCount, allocator),
                    Tangents    = new NativeArray<Vector4>(onlyVertices ? 0 : mesh.vertexCount, allocator),
                    BoneOffsets = new NativeArray<Matrix4x4>(bones.Length, allocator),
                    Weights     = new NativeBoneWeights(mesh, allocator),
                };
                
                // populate job parameters vertex data from mesh
                _meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
                Mesh.MeshData meshData = _meshDataArray[0];
                meshData.GetVertices(JobParameters.Vertices);

                if (!onlyVertices)
                {
                    meshData.GetNormals(JobParameters.Normals);
                    meshData.GetTangents(JobParameters.Tangents);
                }

                // calculate the offset matrices for all bindposes (the TRS from the bindpose to the current bone transform)
                Bindposes = mesh.bindposes;
                Matrix4x4 rootInverse = root.inverse;
                for (int i = 0; i < bones.Length; i++)
                {
                    // get the new bindpose matrix, calculate the offset and update the bindposes array
                    Matrix4x4 bindpose = rootInverse * bones[i].localToWorldMatrix;
                    JobParameters.BoneOffsets[i] = bindpose * Bindposes[i];
                    Bindposes[i] = bindpose.inverse;
                }
            }

            public void Dispose()
            {
                _meshDataArray.Dispose();
                JobParameters.Vertices.Dispose();
                JobParameters.Normals.Dispose();
                JobParameters.Tangents.Dispose();
                JobParameters.BoneOffsets.Dispose();
                JobParameters.Weights.Dispose();
            }
        }

        private struct SkinningJobParameters
        {
            [ReadOnly] public NativeArray<Vector3>   Vertices;
            [ReadOnly] public NativeArray<Vector3>   Normals;
            [ReadOnly] public NativeArray<Vector4>   Tangents;
            [ReadOnly] public NativeArray<Matrix4x4> BoneOffsets;
            [ReadOnly] public NativeBoneWeights      Weights;
        }

        private struct NativeBoneWeights : IDisposable
        {
            [ReadOnly] private NativeArray<BoneWeight1> _weights;
            [ReadOnly] private NativeArray<int>         _weightIndexPerVertex;
            [ReadOnly] private NativeArray<byte>        _weightCountPerVertex;

            public NativeBoneWeights(Mesh mesh, Allocator allocator)
            {
                _weightCountPerVertex = mesh.GetBonesPerVertex();
                _weights = mesh.GetAllBoneWeights();
                
                // initialize the weight indices per vertex
                int weightIndex = 0;
                int vertexCount = mesh.vertexCount;
                _weightIndexPerVertex = new NativeArray<int>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                
                for (int vertexIndex = 0; vertexIndex < vertexCount; ++vertexIndex)
                {
                    _weightIndexPerVertex[vertexIndex] = weightIndex;
                    weightIndex += _weightCountPerVertex[vertexIndex];
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BoneWeight1 GetWeight(int vertexIndex, int weightIndex)
                => _weights[_weightIndexPerVertex[vertexIndex] + weightIndex];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetWeightCount(int vertexIndex)
                => _weightCountPerVertex[vertexIndex];

            public void Dispose()
            {
                _weights.Dispose();
                _weightIndexPerVertex.Dispose();
                _weightCountPerVertex.Dispose();
            }
        }
    }
}
