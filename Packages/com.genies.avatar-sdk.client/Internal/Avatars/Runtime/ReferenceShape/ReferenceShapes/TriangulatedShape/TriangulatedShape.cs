using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IReferenceShape"/> implementation for triangle-based shapes. This is currently our fastest and most
    /// efficient approach for transferring point deformations and can handle high resolution meshes with ease.
    /// </summary>
    public sealed partial class TriangulatedShape : IReferenceShape
    {
        public string                      Id               { get; }
        public int                         PointCount       => _referenceVertices.Length;
        public bool                        AreDeformsLocked => _areDeformsLocked;
        public IReadOnlyCollection<string> DeformIds        => _deformTriangleDeltas.Keys;

        public int IndexCount    => _referenceIndices.Length;
        public int TriangleCount => _referenceTriangles.Count;

        public ITswSolver WeightsSolver
        {
            get => _weightsSolver;
            set
            {
                if (value is null)
                {
                    throw new NullReferenceException($"[{nameof(TriangulatedShape)}] weights solver cannot be null");
                }

                if (value == _weightsSolver)
                {
                    return;
                }

                ClearWeightsCache();
                _weightsSolver = value;
            }
        }
        
        /// <summary>
        /// Whether to use the weights cache. Cache is cleared whenever this is set to false.
        /// </summary>
        public bool EnableWeightsCache
        {
            get => _enableWeightsCache;
            set
            {
                if (!value)
                {
                    ClearWeightsCache();
                }

                _enableWeightsCache = value;
            }
        }
        
        public ITswSolver _weightsSolver;
        
        private bool _initialized;
        private bool _enableWeightsCache;
        private bool _areDeformsLocked;

        // reference mesh data
        private NativeArray<Vector3>  _referenceVertices;
        private NativeArray<int>      _referenceIndices;
        private MeshTriangles         _referenceTriangles;
        
        private readonly Dictionary<string, NativeArray<Matrix4x4>>   _deformTriangleDeltas = new();
        private readonly Dictionary<string, TriangulatedShapeWeights> _weightsCache = new();

        public TriangulatedShape(string id, ITswSolver weightsSolver)
        {
            Id            = id;
            WeightsSolver = weightsSolver;
            _initialized  = false;
        }

        public void Initialize(Mesh referenceMesh)
        {
            // get readonly mesh data to obtain native buffers (this is faster and more efficient than using the mesh directly)
            using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(referenceMesh);
            Initialize(meshDataArray[0]);
        }
        
        public void Initialize(Mesh.MeshData meshData)
        {
            if (_initialized)
            {
                throw new Exception($"[{nameof(TriangulatedShape)}] already initialized");
            }

            _initialized = true;
            
            // get vertices
            _referenceVertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            meshData.GetVertices(_referenceVertices);
            
            // get indices (all submeshes)
            int indexCount = 0;
            for (int i = 0; i < meshData.subMeshCount; ++i)
            {
                indexCount += meshData.GetSubMesh(i).indexCount;
            }

            _referenceIndices  = new NativeArray<int>(indexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < meshData.subMeshCount; ++i)
            {
                meshData.GetIndices(_referenceIndices, i, applyBaseVertex: true);
            }

            // initialize reference triangles
            _referenceTriangles = new MeshTriangles(_referenceVertices, _referenceIndices);
        }

        public void Initialize(NativeArray<Vector3> referenceVertices, NativeArray<int> referenceIndices)
        {
            if (_initialized)
            {
                throw new Exception($"[{nameof(TriangulatedShape)}] already initialized");
            }

            _initialized       = true;
            _referenceVertices = referenceVertices;
            _referenceIndices  = referenceIndices;
            
            // initialize reference triangles
            _referenceTriangles = new MeshTriangles(_referenceVertices, _referenceIndices);
        }
        
#region IReferenceShape
        public void GetPoints(NativeArray<Vector3> points)
        {
            if (points.Length != PointCount)
            {
                throw new Exception($"[{nameof(TriangulatedShape)}] points array must have the same length as the point count");
            }

            points.CopyFrom(_referenceVertices);
        }

        public void AddDeform(string deformId, NativeArray<Vector3> deformPoints)
        {
            if (_deformTriangleDeltas.Remove(deformId, out NativeArray<Matrix4x4> triangleDeltas))
            {
                triangleDeltas.Dispose();
            }

            _deformTriangleDeltas[deformId] = SolveDeformTriangleDeltas(deformPoints, Allocator.Persistent);
        }
        
        public void RemoveDeform(string deformId)
        {
            if (_deformTriangleDeltas.Remove(deformId, out NativeArray<Matrix4x4> triangleDeltas))
            {
                triangleDeltas.Dispose();
            }
        }
        
        public bool ContainsDeform(string deformId)
        {
            return _deformTriangleDeltas.ContainsKey(deformId);
        }
        
        public void ClearDeforms()
        {
            foreach (NativeArray<Matrix4x4> triangleDeltas in _deformTriangleDeltas.Values)
            {
                triangleDeltas.Dispose();
            }

            _deformTriangleDeltas.Clear();
        }

        public void LockDeforms()
        {
            // no-op, since we would only dispose the indices but want them to be available for any user if they want
            // to visualize the shape. Also indices doesn't take much memory.
        }

        public void TransferDeform(string deformId, NativeArray<Vector3> targetPoints, string targetId = null)
        {
            NativeArray<Matrix4x4>   triangleDeltas = GetDeformTriangleDeltas(deformId);
            TriangulatedShapeWeights weights        = GetOrSolveWeights(targetId, targetPoints, out bool disposeAfterUse);
            
            TransferDeform(targetPoints, weights, triangleDeltas, disposeAfterUse);
        }
        
        public void TransferDeform(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, string targetId = null)
        {
            using NativeArray<Matrix4x4> triangleDeltas = SolveDeformTriangleDeltas(deformPoints, Allocator.TempJob);
            TriangulatedShapeWeights     weights        = GetOrSolveWeights(targetId, targetPoints, out bool disposeAfterUse);

            TransferDeform(targetPoints, weights, triangleDeltas, disposeAfterUse);
        }

        public void TransferDeformAsDeltas(string deformId, NativeArray<Vector3> targetPoints, NativeArray<Vector3> deltas, string targetId = null)
        {
            NativeArray<Matrix4x4>   triangleDeltas = GetDeformTriangleDeltas(deformId);
            TriangulatedShapeWeights weights        = GetOrSolveWeights(targetId, targetPoints, out bool disposeAfterUse);
            
            TransferDeformAsDeltas(targetPoints, weights, triangleDeltas, deltas, disposeAfterUse);
        }

        public void TransferDeformAsDeltas(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints,
            NativeArray<Vector3> deltas, string targetId = null)
        {
            using NativeArray<Matrix4x4> triangleDeltas = SolveDeformTriangleDeltas(deformPoints, Allocator.TempJob);
            TriangulatedShapeWeights     weights        = GetOrSolveWeights(targetId, targetPoints, out bool disposeAfterUse);

            TransferDeformAsDeltas(targetPoints, weights, triangleDeltas, deltas, disposeAfterUse);
        }

        public void Dispose()
        {
            _referenceVertices.Dispose();
            _referenceIndices.Dispose();
            
            _referenceVertices  = default;
            _referenceIndices   = default;
            _referenceTriangles = default;
            
            ClearWeightsCache();
            
            foreach (NativeArray<Matrix4x4> triangleDeltas in _deformTriangleDeltas.Values)
            {
                triangleDeltas.Dispose();
            }

            _deformTriangleDeltas.Clear();
        }
#endregion

        public void GetIndices(NativeArray<int> indices)
        {
            if (indices.Length != IndexCount)
            {
                throw new Exception($"[{nameof(TriangulatedShape)}] indices array must have the same length as the index count");
            }

            indices.CopyFrom(_referenceIndices);
        }

        public void TransferDeform(string deformId, NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights)
        {
            NativeArray<Matrix4x4> triangleDeltas = GetDeformTriangleDeltas(deformId);
            TransferDeform(targetPoints, weights, triangleDeltas);
        }
        
        public void TransferDeform(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights)
        {
            using NativeArray<Matrix4x4> triangleDeltas = SolveDeformTriangleDeltas(deformPoints, Allocator.TempJob);
            TransferDeform(targetPoints, weights, triangleDeltas);
        }
        
        public void TransferDeformAsDeltas(string deformId, NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights,
            NativeArray<Vector3> deltas)
        {
            NativeArray<Matrix4x4> triangleDeltas = GetDeformTriangleDeltas(deformId);
            TransferDeformAsDeltas(targetPoints, weights, triangleDeltas, deltas);
        }
        
        public void TransferDeformAsDeltas(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints,
            TriangulatedShapeWeights weights, NativeArray<Vector3> deltas)
        {
            using NativeArray<Matrix4x4> triangleDeltas = SolveDeformTriangleDeltas(deformPoints, Allocator.TempJob);
            TransferDeformAsDeltas(targetPoints, weights, triangleDeltas, deltas);
        }
        
        public NativeArray<Matrix4x4> SolveDeformTriangleDeltas(NativeArray<Vector3> deformPoints, Allocator allocator)
        {
            var job = new SolveDeformTriangleDeltasJob(_referenceVertices, deformPoints, _referenceIndices, allocator);
            job.Schedule(TriangleCount, TriangleCount / JobsUtility.MaxJobThreadCount).Complete();
            return job.TriangleDeltas;
        }
        
        public TriangulatedShapeWeights SolveWeights(NativeArray<Vector3> targetPoints, Allocator allocator)
        {
            return WeightsSolver.SolveWeights(_referenceTriangles, targetPoints, allocator);
        }

        public void ClearWeightsCache()
        {
            foreach (TriangulatedShapeWeights weights in _weightsCache.Values)
            {
                weights.Dispose();
            }

            _weightsCache.Clear();
        }

        private NativeArray<Matrix4x4> GetDeformTriangleDeltas(string deformId)
        {
            if (_deformTriangleDeltas.TryGetValue(deformId, out NativeArray<Matrix4x4> triangleDeltas))
            {
                return triangleDeltas;
            }

            throw new Exception($"[{nameof(TriangulatedShape)}] unknown deform: {deformId}");
        }
        
        private TriangulatedShapeWeights GetOrSolveWeights(string targetId, NativeArray<Vector3> targetPoints, out bool disposeAfterUse)
        {
            disposeAfterUse = targetId is null || !_enableWeightsCache;
            if (disposeAfterUse)
            {
                return SolveWeights(targetPoints, Allocator.TempJob);
            }

            if (_weightsCache.TryGetValue(targetId, out TriangulatedShapeWeights weights))
            {
                if (weights.PointCount == targetPoints.Length)
                {
                    return weights;
                }

                _weightsCache.Remove(targetId);
                weights.Dispose();
            }
            
            weights = SolveWeights(targetPoints, Allocator.Persistent);
            _weightsCache[targetId] = weights;
            return weights;
        }

        private void TransferDeform(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights,
            NativeArray<Matrix4x4> triangleDeltas, bool disposeWeights)
        {
            try
            {
                TransferDeform(targetPoints, weights, triangleDeltas);
            }
            finally
            {
                if (disposeWeights)
                {
                    weights.Dispose();
                }
            }
        }
        
        private void TransferDeformAsDeltas(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights,
            NativeArray<Matrix4x4> triangleDeltas, NativeArray<Vector3> deltas, bool disposeWeights)
        {
            try
            {
                TransferDeformAsDeltas(targetPoints, weights, triangleDeltas, deltas);
            }
            finally
            {
                if (disposeWeights)
                {
                    weights.Dispose();
                }
            }
        }
        
        public static void TransferDeform(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights, NativeArray<Matrix4x4> triangleDeltas)
        {
            var job = new TransferDeformJob(targetPoints, weights, triangleDeltas);
            job.Schedule(targetPoints.Length, targetPoints.Length / JobsUtility.MaxJobThreadCount).Complete();
        }
        
        public static NativeArray<Vector3> TransferDeformAsDeltas(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights,
            NativeArray<Matrix4x4> triangleDeltas, Allocator deltasAllocator)
        {
            var deltas = new NativeArray<Vector3>(targetPoints.Length, deltasAllocator, NativeArrayOptions.UninitializedMemory);
            TransferDeformAsDeltas(targetPoints, weights, triangleDeltas, deltas);
            return deltas;
        }
        
        public static void TransferDeformAsDeltas(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights,
            NativeArray<Matrix4x4> triangleDeltas, NativeArray<Vector3> deltas)
        {
            var job = new TransferDeformAsDeltasJob(targetPoints, weights, triangleDeltas, deltas);
            job.Schedule(targetPoints.Length, targetPoints.Length / JobsUtility.MaxJobThreadCount).Complete();
        }
    }
}