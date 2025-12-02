using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Solves the weights required by the <see cref="TriangulatedShape"/> to transfer deformations using a gaussian RBF kernel.
    /// </summary>
    [Serializable]
    public sealed class GaussTswSolver : ITswSolver
    {
        [Tooltip("The standard deviation to use in the gaussian kernel. The higher the value, the more spread the weights will be")]
        public float standardDeviation = 1.0f;
        [Range(0.0f, 1.0f), Tooltip("The normalized weight threshold to filter out the joints with low influence")]
        public float normalizedWeightThreshold = 0.1f;
        [Tooltip("The minimum distance to consider a point inside a triangle, which means that the point will stick to that single triangle")]
        public float stickyDistance = 0.0001f;
        
        public bool verbose = false;

        public TriangulatedShapeWeights SolveWeights(MeshTriangles referenceTriangles, NativeArray<Vector3> targetPoints, Allocator allocator)
        {
            var job = new SolveJob(referenceTriangles, targetPoints, standardDeviation, normalizedWeightThreshold, stickyDistance, allocator);
            job.Schedule(targetPoints.Length, targetPoints.Length / JobsUtility.MaxJobThreadCount).Complete();

            if (verbose)
            {
                string message = $"<color=blue>[{nameof(GaussTswSolver)}]</color> solved weights for <color=yellow>{targetPoints.Length}</color> points (stdev: <color=yellow>{standardDeviation}</color>, threshold: <color=yellow>{normalizedWeightThreshold}</color>)";
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
            
            [ReadOnly] private readonly float _gaussCoefficient;
            [ReadOnly] private readonly float _weightThreshold;
            [ReadOnly] private readonly float _stickyDistance;

            public SolveJob(MeshTriangles referenceTriangles, NativeArray<Vector3> targetPoints,
                float standardDeviation, float weightThreshold, float stickyDistance, Allocator allocator)
            {
                Weights = new TriangulatedShapeWeights(targetPoints.Length, referenceTriangles.Count, allocator);
                
                _referenceTriangles = referenceTriangles;
                _targetPoints       = targetPoints;
                
                _gaussCoefficient = -1.0f / 2.0f * (standardDeviation * standardDeviation);
                _weightThreshold  = weightThreshold;
                _stickyDistance   = stickyDistance;
            }

            public void Execute(int index)
            {
                Vector3 point = _targetPoints[index];

                // iterate over all triangles to get their distances to the point, also get the minimum distance
                float minDistance = float.MaxValue;
                for (int triangleIndex = 0; triangleIndex < _referenceTriangles.Count; ++triangleIndex)
                {
                    // for now, compute the distance from the point to the triangle and save it as the weight
                    Vector3 closestPoint = _referenceTriangles.GetClosestPoint(triangleIndex, in point);
                    var joint = new TriangulatedShapeWeights.Joint
                    {
                        TriangleIndex = triangleIndex,
                        Weight        = (closestPoint - point).magnitude,
                    };

                    // if the distance is equal or minor to the sticky distance, it means the point is within the triangle surface, so we can skip the rest and put all the weight to this one
                    if (joint.Weight <= _stickyDistance)
                    {
                        joint.Weight = 1.0f;
                        Weights.SetJoint(index, 0, joint);
                        Weights.SetJointCount(index, 1);
                        return;
                    }
                    
                    if (joint.Weight < minDistance)
                    {
                        minDistance = joint.Weight;
                    }

                    Weights.SetJoint(index, triangleIndex, joint);
                }

                /**
                 * Iterate over all triangles again, setting the final weights based on a distance relative to the minimum
                 * distance, and applying a gaussian function to it. Also remove any joints with a weight below the threshold.
                 * Since we know that the max weight is always 1.0, we can filter the joints here rather than using the method
                 * provided by the TriangulatedShapeWeights class.
                 */
                int count = _referenceTriangles.Count;
                int removedCount = 0;
                for (int jointIndex = 0; jointIndex < count; ++jointIndex)
                {
                    TriangulatedShapeWeights.Joint joint = Weights.GetJoint(index, jointIndex + removedCount);

                    float relativeDistance = (joint.Weight - minDistance) / minDistance;
                    joint.Weight = Mathf.Exp(_gaussCoefficient * relativeDistance * relativeDistance);

                    if (joint.Weight >= _weightThreshold)
                    {
                        Weights.SetJoint(index, jointIndex, joint);
                        continue;
                    }

                    // "remove" the joint
                    --jointIndex;
                    ++removedCount;
                    --count;
                }

                Weights.SetJointCount(index, count);
            }
        }
    }
}