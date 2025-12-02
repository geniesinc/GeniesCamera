using System.Collections.Generic;
using UMA;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    public static class MeshDataUtility
    {
        /// <summary>
        /// Builds and returns a <see cref="MeshData"/> object from the given mesh assets. This method doesn't combine
        /// the assets, meaning that each asset will be its own submesh in the returned data.
        /// </summary>
        public static MeshData BuildMeshData(IEnumerable<MeshAsset> assets, Allocator allocator, MeshDataBuilder builder = null)
        {
            builder ??= new MeshDataBuilder();
            builder.Begin(assets, allocator);

            foreach (MeshAsset asset in assets)
            {
                builder.AddAndEndSubMesh(asset);
            }

            return builder.End();
        }
        
        /// <summary>
        /// Builds and returns a <see cref="MeshData"/> object from the given mesh group assets.
        /// </summary>
        public static MeshData BuildMeshData(IEnumerable<IMeshGroupAsset> groups, Allocator allocator, MeshDataBuilder builder = null)
        {
            builder ??= new MeshDataBuilder();
            builder.Begin(groups, allocator);

            foreach (IMeshGroupAsset group in groups)
            {
                builder.AddAndEndSubMesh(group);
            }

            return builder.End();
        }
        
        public static MeshData Create(IEnumerable<IMeshGroupAsset> groupAssets, Allocator allocator)
        {
            int vertexCount = 0;
            int indexCount = 0;
            int boneWeightsCount = 0;

            foreach (IMeshGroupAsset groupAsset in groupAssets)
            {
                for (int i = 0; i < groupAsset.AssetCount; ++i)
                {
                    vertexCount += groupAsset.GetAsset(i).Vertices.Length;
                    indexCount += groupAsset.GetAsset(i).Indices.Length;
                    boneWeightsCount += groupAsset.GetAsset(i).BoneWeights.Length;
                }
            }
            
            return Create(vertexCount, indexCount, boneWeightsCount, allocator);
        }

        public static MeshData Create(IEnumerable<MeshAsset> assets, Allocator allocator)
        {
            int vertexCount = 0;
            int indexCount = 0;
            int boneWeightsCount = 0;

            foreach (MeshAsset asset in assets)
            {
                vertexCount += asset.Vertices.Length;
                indexCount += asset.Indices.Length;
                boneWeightsCount += asset.BoneWeights.Length;
            }
            
            return Create(vertexCount, indexCount, boneWeightsCount, allocator);
        }
        
        public static MeshData Create(int vertexCount, int indexCount, int boneWeightsCount, Allocator allocator)
        {
            return new MeshData
            {
                SubMeshDescriptors = null,
                Materials          = null,
                Indices            = new NativeArray<uint>(indexCount, allocator, NativeArrayOptions.UninitializedMemory),
                Vertices           = new NativeArray<Vector3>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory),
                Normals            = new NativeArray<Vector3>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory),
                Tangents           = new NativeArray<Vector4>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory),
                Uvs                = new NativeArray<Vector2>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory),
                BonesPerVertex     = new NativeArray<byte>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory),
                BonesByHash        = new Dictionary<int, UMATransform>(),
                Bindposes          = null,
                BoneWeights        = new NativeArray<BoneWeight1>(boneWeightsCount, allocator, NativeArrayOptions.UninitializedMemory),
                BlendShapes        = null,
            };
        }
    }
}