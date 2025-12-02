using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IReferenceShape"/> implementation for shapes made of unique points. This is currently our most accurate
    /// approach for transferring point deformations, but it can't handle high resolution meshes. The time complexity
    /// for the class initialization is O(n^3) where n is the number of unique vertices in the reference mesh. Once the
    /// reference data has been initialized, the time complexity for transferring deformations is O(n).
    /// </summary>
    public sealed partial class UniquePointsShape : IReferenceShape
    {
        public string                      Id               { get; }
        public int                         PointCount       => _referencePoints.Length;
        public bool                        AreDeformsLocked => _areDeformsLocked;
        public IReadOnlyCollection<string> DeformIds        => _deformTransforms.Keys;
        
        public int UniquePointCount => _referenceUniquePoints.Length;
        
        /// <summary>
        /// Whether to use the reference correlations cache. Cache is cleared whenever this is set to false.
        /// </summary>
        public bool EnableReferenceCorrelationsCache
        {
            get => _enableReferenceCorrelationsCache;
            set
            {
                if (!value)
                {
                    ClearReferenceCorrelationsCache();
                }

                _enableReferenceCorrelationsCache = value;
            }
        }
        
        private bool _initialized;
        private bool _enableReferenceCorrelationsCache;
        private bool _areDeformsLocked;
        
        private NativeArray<Vector3> _referencePoints;
        private NativeArray<Vector3> _referenceUniquePoints;
        private NativeArray<int>     _referenceUniquePointIndices; // the indices within the reference points that give the unique points
        private NativeMatrix         _referenceSelfCorrelationPInv;
        private NativeMatrix         _deformPointMatrix;
        
        private readonly Dictionary<string, NativeMatrix> _deformTransforms = new();
        private readonly Dictionary<string, NativeMatrix> _referenceCorrelationsCache = new();

        public UniquePointsShape(string id)
        {
            Id = id;
            _initialized = false;
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
                throw new Exception($"[{nameof(UniquePointsShape)}] already initialized");
            }

            _initialized = true;
            
            // get reference points
            _referencePoints = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            meshData.GetVertices(_referencePoints);
            
            // get unique points and indices
            ComputeUniquePoints(_referencePoints, Allocator.Persistent, out _referenceUniquePoints, out _referenceUniquePointIndices);
            
            // compute the reference self correlation matrix pseudo-inverse
            _referenceSelfCorrelationPInv = ComputeReferenceSelfCorrelationPInv(_referenceUniquePoints, Allocator.Persistent);
            
            // initialize the deform point matrix
            _deformPointMatrix = CreateDeformPointMatrix(_referenceUniquePoints.Length, Allocator.Persistent);
        }

        public void Initialize(NativeArray<Vector3> referencePoints)
        {
            if (_initialized)
            {
                throw new Exception($"[{nameof(UniquePointsShape)}] already initialized");
            }

            _initialized = true;
            _referencePoints = referencePoints;
            ComputeUniquePoints(_referencePoints, Allocator.Persistent, out _referenceUniquePoints, out _referenceUniquePointIndices);
            _referenceSelfCorrelationPInv = ComputeReferenceSelfCorrelationPInv(_referenceUniquePoints, Allocator.Persistent);

            _deformPointMatrix = CreateDeformPointMatrix(_referenceUniquePoints.Length, Allocator.Persistent);
        }
        
