using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a set of reference points that can hold multiple deformations and transfer them to other arbitrary points.
    /// </summary>
    public interface IReferenceShape : IDisposable
    {
        string                      Id               { get; }
        int                         PointCount       { get; }
        bool                        AreDeformsLocked { get; }
        IReadOnlyCollection<string> DeformIds        { get; }
        
        /// <summary>
        /// Get the reference shape points into the given native array.
        /// </summary>
        void GetPoints(NativeArray<Vector3> points);
        
        /// <summary>
        /// Adds the given deformed points as a new deformation (or overrides an existing one) with the given id. The
        /// given deformed points native array must be owned by the caller (it won't be stored, modified or disposed).
        /// </summary>
        void AddDeform      (string deformId, NativeArray<Vector3> deformPoints);
        void RemoveDeform   (string deformId);
        bool ContainsDeform (string deformId);
        void ClearDeforms   ();
        
        /// <summary>
        /// Locks current deforms, meaning that no more deforms can be added or removed. This is useful for some
        /// implementations that may dispose the data needed for adding new deformations, or for optimizations. Call
        /// this after you know that you won't be adding more deformations. Implementations may choose to do nothing if
        /// there are no benefits on locking the deforms.
        /// </summary>
        void LockDeforms ();
        
        /// <summary>
        /// Transfer the deformation represented by the given ID to the given target points. You can optionally pass
        /// a target ID for implementations that may cache calculations for each target.
        /// </summary>
        void TransferDeform (string deformId, NativeArray<Vector3> targetPoints, string targetId = null);
        void TransferDeform (NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, string targetId = null);
        
        /// <summary>
        /// Transfer the deformation represented by the given ID to the given target points, but instead of modifying
        /// the target points array it will store the results in the given deltas array (which can be uninitialized).
        /// You can optionally pass a target ID for implementations that may cache calculations for each target.
        /// </summary>
        void TransferDeformAsDeltas (string deformId, NativeArray<Vector3> targetPoints, NativeArray<Vector3> deltas, string targetId = null);
        void TransferDeformAsDeltas (NativeArray<Vector3> deformPoints, NativeArray<Vector3> targetPoints, NativeArray<Vector3> deltas, string targetId = null);
    }
}