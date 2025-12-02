using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Given a set of target points, triangulated shape weights and a set of triangle deltas, this job will transfer
    /// the deformation to the target points array.
    /// </summary>
    [BurstCompile]
    public struct TransferDeformJob : IJobParallelFor
    {
        private NativeArray<Vector3> _targetPoints;
        
        [ReadOnly] private readonly TriangulatedShapeWeights _weights;
        [ReadOnly] private readonly NativeArray<Matrix4x4>   _triangleDeltas;

        public TransferDeformJob(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights, NativeArray<Matrix4x4> triangleDeltas)
        {
            if (targetPoints.Length != weights.PointCount)
            {
                throw new Exception("The target points array must have the same length as the target weights point count");
            }

            _targetPoints = targetPoints;
            
            _weights        = weights;
            _triangleDeltas = triangleDeltas;
        }

        public void Execute(int index)
        {
            _targetPoints[index] = _weights.ComputeDeformedPoint(_targetPoints[index], index, _triangleDeltas);
        }
    }
}