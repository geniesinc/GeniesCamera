using System.Collections.Generic;
using UnityEngine;
using UMA;
using System.Collections;

namespace Genies.Avatars
{
    /// <summary>
    /// Utilities for working with occlusion of mesh geometry by overlapping geometry.
    /// Typically this refers to faces on the avatar base body being occluded by clothing meshes.
    /// </summary>
    public static class MeshOcclusionUtilities
    { 
        /// <summary>
        /// Returns the triangles of a mesh that are unoccluded by the occlusion map.
        /// </summary>
        /// <param name="mesh">The mesh to cull.</param>
        /// <param name="occlusionMap">The occlusion map to use for culling.</param>
        /// <param name="submesh">The submesh to cull.</param>
        /// <param name="occlusionThreshold">The threshold for occlusion. Corresponds to brightness values on the occlusion map.</param>
        public static int[] GetUnoccludedTris(Mesh mesh, Texture2D occlusionMap, int submesh=0, float occlusionThreshold=0.125f) 
        {
            List<int> unoccludedTris = new List<int>();
            
            // Get occlusion bit array
            BitArray occlusionBitArray = GetOcclusionBitArray(mesh.GetTriangles(0), mesh.uv, occlusionMap, submesh, occlusionThreshold);

            // Get the array containing vertex indices (in groups of 3) for the triangles in the mesh
            int[] triangleVertices = mesh.GetTriangles(submesh);

            for (int i = 0; i < triangleVertices.Length; i += 3)
            {
                if (occlusionBitArray[i / 3])
                {
                    unoccludedTris.Add(triangleVertices[i]);
                    unoccludedTris.Add(triangleVertices[i + 1]);
                    unoccludedTris.Add(triangleVertices[i + 2]);
                }
            }
            
            return unoccludedTris.ToArray();
        }

        /// <summary>
        /// Returns a bit array indicating whether each triangle in the mesh is occluded by the occlusion map.
        /// </summary>
        /// <param name="triangleVertices">The array containing vertex indices (in groups of 3) for the triangles in the mesh.</param>
        /// <param name="uvs">The UV coordinates of the mesh.</param>   
        /// <param name="occlusionMap">The occlusion map to use for culling.</param>
        /// <param name="submesh">The submesh to cull.</param>
        /// <param name="occlusionThreshold">The threshold for occlusion. Corresponds to brightness values on the occlusion map.</param>
        public static BitArray GetOcclusionBitArray(int[] triangleVertices, Vector2[] uvs, Texture2D occlusionMap, int submesh=0, float occlusionThreshold=0.125f)
        {
            // Create a bit array to store the occlusion state of each triangle by triangle index
            BitArray occlusionBitArray = new BitArray(triangleVertices.Length / 3);

            for (int i = 0; i < triangleVertices.Length; i += 3)
            {
                // Sample the occlusion map at all three UV coordinates
                Vector2 uv0 = uvs[triangleVertices[i]];
                Vector2 uv1 = uvs[triangleVertices[i + 1]];
                Vector2 uv2 = uvs[triangleVertices[i + 2]];
                Vector3 uvCenter = (uvs[triangleVertices[i]] + uvs[triangleVertices[i + 1]] + uvs[triangleVertices[i + 2]]) / 3f;
                
                // If anny of the three UV coordinates are unoccluded, keep the triangle
                if (occlusionMap.GetPixelBilinear(uv0.x, uv0.y).r > occlusionThreshold ||
                    occlusionMap.GetPixelBilinear(uv1.x, uv1.y).r > occlusionThreshold ||
                    occlusionMap.GetPixelBilinear(uv2.x, uv2.y).r > occlusionThreshold ||
                    occlusionMap.GetPixelBilinear(uvCenter.x, uvCenter.y).r > occlusionThreshold // Test the center of the triangle for edge cases
                    )
                {
                    occlusionBitArray[i / 3] = false;
                }
                else
                {
                    occlusionBitArray[i / 3] = true;
                }
            }
            
            return occlusionBitArray;
        }

        /// <summary>
        /// Returns a MeshHideAsset for the given SlotDataAsset and occlusion map.
        /// </summary>
        /// <param name="slotDataAsset">The UMA SlotDataAsset to create the MeshHideAsset for.</param>
        /// <param name="occlusionMap">The occlusion map to use for culling.</param>
        /// <param name="occlusionThreshold">The threshold for occlusion. Corresponds to brightness values on the occlusion map.</param>
        public static MeshHideAsset GetMeshHideAssetFromOcclusionMap(SlotDataAsset slotDataAsset, Texture2D occlusionMap, float occlusionThreshold=0.125f)
        {
            MeshHideAsset meshHideAsset = ScriptableObject.CreateInstance<MeshHideAsset>();
            meshHideAsset.asset = slotDataAsset;
            meshHideAsset.Initialize();
            
            meshHideAsset.SaveSelection(
                GetOcclusionBitArray(slotDataAsset.meshData.submeshes[0].triangles, slotDataAsset.meshData.uv, occlusionMap, 0, occlusionThreshold)
                );

            return meshHideAsset;
        }
    }
}
