using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Solves the weights required by the <see cref="TriangulatedShape"/> to transfer deformations using an inverted
    /// exponential RBF kernel.
    /// </summary>
    [Serializable]
    public sealed class ExponentialTswSolver : ITswSolver
    {
        [Tooltip("The exponent to use in the exponential kernel. The higher the value, the more localized the weights will be")]
        public float exponent = 4.0f;
        [Range(0.0f, 1.0f), Tooltip("The normalized weight threshold to filter out the joints with low influence")]
        public float normalizedWeightThreshold = 0.05f;
        [Tooltip("The minimum distance to consider a point inside a triangle, which means that the point will stick to that single triangle")]
        public float stickyDistance = 0.0001f;
        
        public bool verbose = false;
        
        public TriangulatedShapeWeights SolveWeights(MeshTriangles referenceTriangles, NativeArray<Vector3> targetPoints, Allocator allocator)
        {
            var job = new SolveJob(referenceTriangles, targetPoints, exponent, normalizedWeightThreshold, stickyDistance, allocator);
            job.Schedule(targetPoints.Length, targetPoints.Length / JobsUtility.MaxJobThreadCount).Complete();
            
            if (verbose)
            {
                string message = $"<color=blue>[{nameof(ExponentialTswSolver)}]</color> solved weights for <color=yellow>{targetPoints.Length}</color> points (exp: <color=yellow>{exponent}</color>, threshold: <color=yellow>{normalizedWeightThreshold}</color>)";
                message = $"{message}. Joints -> {job.Weights.CalculateJointStatsMessage()}";
                Debug.Log(message);
            }
            
            return job.Weights;
        }
        
        [BurstCompile]
        private struct SolveJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public TriangulatedShapeWeights Weights;
            
            [ReadOnly] private readonly MeshTriangles         _referenceTriangles;
            [ReadOnly] private readonly NativeArray<Vector3>  _targetPoints;
            
            [ReadOnly] private readonly float _halfInvertedExponent;
            [ReadOnly] private readonly float _weightThreshold;
            [ReadOnly] private readonly float _sqrStickyDistance;

            public SolveJob(MeshTriangles referenceTriangles, NativeArray<Vector3> targetPoints,
                float exponent, float weightThreshold, float stickyDistance, Allocator allocator)
            {
                Weights = new TriangulatedShapeWeights(targetPoints.Length, referenceTriangles.Count, allocator);
                
                _referenceTriangles = referenceTriangles;
                _targetPoints       = targetPoints;
                
                _halfInvertedExponent = -0.5f * exponent; // we multiply by 0.5, so we can use the square of the distance for more performance
                _weightThreshold      = weightThreshold;
                _sqrStickyDistance    = stickyDistance * stickyDistance;
            }

            public void Execute(int index)
            {
                Vector3 point = _targetPoints[index];

                // iterate over all triangles to get their distances to the point, also get the max weight
                float maxWeight = float.MinValue;
                for (int triangleIndex = 0; triangleIndex < _referenceTriangles.Count; ++triangleIndex)
                {
                    Vector3 closestPoint = _referenceTriangles.GetClosestPoint(triangleIndex, in point);
                    float sqrDistance = (closestPoint - point).sqrMagnitude;
                    
                    // if the distance is equal or minor to the sticky distance, it means the point is within the triangle surface, so we can skip the rest and put all the weight to this one
                    if (sqrDistance <= _sqrStickyDistance)
                    {
                        Weights.SetJoint(index, 0, new TriangulatedShapeWeights.Joint { TriangleIndex = triangleIndex, Weight = 1.0f });
                        Weights.SetJointCount(index, 1);
                        return;
                    }
                    
                    var joint = new TriangulatedShapeWeights.Joint
                    {
                        TriangleIndex = triangleIndex,
                        Weight        = Mathf.Pow(sqrDistance, _halfInvertedExponent)
                    };
                    
                    if (joint.Weight > maxWeight)
                    {
                        maxWeight = joint.Weight;
                    }

                    Weights.SetJoint(index, triangleIndex, joint);
                }
                
                Weights.FilterJoints(index, _referenceTriangles.Count, maxWeight, _weightThreshold);
            }
        }
    }
}