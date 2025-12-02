using System;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Computes the area of the mesh asset in world units (meters) divided by the area of the mesh in texture units (UVs).
    /// This area values can be used to compute the required texture resolutions so every asset looks at the same quality
    /// independently of their size. Uses the job system to make the calculations outside of the main thread.
    /// </summary>
    public sealed class MeshAssetSmpsuJob
    {
        // asset
        private readonly MeshAsset _asset;
        
        // state
        private SmpsuJob  _job;
        private JobHandle _handle;
        private bool      _completed;
        private float     _result;
        
        public MeshAssetSmpsuJob(MeshAsset asset)
        {
            _asset = asset;
            Schedule().Forget();
        }

        public float GetSquareMetersPerSquareUvs()
        {
            if (_completed)
            {
                return _result;
            }

            Complete();
            return _result;
        }

        private async UniTaskVoid Schedule()
        {
            _job = new SmpsuJob
            {
                Vertices = new NativeArray<Vector3>(_asset.Vertices, Allocator.TempJob),
                Uvs      = new NativeArray<Vector2>(_asset.Uvs, Allocator.TempJob),
                Indices  = new NativeArray<int>(_asset.Indices, Allocator.TempJob),
                Result   = new NativeArray<float>(1, Allocator.TempJob)
            };
            
            _handle = _job.Schedule();
            
            await UniTask.WaitUntil(() => _handle.IsCompleted);
            
            Complete();
        }

        public void Complete()
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            _handle.Complete(); // even if _handle.IsCompleted is true, this needs to be called at least once before reading results from native array
            _result = _job.Result[0];
            _job.Dispose();
        }

        [BurstCompile]
        private struct SmpsuJob : IJob, IDisposable
        {
            // mesh data
            [ReadOnly] public NativeArray<Vector3> Vertices;
            [ReadOnly] public NativeArray<Vector2> Uvs;
            [ReadOnly] public NativeArray<int>     Indices;
            
            // result
            [WriteOnly] public NativeArray<float> Result;
            
            public void Execute()
            {
                float squareMeters = 0.0f;
                float squareUVs = 0.0f;

                int index0, index1, index2;
                Vector2 uv0, uv1, uv2;
                Vector3 vertex0, vertex1, vertex2;
                float crossX, crossY, crossZ;
                
                // let's get the total sum of squareMeters per squareUVs of all triangles
                float squareMetersPerSquareUvs = 0.0f;

                for (int i = 0; i < Indices.Length; i += 3)
                {
                    index0 = Indices[i + 0];
                    index1 = Indices[i + 1];
                    index2 = Indices[i + 2];
                    uv0 = Uvs[index0];
                    uv1 = Uvs[index1];
                    uv2 = Uvs[index2];

                    // calculate the first part of the triangle UV area by reusing the crossX variable (we are using the Heron's formula)
                    // values resulting in 0 are triangles that are not mapped to any UVs. Skip them
                    crossX = uv0.x * (uv1.y - uv2.y) + uv1.x * (uv2.y - uv0.y) + uv2.x * (uv0.y - uv1.y);
                    if (crossX == 0.0f)
                    {
                        continue;
                    }

                    // finish the triangle UV area calculation
                    squareUVs = crossX < 0 ? -0.5f * crossX : 0.5f * crossX;

                    // calculate the triangle world area
                    vertex0 = Vertices[index0];
                    vertex1 = Vertices[index1];
                    vertex2 = Vertices[index2];

                    // fast computation of Vector3.Cross(vertex1 - vertex0, vertex2 - vertex0)
                    crossX = vertex1.x * vertex0.y - vertex2.x * vertex0.y - vertex0.x * vertex1.y +
                        vertex2.x * vertex1.y + vertex0.x * vertex2.y - vertex1.x * vertex2.y;
                    crossY = vertex1.x * vertex0.z - vertex2.x * vertex0.z - vertex0.x * vertex1.z +
                        vertex2.x * vertex1.z + vertex0.x * vertex2.z - vertex1.x * vertex2.z;
                    crossZ = vertex1.y * vertex0.z - vertex2.y * vertex0.z - vertex0.y * vertex1.z +
                        vertex2.y * vertex1.z + vertex0.y * vertex2.z - vertex1.y * vertex2.z;

                    // half of the magnitude of the cross product is the triangle area
                    squareMeters = 0.5f * Mathf.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);
                    
                    // add this triangle's smpsu to the total
                    squareMetersPerSquareUvs += squareMeters / squareUVs;
                }
                
                // get the smpsu per triangle average
                float triangleCount = Indices.Length / 3.0f;
                Result[0] = squareMetersPerSquareUvs / triangleCount;
            }

            public void Dispose()
            {
                Vertices.Dispose();
                Uvs.Dispose();
                Indices.Dispose();
                Result.Dispose();
            }
        }
    }
}