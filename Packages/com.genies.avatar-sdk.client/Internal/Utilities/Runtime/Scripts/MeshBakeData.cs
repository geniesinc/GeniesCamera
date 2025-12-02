using System;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Contains the mesh data that is required when baking blend shapes.
    /// </summary>
    public readonly struct MeshBakeData
    {
        public readonly int                  VertexCount;
        public readonly BlendShapeProperties Properties;
        public readonly Vector3[]            Vertices;
        public readonly Vector3[]            Normals;
        public readonly Vector4[]            Tangents;
        
        public MeshBakeData(Mesh mesh, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            VertexCount = mesh.vertexCount;
            Properties = properties;
            Vertices = (properties & BlendShapeProperties.Vertices) == 0 ? null : mesh.vertices;
            Normals  = (properties & BlendShapeProperties.Normals)  == 0 ? null : mesh.normals;
            Tangents = (properties & BlendShapeProperties.Tangents) == 0 ? null : mesh.tangents;
        }

        public MeshBakeData(int vertexCount, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            VertexCount = vertexCount;
            Properties = properties;
            Vertices = (properties & BlendShapeProperties.Vertices) == 0 ? null : new Vector3[vertexCount];
            Normals  = (properties & BlendShapeProperties.Normals)  == 0 ? null : new Vector3[vertexCount];
            Tangents = (properties & BlendShapeProperties.Tangents) == 0 ? null : new Vector4[vertexCount];
        }

        public void ApplyTo(Mesh mesh)
        {
            if (VertexCount != mesh.vertexCount)
            {
                throw new Exception("Cannot apply mesh bake data to a mesh with different vertex count");
            }

            if (Vertices is not null)
            {
                mesh.vertices = Vertices;
            }

            if (Normals is not null)
            {
                mesh.normals = Normals;
            }

            if (Tangents is not null)
            {
                mesh.tangents = Tangents;
            }
        }
    }
}
