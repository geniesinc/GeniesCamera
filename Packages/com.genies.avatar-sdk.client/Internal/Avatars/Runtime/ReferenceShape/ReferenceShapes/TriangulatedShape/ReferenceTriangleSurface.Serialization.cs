using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed partial class TriangulatedShape
    {
        /// <summary>
        /// Supports serialization of a <see cref="TriangulatedShape"/>. In practice, initialization is so fast that
        /// deserializing a cached instance is much slower.
        /// </summary>
        [Serializable, JsonObject(ItemTypeNameHandling = TypeNameHandling.Auto)] // set to auto so the tws solver is serialized/deserialized properly
        public struct Serializable
        {
            public string                          id;
            public bool                            areDeformsLocked;
            public Vector3[]                       referenceVertices;
            public int[]                           referenceIndices;
            public ITswSolver                      weightsSolver;
            public Dictionary<string, Matrix4x4[]> deformTriangleDeltas;
            
            public Serializable(TriangulatedShape shape)
            {
                id                 = shape.Id;
                areDeformsLocked   = shape._areDeformsLocked;
                referenceVertices  = shape._referenceVertices.ToArray();
                referenceIndices   = shape._referenceIndices.ToArray();
                weightsSolver      = shape._weightsSolver;

                deformTriangleDeltas = new Dictionary<string, Matrix4x4[]>();
                foreach ((string deformId, NativeArray<Matrix4x4> deltas) in shape._deformTriangleDeltas)
                {
                    deformTriangleDeltas.Add(deformId, deltas.ToArray());
                }
            }
        }

        public TriangulatedShape(Serializable serializable)
        {
            _initialized = true;
            
            Id                  = serializable.id;
            _areDeformsLocked   = serializable.areDeformsLocked;
            _referenceVertices  = new NativeArray<Vector3>(serializable.referenceVertices, Allocator.Persistent);
            _referenceIndices   = new NativeArray<int>(serializable.referenceIndices, Allocator.Persistent);
            _referenceTriangles = new MeshTriangles(_referenceVertices, _referenceIndices);
            _weightsSolver      = serializable.weightsSolver;

            _deformTriangleDeltas = new Dictionary<string, NativeArray<Matrix4x4>>();
            foreach ((string deformId, Matrix4x4[] deltas) in serializable.deformTriangleDeltas)
            {
                _deformTriangleDeltas.Add(deformId, new NativeArray<Matrix4x4>(deltas, Allocator.Persistent));
            }
        }
    }
}