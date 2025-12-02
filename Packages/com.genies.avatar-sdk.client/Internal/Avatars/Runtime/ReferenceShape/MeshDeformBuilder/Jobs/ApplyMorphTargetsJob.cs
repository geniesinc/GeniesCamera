using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    [BurstCompile]
    public struct ApplyMorphTargetsJob : IJobParallelFor
    {
        private NativeArray<Vector3> _vertices;
        
        [ReadOnly] private readonly NativeArray<Vector3> _morphTargetDeltas;
        [ReadOnly] private readonly float                _weight;
        [ReadOnly] private readonly int                  _deltasOffset;

        public ApplyMorphTargetsJob(NativeArray<Vector3> vertices,NativeArray<Vector3> morphTargetDeltas,
            float weight, int deltasOffset = 0)
        {
            _vertices           = vertices;
            _morphTargetDeltas  = morphTargetDeltas;
            _deltasOffset       = deltasOffset;
            _weight             = weight;
        }
        
        public void Execute(int index)
        {
            _vertices[index] += _weight * _morphTargetDeltas[_deltasOffset + index];
        }
    }
}