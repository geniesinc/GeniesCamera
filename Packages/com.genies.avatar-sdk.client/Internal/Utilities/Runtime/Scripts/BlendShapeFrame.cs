using System;
using UnityEngine;

namespace Genies.Utilities
{
    public readonly struct BlendShapeFrame
    {
        public bool HasDeltaVertices => DeltaVertices is not null;
        public bool HasDeltaNormals  => DeltaNormals  is not null;
        public bool HasDeltaTangents => DeltaTangents is not null;
        
        public readonly float                Weight;
        public readonly int                  VertexCount;
        public readonly BlendShapeProperties Properties;
        public readonly Vector3[]            DeltaVertices;
        public readonly Vector3[]            DeltaNormals;
        public readonly Vector3[]            DeltaTangents;
        
        public BlendShapeFrame(float weight, int vertexCount, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            Weight = weight;
            VertexCount = vertexCount;
            Properties = properties;
            DeltaVertices = (properties & BlendShapeProperties.Vertices) == 0 ? null : new Vector3[vertexCount];
            DeltaNormals  = (properties & BlendShapeProperties.Normals)  == 0 ? null : new Vector3[vertexCount];
            DeltaTangents = (properties & BlendShapeProperties.Tangents) == 0 ? null : new Vector3[vertexCount];
        }

        /// <summary>
        /// Creates a new frame instance that reuses the delta arrays from another frame.
        /// </summary>
        public BlendShapeFrame(float weight, BlendShapeFrame source)
        {
            Weight = weight;
            VertexCount = source.VertexCount;
            Properties = source.Properties;
            DeltaVertices = source.DeltaVertices;
            DeltaNormals = source.DeltaNormals;
            DeltaTangents = source.DeltaTangents;
        }

        /// <summary>
        /// Bakes this frame into the given data. The interpolation is expected to range from 0 to 1, but it can fall
        /// out of range for an unclamped interpolation.
        /// </summary>
        public void Bake(MeshBakeData data, float interpolation)
        {
            if (data.Vertices is not null && DeltaVertices is not null)
            {
                BlendShapeBakingUtility.BakeDeltas(data.Vertices, DeltaVertices, interpolation);
            }

            if (data.Normals is not null && DeltaNormals is not null)
            {
                BlendShapeBakingUtility.BakeDeltas(data.Normals, DeltaNormals, interpolation);
            }

            if (data.Tangents is not null && DeltaTangents is not null)
            {
                BlendShapeBakingUtility.BakeTangents(data.Tangents, DeltaTangents, interpolation);
            }
        }
        
        /// <summary>
        /// Bakes this frame into the given data interpolating between it and the given next frame. The interpolation is
        /// expected to range from 0 to 1, but it can fall out of range for an unclamped interpolation.
        /// </summary>
        public void Bake(MeshBakeData data, BlendShapeFrame nextFrame, float interpolation)
        {
            if (data.Vertices is not null && DeltaVertices is not null && nextFrame.DeltaVertices is not null)
            {
                BlendShapeBakingUtility.BakeDeltas(data.Vertices, DeltaVertices, nextFrame.DeltaVertices, interpolation);
            }

            if (data.Normals is not null && DeltaNormals is not null && nextFrame.DeltaNormals is not null)
            {
                BlendShapeBakingUtility.BakeDeltas(data.Normals, DeltaNormals, nextFrame.DeltaNormals, interpolation);
            }

            if (data.Tangents is not null && DeltaTangents is not null && nextFrame.DeltaTangents is not null)
            {
                BlendShapeBakingUtility.BakeTangents(data.Tangents, DeltaTangents, nextFrame.DeltaTangents, interpolation);
            }
        }

        /// <summary>
        /// Adds the deltas from the other frame to this one. The other frame must have the same weight, properties and
        /// vertex count.
        /// </summary>
        public void MergeWith(BlendShapeFrame other)
        {
            if (Weight != other.Weight)
            {
                throw new Exception("Cannot merge blend shape frames because they have different weights");
            }

            if (VertexCount != other.VertexCount)
            {
                throw new Exception("Cannot merge blend shape frames because they have a different vertex count");
            }

            if (Properties != other.Properties)
            {
                throw new Exception("Cannot merge blend shape frames because they have different properties");
            }

            // no need to check for both frames since they have the same properties
            if (DeltaVertices is not null)
            {
                for (int i = 0; i < VertexCount; ++i)
                {
                    DeltaVertices[i] += other.DeltaVertices[i];
                }
            }
            
            if (DeltaNormals is not null)
            {
                for (int i = 0; i < VertexCount; ++i)
                {
                    DeltaNormals[i] += other.DeltaNormals[i];
                }
            }
            
            if (DeltaTangents is not null)
            {
                for (int i = 0; i < VertexCount; ++i)
                {
                    DeltaTangents[i] += other.DeltaTangents[i];
                }
            }
        }
    }
}
