using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Given a set of target points, triangulated shape weights and a set of triangle deltas, this job will output the
    /// deform transfer as target point deltas.
    /// </summary>
    [BurstCompile]
    public struct TransferDeformAsDeltasJob : IJobParallelFor
    {
        public NativeArray<Vector3> Deltas;

        [ReadOnly] private readonly NativeArray<Vector3>     _targetPoints;
        [ReadOnly] private readonly TriangulatedShapeWeights _weights;
        [ReadOnly] private readonly NativeArray<Matrix4x4>   _triangleDeltas;

        public TransferDeformAsDeltasJob(NativeArray<Vector3> targetPoints, TriangulatedShapeWeights weights,
            NativeArray<Matrix4x4> triangleDeltas, NativeArray<Vector3> deltas)
        {
            if (targetPoints.Length != weights.PointCount)
            {
                throw new Exception("The target points array must have the same length as the weights point count");
            }

            if (deltas.Length != targetPoints.Length)
            {
                throw new Exception("The deltas array must have the same length as the target points array");
            }

            Deltas = deltas;
            
            _targetPoints   = targetPoints;
            _weights        = weights;
            _triangleDeltas = triangleDeltas;
        }

        public void Execute(int index)
        {
            Vector3 point         = _targetPoints[index];
            Vector3 deformedPoint = _weights.ComputeDeformedPoint(point, index, _triangleDeltas);
            
            Deltas[index] = deformedPoint - point;
        }
    }
}