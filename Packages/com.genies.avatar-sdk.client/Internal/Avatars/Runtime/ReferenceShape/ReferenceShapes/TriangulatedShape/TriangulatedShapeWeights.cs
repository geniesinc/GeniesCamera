using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Weights data required by the <see cref="TriangulatedShape"/> to transfer deformations. It's optimized for burst
    /// compiled jobs.
    /// </summary>
    public struct TriangulatedShapeWeights : IDisposable
    {
        public struct Joint
        {
            public int   TriangleIndex;
            public float Weight;
        }
        
        public readonly int PointCount;
        
        private NativeArray<Joint> _joints;
        private NativeArray<int>   _offsets;
        private NativeArray<int>   _counts;
        
        private TriangulatedShapeWeights(int pointCount, Allocator allocator)
        {
            PointCount = pointCount;

            _joints  = default;
            _offsets = new NativeArray<int>(pointCount, allocator, NativeArrayOptions.UninitializedMemory);
            _counts  = new NativeArray<int>(pointCount, allocator, NativeArrayOptions.UninitializedMemory);
        }

        public TriangulatedShapeWeights(int pointCount, int triangleCount, Allocator allocator)
        {
            PointCount = pointCount;

            _joints  = new NativeArray<Joint>(pointCount * triangleCount, allocator, NativeArrayOptions.UninitializedMemory);
            _offsets = new NativeArray<int>(pointCount, allocator, NativeArrayOptions.UninitializedMemory);
            _counts  = new NativeArray<int>(pointCount, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < pointCount; i++)
            {
                _offsets[i] = i * triangleCount;
                _counts[i]  = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetJointCount(int pointIndex)
            => _counts[pointIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SetJointCount(int pointIndex, int count)
            => _counts[pointIndex] = count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Joint GetJoint(int pointIndex, int jointIndex)
            => _joints[_offsets[pointIndex] + jointIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetJoint(int pointIndex, int jointIndex, Joint joint)
            => _joints[_offsets[pointIndex] + jointIndex] = joint;
        
        /// <summary>
        /// Removes the given point joints when the weight divided by the given max weight is below the given threshold.
        /// You should call this method after computing the max joint weight for the point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FilterJoints(int pointIndex, int currentCount, float maxWeight, float threshold)
        {
            if (threshold <= 0.0f)
            {
                SetJointCount(pointIndex, currentCount);
                return;
            }
            
            // remove joints with a normalized weight below the threshold
            int removedCount = 0;
            for (int jointIndex = 0; jointIndex < currentCount; ++jointIndex)
            {
                Joint joint = GetJoint(pointIndex, jointIndex + removedCount);
                if (joint.Weight / maxWeight >= threshold)
                {
                    SetJoint(pointIndex, jointIndex, joint);
                    continue;
                }
                
                // "remove" the joint
                --jointIndex;
                ++removedCount;
                --currentCount;
            }
            
            SetJointCount(pointIndex, currentCount);
        }
        
        /// <summary>
        /// Given a point value with its index and triangle deltas that correspond to these weights, this method
        /// computes the deformed point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 ComputeDeformedPoint(Vector3 point, int pointIndex, NativeArray<Matrix4x4> triangleDeltas)
        {
            Vector3 deformedPoint = Vector3.zero;
            float   totalWeight   = 0.0f;
            int     jointCount    = GetJointCount(pointIndex);

            // compute the deformed point by doing a weighted sum of the deformation caused by all its triangle joints
            for (int jointIndex = 0; jointIndex < jointCount; ++jointIndex)
            {
                Joint joint = GetJoint(pointIndex, jointIndex);

                deformedPoint += joint.Weight * triangleDeltas[joint.TriangleIndex].MultiplyPoint(point);
                totalWeight   += joint.Weight;
            }

            // weights doesn't need to sum 1.0, so we have to normalize the total deformation sum
            deformedPoint /= totalWeight;
            
            return deformedPoint;
        }

        /// <summary>
        /// Returns a copy of the weights with the same data, but packed in memory for better cache performance.
        /// </summary>
        public readonly TriangulatedShapeWeights GetPackedCopy(Allocator allocator)
        {
            var copy = new TriangulatedShapeWeights(PointCount, allocator);
            
            // initialize counts
            int totalJoints = 0;
            for (int i = 0; i < _counts.Length; i++)
            {
                copy._counts[i] = _counts[i];
                totalJoints += _counts[i];
            }
            
            // initialize joints
            copy._joints = new NativeArray<Joint>(totalJoints, allocator, NativeArrayOptions.UninitializedMemory);
            int copyJointIndex = 0;
            for (int i = 0; i < _counts.Length; ++i)
            {
                copy._offsets[i] = copyJointIndex;
                int jointIndex    = _offsets[i];
                int jointIndexEnd = jointIndex + _counts[i];
                
                while (jointIndex < jointIndexEnd)
                {
                    copy._joints[copyJointIndex++] = _joints[jointIndex++];
                }
            }

            return copy;
        }

        public readonly void CalculateJoinStats(out int minJointCount, out int maxJointCount, out float averageJointCount)
        {
            minJointCount     = int.MaxValue;
            maxJointCount     = 0;
            averageJointCount = 0.0f;
            
            for (int i = 0; i < PointCount; ++i)
            {
                int count = GetJointCount(i);
                averageJointCount += count;
                
                if (count < minJointCount)
                {
                    minJointCount = count;
                }

                if (count > maxJointCount)
                {
                    maxJointCount = count;
                }
            }
            
            averageJointCount /= PointCount;
        }

        public readonly string CalculateJointStatsMessage()
        {
            CalculateJoinStats(out int minJointCount, out int maxJointCount, out float averageJointCount);
            return $"Min: {minJointCount}; Max: {maxJointCount}; Average: {averageJointCount:0.00}";
        }

        public void Dispose()
        {
            _joints.Dispose();
            _offsets.Dispose();
            _counts.Dispose();
        }
    }
}