using System;
using Genies.Utilities;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Utility extensions to easily use <see cref="IReferenceShape"/> methods with managed arrays and minimum overhead.
    /// </summary>
    public static class ReferenceShapeExtensions
    {
        public static unsafe void GetPoints(this IReferenceShape shape, Vector3[] results)
        {
            // this is the fastest way to use points as a native array
            fixed (void* pointer = &results[0])
            {
                using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(pointer, results.Length);
                shape.GetPoints(nativePoints);
            }
        }
        
        public static NativeArray<Vector3> GetPoints(this IReferenceShape shape, Allocator allocator)
        {
            var nativePoints = new NativeArray<Vector3>(shape.PointCount, allocator, NativeArrayOptions.UninitializedMemory);
            shape.GetPoints(nativePoints);
            return nativePoints;
        }
        
        public static Vector3[] GetPoints(this IReferenceShape shape)
        {
            var pointsArray = new Vector3[shape.PointCount];
            GetPoints(shape, pointsArray);
            return pointsArray;
        }
        
        public static unsafe void TransferDeform(this IReferenceShape shape, string deformId, Vector3[] targetPoints,
            string targetId = null)
        {
            fixed (void* pointer = &targetPoints[0])
            {
                using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(pointer, targetPoints.Length);
                shape.TransferDeform(deformId, nativePoints, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            NativeArray<Vector3> targetPoints, Vector3[] deltas, string targetId = null)
        {
            fixed (void* pointer = &deltas[0])
            {
                using NativeArray<Vector3> nativeDeltas = ArrayExtensions.CreateNativeArray<Vector3>(pointer, deltas.Length);
                shape.TransferDeformAsDeltas(deformId, targetPoints, nativeDeltas, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            Vector3[] targetPoints, NativeArray<Vector3> deltas, string targetId = null)
        {
            fixed (void* pointer = &targetPoints[0])
            {
                using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(pointer, targetPoints.Length);
                shape.TransferDeformAsDeltas(deformId, nativePoints, deltas, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            Vector3[] targetPoints, Vector3[] deltas, string targetId = null)
        {
            fixed (void* deltasPointer = &deltas[0])
            {
                using NativeArray<Vector3> nativeDeltas = ArrayExtensions.CreateNativeArray<Vector3>(deltasPointer, deltas.Length);
                fixed (void* targetPointsPointer = &targetPoints[0])
                {
                    using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(targetPointsPointer, targetPoints.Length);
                    shape.TransferDeformAsDeltas(deformId, nativePoints, nativeDeltas, targetId);
                }
            }
        }

        public static Vector3[] TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            NativeArray<Vector3> targetPoints, string targetId = null)
        {
            var deltas = new Vector3[targetPoints.Length];
            TransferDeformAsDeltas(shape, deformId, targetPoints, deltas, targetId);
            return deltas;
        }

        public static Vector3[] TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            Vector3[] targetPoints, string targetId = null)
        {
            var deltas = new Vector3[targetPoints.Length];
            TransferDeformAsDeltas(shape, deformId, targetPoints, deltas, targetId);
            return deltas;
        }

        public static NativeArray<Vector3> TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            NativeArray<Vector3> targetPoints, Allocator allocator, string targetId = null)
        {
            var deltas = new NativeArray<Vector3>(targetPoints.Length, allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                shape.TransferDeformAsDeltas(deformId, targetPoints, deltas, targetId);
                return deltas;
            }
            catch(Exception)
            {
                deltas.Dispose();
                throw;
            }
        }

        public static NativeArray<Vector3> TransferDeformAsDeltas(this IReferenceShape shape, string deformId,
            Vector3[] targetPoints, Allocator allocator, string targetId = null)
        {
            var deltas = new NativeArray<Vector3>(targetPoints.Length, allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                TransferDeformAsDeltas(shape, deformId, targetPoints, deltas, targetId);
                return deltas;
            }
            catch(Exception)
            {
                deltas.Dispose();
                throw;
            }
        }
        
        public static unsafe void TransferDeform(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            Vector3[] targetPoints, string targetId = null)
        {
            fixed (void* pointer = &targetPoints[0])
            {
                using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(pointer, targetPoints.Length);
                shape.TransferDeform(deformPoints, nativePoints, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            NativeArray<Vector3> targetPoints, Vector3[] deltas, string targetId = null)
        {
            fixed (void* pointer = &deltas[0])
            {
                using NativeArray<Vector3> nativeDeltas = ArrayExtensions.CreateNativeArray<Vector3>(pointer, deltas.Length);
                shape.TransferDeformAsDeltas(deformPoints, targetPoints, nativeDeltas, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            Vector3[] targetPoints, NativeArray<Vector3> deltas, string targetId = null)
        {
            fixed (void* pointer = &targetPoints[0])
            {
                using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(pointer, targetPoints.Length);
                shape.TransferDeformAsDeltas(deformPoints, nativePoints, deltas, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            Vector3[] targetPoints, Vector3[] deltas, string targetId = null)
        {
            fixed (void* deltasPointer = &deltas[0])
            {
                using NativeArray<Vector3> nativeDeltas = ArrayExtensions.CreateNativeArray<Vector3>(deltasPointer, deltas.Length);
                fixed (void* targetPointsPointer = &targetPoints[0])
                {
                    using NativeArray<Vector3> nativePoints = ArrayExtensions.CreateNativeArray<Vector3>(targetPointsPointer, targetPoints.Length);
                    shape.TransferDeformAsDeltas(deformPoints, nativePoints, nativeDeltas, targetId);
                }
            }
        }

        public static Vector3[] TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            NativeArray<Vector3> targetPoints, string targetId = null)
        {
            var deltas = new Vector3[targetPoints.Length];
            TransferDeformAsDeltas(shape, deformPoints, targetPoints, deltas, targetId);
            return deltas;
        }

        public static Vector3[] TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            Vector3[] targetPoints, string targetId = null)
        {
            var deltas = new Vector3[targetPoints.Length];
            TransferDeformAsDeltas(shape, deformPoints, targetPoints, deltas, targetId);
            return deltas;
        }

        public static NativeArray<Vector3> TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            NativeArray<Vector3> targetPoints, Allocator allocator, string targetId = null)
        {
            var deltas = new NativeArray<Vector3>(targetPoints.Length, allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                shape.TransferDeformAsDeltas(deformPoints, targetPoints, deltas, targetId);
                return deltas;
            }
            catch(Exception)
            {
                deltas.Dispose();
                throw;
            }
        }

        public static NativeArray<Vector3> TransferDeformAsDeltas(this IReferenceShape shape, NativeArray<Vector3> deformPoints,
            Vector3[] targetPoints, Allocator allocator, string targetId = null)
        {
            var deltas = new NativeArray<Vector3>(targetPoints.Length, allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                TransferDeformAsDeltas(shape, deformPoints, targetPoints, deltas, targetId);
                return deltas;
            }
            catch(Exception)
            {
                deltas.Dispose();
                throw;
            }
        }
        
        public static unsafe void TransferDeform(this IReferenceShape shape, Vector3[] deformPoints, Vector3[] targetPoints,
            string targetId = null)
        {
            fixed (void* deformPointer = &deformPoints[0])
            {
                using NativeArray<Vector3> nativeDeformPoints = ArrayExtensions.CreateNativeArray<Vector3>(deformPointer, deformPoints.Length);
                TransferDeform(shape, nativeDeformPoints, targetPoints, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            NativeArray<Vector3> targetPoints, Vector3[] deltas, string targetId = null)
        {
            fixed (void* deformPointer = &deformPoints[0])
            {
                using NativeArray<Vector3> nativeDeformPoints = ArrayExtensions.CreateNativeArray<Vector3>(deformPointer, deformPoints.Length);
                TransferDeformAsDeltas(shape, nativeDeformPoints, targetPoints, deltas, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            Vector3[] targetPoints, NativeArray<Vector3> deltas, string targetId = null)
        {
            fixed (void* deformPointer = &deformPoints[0])
            {
                using NativeArray<Vector3> nativeDeformPoints = ArrayExtensions.CreateNativeArray<Vector3>(deformPointer, deformPoints.Length);
                TransferDeformAsDeltas(shape, nativeDeformPoints, targetPoints, deltas, targetId);
            }
        }

        public static unsafe void TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            Vector3[] targetPoints, Vector3[] deltas, string targetId = null)
        {
            fixed (void* deformPointer = &deformPoints[0])
            {
                using NativeArray<Vector3> nativeDeformPoints = ArrayExtensions.CreateNativeArray<Vector3>(deformPointer, deformPoints.Length);
                TransferDeformAsDeltas(shape, nativeDeformPoints, targetPoints, deltas, targetId);
            }
        }

        public static Vector3[] TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            NativeArray<Vector3> targetPoints, string targetId = null)
        {
            var deltas = new Vector3[targetPoints.Length];
            TransferDeformAsDeltas(shape, deformPoints, targetPoints, deltas, targetId);
            return deltas;
        }

        public static Vector3[] TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            Vector3[] targetPoints, string targetId = null)
        {
            var deltas = new Vector3[targetPoints.Length];
            TransferDeformAsDeltas(shape, deformPoints, targetPoints, deltas, targetId);
            return deltas;
        }

        public static unsafe NativeArray<Vector3> TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            NativeArray<Vector3> targetPoints, Allocator allocator, string targetId = null)
        {
            fixed (void* deformPointer = &deformPoints[0])
            {
                using NativeArray<Vector3> nativeDeformPoints = ArrayExtensions.CreateNativeArray<Vector3>(deformPointer, deformPoints.Length);
                return TransferDeformAsDeltas(shape, nativeDeformPoints, targetPoints, allocator, targetId);
            }
        }

        public static unsafe NativeArray<Vector3> TransferDeformAsDeltas(this IReferenceShape shape, Vector3[] deformPoints,
            Vector3[] targetPoints, Allocator allocator, string targetId = null)
        {
            fixed (void* deformPointer = &deformPoints[0])
            {
                using NativeArray<Vector3> nativeDeformPoints = ArrayExtensions.CreateNativeArray<Vector3>(deformPointer, deformPoints.Length);
                return TransferDeformAsDeltas(shape, nativeDeformPoints, targetPoints, allocator, targetId);
            }
        }
    }
}