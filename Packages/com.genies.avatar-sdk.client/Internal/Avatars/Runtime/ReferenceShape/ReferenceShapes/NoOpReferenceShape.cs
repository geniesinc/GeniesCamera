using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class NoOpReferenceShape : IReferenceShape
    {
        public string                      Id               => "NoOp";
        public int                         PointCount       => 0;
        public IReadOnlyCollection<string> DeformIds        { get; } = Array.Empty<string>();
        public bool                        AreDeformsLocked => false;

        public void GetPoints(NativeArray<Vector3> points) { }
        public void AddDeform(string deformId, NativeArray<Vector3> deformPoints) { }
        public void RemoveDeform(string deformId) { }
        public bool ContainsDeform(string deformId) => false;
        public void ClearDeforms() { }
        public void LockDeforms() { }

        public void TransferDeform(string deformId, NativeArray<Vector3> targetPoints, string targetId = null) { }
        public void TransferDeform(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, string targetId = null) { }
        public void TransferDeformAsDeltas(string deformId, NativeArray<Vector3> targetPoints, NativeArray<Vector3> deltas, string targetId = null) { }
        public void TransferDeformAsDeltas(NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, NativeArray<Vector3> deltas, string targetId = null) { }
        
        public void Dispose() { }
    }
}