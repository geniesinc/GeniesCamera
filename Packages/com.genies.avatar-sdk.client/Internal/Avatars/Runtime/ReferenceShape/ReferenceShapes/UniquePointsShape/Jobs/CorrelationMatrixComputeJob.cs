using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Computes the correlation matrix for the reference points and the target points. Whichs encodes the distances
    /// between each target point and each reference point, plus the target points themselves. This job is used by
    /// <see cref="UniquePointsShape"/> to transfer deforms.
    /// </summary>
    [BurstCompile]
    public struct CorrelationMatrixComputeJob : IJobParallelFor
    {
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeMatrix CorrelationMatrix;
        
        [ReadOnly] private readonly NativeArray<Vector3> _referencePoints;
        [ReadOnly] private readonly NativeArray<Vector3> _targetPoints;
        
        [ReadOnly] private readonly int _biasTermColIndex;
        [ReadOnly] private readonly int _xColIndex;
        [ReadOnly] private readonly int _yColIndex;
        [ReadOnly] private readonly int _zColIndex;

        public CorrelationMatrixComputeJob(NativeArray<Vector3> referencePoints, NativeArray<Vector3> targetPoints, Allocator targetAllocator)
        {
            CorrelationMatrix = new NativeMatrix(targetPoints.Length, referencePoints.Length + 4, targetAllocator, NativeArrayOptions.UninitializedMemory);
            
            _referencePoints = referencePoints;
            _targetPoints    = targetPoints;
            
            _biasTermColIndex = referencePoints.Length;
            _xColIndex        = referencePoints.Length + 1;
            _yColIndex        = referencePoints.Length + 2;
            _zColIndex        = referencePoints.Length + 3;
        }

        public void Execute(int index)
        {
            Vector3 targetPoint = _targetPoints[index];
            int rowIndex = index * CorrelationMatrix.Cols;
            
            // set bias term and target point
            CorrelationMatrix.Data[rowIndex + _biasTermColIndex] = 1.0f;
            CorrelationMatrix.Data[rowIndex + _xColIndex       ] = targetPoint.x;
            CorrelationMatrix.Data[rowIndex + _yColIndex       ] = targetPoint.y;
            CorrelationMatrix.Data[rowIndex + _zColIndex       ] = targetPoint.z;
            
            // compute the distances with each reference point
            for (int i = 0; i < _referencePoints.Length; ++i)
            {
                CorrelationMatrix.Data[rowIndex + i] = Vector3.Distance(_referencePoints[i], targetPoint);
            }
        }
    }
}