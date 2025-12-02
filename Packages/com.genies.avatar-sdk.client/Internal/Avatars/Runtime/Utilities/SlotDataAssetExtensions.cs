using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    public static class SlotDataAssetExtensions
    {
        /// <summary>
        /// Computes the area of the slot mesh in world units (meters) divided by the area of the mesh in texture units (UVs).
        /// This area values can be used to compute the required texture resolutions so every slot looks at the same quality
        /// independently of their size.
        /// </summary>
        public static float GetSquareMetersPerSquareUVs(this SlotDataAsset slot)
        {
            if (!slot || slot.meshData is null)
            {
                return 0.0f;
            }

            float squareMeters = 0.0f;
            float squareUVs = 0.0f;

            UMAMeshData meshData = slot.meshData;
            Vector3[] vertices = meshData.vertices;
            SubMeshTriangles[] submeshes = meshData.submeshes;
            int[] triangles;
            int index0, index1, index2;
            Vector2 uv0, uv1, uv2;
            Vector3 vertex0, vertex1, vertex2;
            float crossX, crossY, crossZ;

            // iterate over all submeshes
            for (int submeshIndex = 0; submeshIndex < submeshes.Length; ++submeshIndex)
            {
                triangles = submeshes[submeshIndex].triangles;

                // iterate over all triangles of the submesh
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    index0 = triangles[i + 0];
                    index1 = triangles[i + 1];
                    index2 = triangles[i + 2];
                    uv0 = meshData.uv[index0];
                    uv1 = meshData.uv[index1];
                    uv2 = meshData.uv[index2];

                    // calculate the first part of the triangle UV area by reusing the crossX variable (we are using the Heron's formula)
                    // values resulting in 0 are triangles that are not mapped to any UVs. Skip them
                    crossX = uv0.x * (uv1.y - uv2.y) + uv1.x * (uv2.y - uv0.y) + uv2.x * (uv0.y - uv1.y);
                    if (crossX == 0.0f)
                    {
                        continue;
                    }

                    // finish the triangle UV area calculation and add it to the total UV area
                    squareUVs += crossX < 0 ? -0.5f * crossX : 0.5f * crossX;

                    // calculate the triangle world area
                    vertex0 = vertices[index0];
                    vertex1 = vertices[index1];
                    vertex2 = vertices[index2];

                    // fast computation of Vector3.Cross(vertex1 - vertex0, vertex2 - vertex0)
                    crossX = vertex1.x * vertex0.y - vertex2.x * vertex0.y - vertex0.x * vertex1.y + vertex2.x * vertex1.y + vertex0.x * vertex2.y - vertex1.x * vertex2.y;
                    crossY = vertex1.x * vertex0.z - vertex2.x * vertex0.z - vertex0.x * vertex1.z + vertex2.x * vertex1.z + vertex0.x * vertex2.z - vertex1.x * vertex2.z;
                    crossZ = vertex1.y * vertex0.z - vertex2.y * vertex0.z - vertex0.y * vertex1.z + vertex2.y * vertex1.z + vertex0.y * vertex2.z - vertex1.y * vertex2.z;

                    // half of the magnitude of the cross product is the triangle area. Add it to the total world area
                    squareMeters += 0.5f * Mathf.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);
                }
            }

            return squareMeters / squareUVs;
        }
    }
}