#region IReferenceShape
        public void GetPoints(NativeArray<Vector3> points)
        {
            if (points.Length != PointCount)
            {
                throw new Exception($"[{nameof(UniquePointsShape)}] points array must have the same length as the point count");
            }

            points.CopyFrom(_referencePoints);
        }

        public void AddDeform(string deformId, NativeArray<Vector3> deformPoints)
        {
            if (AreDeformsLockedWighLog())
            {
                return;
            }

            if (_deformTransforms.Remove(deformId, out NativeMatrix transform))
            {
                transform.Dispose();
            }

            _deformTransforms[deformId] = SolveDeformTransform(deformPoints, Allocator.Persistent);
        }
        
        public void RemoveDeform(string deformId)
        {
            if (AreDeformsLockedWighLog())
            {
                return;
            }

            if (_deformTransforms.Remove(deformId, out NativeMatrix transform))
            {
                transform.Dispose();
            }
        }
        
        public bool ContainsDeform(string deformId)
        {
            return _deformTransforms.ContainsKey(deformId);
        }
        
        public void ClearDeforms()
        {
            if (AreDeformsLockedWighLog())
            {
                return;
            }

            foreach (NativeMatrix transform in _deformTransforms.Values)
            {
                transform.Dispose();
            }

            _deformTransforms.Clear();
        }

        public void LockDeforms()
        {
            _areDeformsLocked = true;
            
            _referenceUniquePointIndices.Dispose();
            _referenceSelfCorrelationPInv.Dispose();
            _deformPointMatrix.Dispose();
            
            _referenceUniquePointIndices  = default;
            _referenceSelfCorrelationPInv = default;
            _deformPointMatrix            = default;
        }

        public void TransferDeform(string deformId, NativeArray<Vector3> targetPoints, string targetId = null)
        {
            NativeMatrix transform            = GetDeformTransform(deformId);
            NativeMatrix referenceCorrelation = GetOrSolveReferenceCorrelationMatrix(targetId, targetPoints, out bool disposeAfterUse);
            TransferDeform(targetPoints, transform, referenceCorrelation, disposeAfterUse);
        }

        public void TransferDeform(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, string targetId = null)
        {
            using NativeMatrix transform      = SolveDeformTransform(deformPoints, Allocator.TempJob);
            NativeMatrix referenceCorrelation = GetOrSolveReferenceCorrelationMatrix(targetId, targetPoints, out bool disposeAfterUse);
            TransferDeform(targetPoints, transform, referenceCorrelation, disposeAfterUse);
        }

        public void TransferDeformAsDeltas(string deformId, NativeArray<Vector3> targetPoints, NativeArray<Vector3> deltas,
            string targetId = null)
        {
            NativeMatrix transform            = GetDeformTransform(deformId);
            NativeMatrix referenceCorrelation = GetOrSolveReferenceCorrelationMatrix(targetId, targetPoints, out bool disposeAfterUse);
            TransferDeformAsDeltas(targetPoints, transform, referenceCorrelation, deltas, disposeAfterUse);
        }

        public void TransferDeformAsDeltas(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints,
            NativeArray<Vector3> deltas, string targetId = null)
        {
            using NativeMatrix transform      = SolveDeformTransform(deformPoints, Allocator.TempJob);
            NativeMatrix referenceCorrelation = GetOrSolveReferenceCorrelationMatrix(targetId, targetPoints, out bool disposeAfterUse);
            TransferDeformAsDeltas(targetPoints, transform, referenceCorrelation, deltas, disposeAfterUse);
        }
        
        public void Dispose()
        {
            _referencePoints.Dispose();
            _referenceUniquePoints.Dispose();
            _referenceUniquePointIndices.Dispose();
            _referenceSelfCorrelationPInv.Dispose();
            _deformPointMatrix.Dispose();
            
            _referencePoints              = default;
            _referenceUniquePoints        = default;
            _referenceUniquePointIndices  = default;
            _referenceSelfCorrelationPInv = default;
            _deformPointMatrix            = default;
            
            ClearReferenceCorrelationsCache();
            
            foreach (NativeMatrix transform in _deformTransforms.Values)
            {
                transform.Dispose();
            }

            _deformTransforms.Clear();
        }
