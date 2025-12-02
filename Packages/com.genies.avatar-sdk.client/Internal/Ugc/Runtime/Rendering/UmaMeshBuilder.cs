using Genies.Refs;
using UMA;
using Unity.Collections;
using UnityEngine;

namespace Genies.Ugc
{
    public class UmaMeshBuilder
    {
        /// <summary>
        /// Builds a Mesh asset from the given slot data asset. The given SlotDataAsset instance can be safely destroyed afterwards
        /// as the generated mesh does not depend on it.
        /// </summary>
        public static Ref<Mesh> BuildMesh(SlotDataAsset slotDataAsset)
        {
            var meshData = slotDataAsset.meshData;

            // build mesh
            var mesh = new Mesh
            {
                name = $"generated-from-{slotDataAsset.name}",
                vertices = meshData.vertices,
                normals = meshData.normals,
                uv = meshData.uv,
                uv2 = meshData.uv2,
                uv3 = meshData.uv3,
                uv4 = meshData.uv4,
                colors32 = meshData.colors32,
                bindposes = meshData.bindPoses,
                // build submeshes
                subMeshCount = meshData.subMeshCount
            };

            for (int i = 0; i < meshData.subMeshCount; i++)
            {
                int[] triangles = meshData.submeshes[i].triangles;
                NativeArray<int> nativeTriangles = new NativeArray<int>(triangles, Allocator.Temp);
                mesh.SetIndices(triangles, MeshTopology.Triangles, i);
                nativeTriangles.Dispose();
            }

            return CreateRef.FromUnityObject(mesh);
        }
        public Ref<MeshRenderer> ToMeshRenderer(SlotDataAsset slotDataAsset)
        {
            return MakeRenderer(slotDataAsset, null);
        }
        
        public Ref<MeshRenderer> ToMeshRenderer(SlotDataAsset slotDataAsset, Material rendererMaterial)
        {
            return MakeRenderer(slotDataAsset, rendererMaterial);
        }
        
        private Ref<MeshRenderer> MakeRenderer(SlotDataAsset slotDataAsset, Material rendererMaterial)
        {
            var go = CreateRef.FromUnityObject(new GameObject("GeneratedMeshRenderer"));
            var meshFilter = go.Item.AddComponent<MeshFilter>();
            var meshRef = CreateRef.FromUnityObject(MakeMeshFrom(slotDataAsset));
            meshFilter.mesh = meshRef.Item;

            var meshRenderer = go.Item.AddComponent<MeshRenderer>();
            meshRenderer.material = (rendererMaterial != null) ? rendererMaterial : slotDataAsset.material.material;
            var meshRendererRef = CreateRef.FromUnityObject(meshRenderer);
            
            return CreateRef.FromDependentResource(meshRendererRef, go, meshRef);
        }

        private Mesh MakeMeshFrom(SlotDataAsset slotDataAsset)
        {
            var meshData = slotDataAsset.meshData;

            var mesh = new Mesh();
            mesh.vertices = meshData.vertices;
            mesh.normals = meshData.normals;
            mesh.uv = meshData.uv;
            mesh.uv2 = meshData.uv2;
            mesh.uv3 = meshData.uv3;
            mesh.uv4 = meshData.uv4;
            mesh.colors32 = meshData.colors32;
            mesh.bindposes = meshData.bindPoses;
            
            mesh.subMeshCount = meshData.subMeshCount;
            for (int i = 0; i < meshData.subMeshCount; i++)
            {
                int[] tris = meshData.submeshes[i].triangles;
                NativeArray<int> triangles = new NativeArray<int>(tris, Allocator.Temp);
                mesh.SetIndices(tris, MeshTopology.Triangles, i);
                triangles.Dispose();
            }
            
            mesh.name = $"generated-from-{slotDataAsset.name}";

            return mesh;
        }
    }
}
