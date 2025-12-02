using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed partial class UniquePointsShape
    {
        [Serializable]
        public struct Serializable
        {
            public string                                        id;
            public bool                                          areDeformsLocked;
            public Vector3[]                                     referencePoints;
            public Vector3[]                                     referenceUniquePoints;
            public int[]                                         referenceUniquePointIndices;
            public NativeMatrix.Serializable                     referenceSelfCorrelationPInv;
            public Dictionary<string, NativeMatrix.Serializable> deformTransforms;

            public Serializable(UniquePointsShape shape, bool? lockDeformsOverride = null)
            {
                id                    = shape.Id;
                areDeformsLocked      = lockDeformsOverride ?? shape._areDeformsLocked;
                referencePoints       = shape._referencePoints.ToArray();
                referenceUniquePoints = shape._referenceUniquePoints.ToArray();
                
                deformTransforms = new Dictionary<string, NativeMatrix.Serializable>();
                foreach ((string deformId, NativeMatrix transform) in shape._deformTransforms)
                {
                    deformTransforms.Add(deformId, new NativeMatrix.Serializable(transform));
                }

                if (areDeformsLocked)
                {
                    referenceUniquePointIndices = null;
                    referenceSelfCorrelationPInv = default;
                }
                else
                {
                    referenceUniquePointIndices = shape._referenceUniquePointIndices.ToArray();
                    referenceSelfCorrelationPInv = new NativeMatrix.Serializable(shape._referenceSelfCorrelationPInv);
                }
            }
        }

        public UniquePointsShape(Serializable serializable)
        {
            _initialized = true;
            
            Id                     = serializable.id;
            _areDeformsLocked      = serializable.areDeformsLocked;
            _referencePoints       = new NativeArray<Vector3>(serializable.referencePoints, Allocator.Persistent);
            _referenceUniquePoints = new NativeArray<Vector3>(serializable.referenceUniquePoints, Allocator.Persistent);

            if (_areDeformsLocked)
            {
                _referenceUniquePointIndices = default;
                _referenceSelfCorrelationPInv = default;
            }
            else
            {
                _referenceUniquePointIndices = new NativeArray<int>(serializable.referenceUniquePointIndices, Allocator.Persistent);
                _referenceSelfCorrelationPInv = new NativeMatrix(serializable.referenceSelfCorrelationPInv, Allocator.Persistent);
            }

            _deformTransforms = new Dictionary<string, NativeMatrix>();
            foreach ((string deformId, NativeMatrix.Serializable transform) in serializable.deformTransforms)
            {
                _deformTransforms.Add(deformId, new NativeMatrix(transform, Allocator.Persistent));
            }

            _deformPointMatrix = CreateDeformPointMatrix(_referenceUniquePoints.Length, Allocator.Persistent);
        }
    }
}