#endregion

        public void TransferDeform(string deformId, NativeArray<Vector3> targetPoints, NativeMatrix referenceCorrelation)
        {
            NativeMatrix transform = GetDeformTransform(deformId);
            referenceCorrelation.MultiplyIntoPoints(transform, targetPoints);
        }
        
        public void TransferDeform(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, NativeMatrix referenceCorrelation)
        {
            using NativeMatrix transform = SolveDeformTransform(deformPoints, Allocator.TempJob);
            referenceCorrelation.MultiplyIntoPoints(transform, targetPoints);
        }
        
        public void TransferDeformAsDeltas(string deformId, NativeArray<Vector3> targetPoints, NativeMatrix referenceCorrelation,
            NativeArray<Vector3> deltas)
        {
            NativeMatrix transform = GetDeformTransform(deformId);
            referenceCorrelation.MultiplyIntoDeltas(transform, targetPoints, deltas);
        }
        
        public void TransferDeformAsDeltas(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints,
            NativeMatrix referenceCorrelation, NativeArray<Vector3> deltas)
        {
            using NativeMatrix transform = SolveDeformTransform(deformPoints, Allocator.TempJob);
            referenceCorrelation.MultiplyIntoDeltas(transform, targetPoints, deltas);
        }

        public NativeMatrix SolveDeformTransform(NativeArray<Vector3> deformPoints, Allocator allocator)
        {
            for (int i = 0; i < _referenceUniquePoints.Length; ++i)
            {
                Vector3 point = deformPoints[_referenceUniquePointIndices[i]];
                _deformPointMatrix.Set(i, 0, point.x);
                _deformPointMatrix.Set(i, 1, point.y);
                _deformPointMatrix.Set(i, 2, point.z);
            }

            return _referenceSelfCorrelationPInv.Multiply(_deformPointMatrix, allocator);
        }
        
        public NativeMatrix SolveReferenceCorrelationMatrix(NativeArray<Vector3> targetPoints, Allocator allocator)
        {
            var job = new CorrelationMatrixComputeJob(_referenceUniquePoints, targetPoints, allocator);
            job.Schedule(targetPoints.Length, targetPoints.Length / JobsUtility.MaxJobThreadCount).Complete();
            return job.CorrelationMatrix;
        }
        
        public void ClearReferenceCorrelationsCache()
        {
            foreach (NativeMatrix matrix in _referenceCorrelationsCache.Values)
            {
                matrix.Dispose();
            }

            _referenceCorrelationsCache.Clear();
        }
        
        private NativeMatrix GetDeformTransform(string deformId)
        {
            if (_deformTransforms.TryGetValue(deformId, out NativeMatrix transform))
            {
                return transform;
            }

            throw new Exception($"[{nameof(UniquePointsShape)}] unknown deform: {deformId}");
        }
        
        private NativeMatrix GetOrSolveReferenceCorrelationMatrix(string targetId, NativeArray<Vector3> targetPoints, out bool disposeAfterUse)
        {
            disposeAfterUse = targetId is null || !_enableReferenceCorrelationsCache;
            if (disposeAfterUse)
            {
                return SolveReferenceCorrelationMatrix(targetPoints, Allocator.TempJob);
            }

            if (_referenceCorrelationsCache.TryGetValue(targetId, out NativeMatrix matrix))
            {
                if (matrix.Rows == targetPoints.Length)
                {
                    return matrix;
                }

                _referenceCorrelationsCache.Remove(targetId);
                matrix.Dispose();
            }
            
            matrix = SolveReferenceCorrelationMatrix(targetPoints, Allocator.Persistent);
            _referenceCorrelationsCache[targetId] = matrix;
            return matrix;
        }
        
        private void TransferDeform(NativeArray<Vector3> targetPoints, NativeMatrix transform, NativeMatrix referenceCorrelation, bool disposeCorrelation)
        {
            try
            {
                referenceCorrelation.MultiplyIntoPoints(transform, targetPoints);
            }
            finally
            {
                if (disposeCorrelation)
                {
                    referenceCorrelation.Dispose();
                }
            }
        }
        
        private void TransferDeformAsDeltas(NativeArray<Vector3> targetPoints, NativeMatrix transform, NativeMatrix referenceCorrelation, NativeArray<Vector3> deltas, bool disposeCorrelation)
        {
            try
            {
                referenceCorrelation.MultiplyIntoDeltas(transform, targetPoints, deltas);
            }
            finally
            {
                if (disposeCorrelation)
                {
                    referenceCorrelation.Dispose();
                }
            }
        }

        private bool AreDeformsLockedWighLog()
        {
            if (_areDeformsLocked)
            {
                Debug.LogError($"[{nameof(UniquePointsShape)}] you are trying to add/remove deforms after locking them. This is not allowed.");
            }

            return _areDeformsLocked;
        }
        
        public static void ComputeUniquePoints(NativeArray<Vector3> referencePoints, Allocator allocator,
            out NativeArray<Vector3> referenceUniquePoints, out NativeArray<int> referenceUniquePointIndices)
        {
            var indices = new NativeArray<int>(referencePoints.Length, allocator, NativeArrayOptions.UninitializedMemory);
            var points  = new HashSet<Vector3>(referencePoints.Length);
            
            int uniquePointCount = 0;
            for (int i = 0; i < referencePoints.Length; ++i)
            {
                if (points.Add(referencePoints[i]))
                {
                    indices[uniquePointCount++] = i;
                }
            }
            
            referenceUniquePoints = new NativeArray<Vector3>(uniquePointCount, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < uniquePointCount; ++i)
            {
                referenceUniquePoints[i] = referencePoints[indices[i]];
            }

            if (referencePoints.Length == uniquePointCount)
            {
                referenceUniquePointIndices = indices;
                return;
            }
            
            referenceUniquePointIndices = new NativeArray<int>(uniquePointCount, allocator, NativeArrayOptions.UninitializedMemory);
            NativeArray<int>.Copy(indices, referenceUniquePointIndices, uniquePointCount);
            indices.Dispose();
        }
        
        public static NativeMatrix ComputeReferenceSelfCorrelationPInv(NativeArray<Vector3> referencePoints, Allocator allocator)
        {
            var job = new ReferenceSelfCorrelationMatrixComputeJob(referencePoints, Allocator.TempJob);
            job.Schedule(referencePoints.Length, referencePoints.Length / JobsUtility.MaxJobThreadCount).Complete();
            
            // get the math net library matrix and dispose the one from the job
            Matrix<double> matrix = job.CorrelationMatrix.ToDoubleMatrix();
            job.CorrelationMatrix.Dispose();
            
            // use the math net library to get the pseudo-inverse and transform back to a native matrix
            return new NativeMatrix(matrix.PseudoInverse(), allocator);
        }

        private static NativeMatrix CreateDeformPointMatrix(int uniquePointCount, Allocator allocator)
        {
            var matrix = new NativeMatrix(uniquePointCount + 4, 3, allocator, NativeArrayOptions.UninitializedMemory);
            
            // fill the extra rows with zeros
            for (int row = uniquePointCount; row < matrix.Rows; ++row)
            {
                matrix.Set(row, 0, 0.0f);
                matrix.Set(row, 1, 0.0f);
                matrix.Set(row, 2, 0.0f);
            }
            
            return matrix;
        }
    }
}