using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Genies.Utilities;
using UMA;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Avatars
{
    /// <summary>
    /// Utility for building <see cref="MeshData"/> instances from <see cref="MeshAsset"/> or <see cref="IMeshGroupAsset"/>.
    /// </summary>
    public sealed class MeshDataBuilder
    {
        private const int MaxHiddenTriangleFlagsPerAsset = 32;

        /// <summary>
        /// Whether to generate the mesh data <see cref="MeshData.BlendShapes"/> array. Changes to this property will
        /// not affect ongoing builds.
        /// </summary>
        public bool BuildBlendShapes = true;
        
        private readonly BindposeBuilder                               _bindposeBuilder        = new();
        private readonly Dictionary<string, MeshAssetTriangleFlagsSet> _hiddenTrianglesByAsset = new();
        private readonly Dictionary<string, BlendShape>                _blendShapes            = new();
        private readonly List<SubMeshDescriptor>                       _subMeshDescriptors     = new();
        private readonly List<Material>                                _materials              = new();
        
        private MeshData _data;
        
        private int  _vertexIndex;
        private int  _triangleIndex;
        private int  _boneWeightIndex;
        private int  _subMeshIndexStart;
        private bool _buildBlendShapes;

        public void Begin(IEnumerable<IMeshGroupAsset> groupAssets, Allocator allocator)
        {
            Begin(MeshDataUtility.Create(groupAssets, allocator));
        }

        public void Begin(IEnumerable<MeshAsset> assets, Allocator allocator)
        {
            Begin(MeshDataUtility.Create(assets, allocator));
        }

        /// <summary>
        /// Starts a build for the given <see cref="MeshData"/> instance, which will be returned full built when
        /// <see cref="End"/> is called.
        /// </summary>
        public void Begin(MeshData data)
        {
            ResetBuildState();
            _data = data;
        }

        /// <summary>
        /// Adds the given mesh asset to the current submesh.
        /// </summary>
        public void Add(MeshAsset asset)
        {
            Add(asset, Vector2.zero, Vector2.one);
        }

        /// <summary>
        /// Adds the given mesh asset to the current submesh, remapping its UVs with the given offset and scale vectors.
        /// </summary>
        public void Add(MeshAsset asset, Vector2 uvOffset, Vector2 uvScale)
        {
            // if we have a set of hidden triangles for this asset then use it
            if (_hiddenTrianglesByAsset.TryGetValue(asset.Id, out MeshAssetTriangleFlagsSet hiddenTriangles))
            {
                int triangleCount = asset.Indices.Length / 3;
                int meshTriangleIndex = 0;
                
                for (int triangle = 0; triangle < triangleCount; ++triangle)
                {
                    if (hiddenTriangles.HasTriangle(triangle))
                    {
                        meshTriangleIndex += 3;
                        continue;
                    }
                    
                    _data.Indices[_triangleIndex++] = (uint)(asset.Indices[meshTriangleIndex++] + _vertexIndex);
                    _data.Indices[_triangleIndex++] = (uint)(asset.Indices[meshTriangleIndex++] + _vertexIndex);
                    _data.Indices[_triangleIndex++] = (uint)(asset.Indices[meshTriangleIndex++] + _vertexIndex);
                }
            }
            else
            {
                for (int i = 0; i < asset.Indices.Length; ++i, ++_triangleIndex)
                {
                    _data.Indices[_triangleIndex] = (uint)(asset.Indices[i] + _vertexIndex);
                }
            }

            for (int i = 0; i < asset.Vertices.Length; ++i, ++_vertexIndex)
            {
                _data.Vertices      [_vertexIndex] = asset.Vertices[i];
                _data.Normals       [_vertexIndex] = asset.Normals[i];
                _data.Tangents      [_vertexIndex] = asset.Tangents[i];
                _data.Uvs           [_vertexIndex] = uvOffset + uvScale * asset.Uvs[i];
                _data.BonesPerVertex[_vertexIndex] = asset.BonesPerVertex[i];
            }
            
            foreach (UMATransform bone in asset.Bones)
            {
                _data.BonesByHash.TryAdd(bone.hash, bone);
            }

            int bindposeAddedIndexStart = _bindposeBuilder.AddedBindposeCount;
            for (int i = 0; i < asset.Bindposes.Length; ++i)
            {
                _bindposeBuilder.AddBindpose(asset.Bindposes[i], asset.IsRefitted);
            }

            for (int i = 0; i < asset.BoneWeights.Length; ++i)
            {
                BoneWeight1 weight = asset.BoneWeights[i];
                
                // update weight index to be relative to the current bindposes on the builder
                int bindposeAddedIndex = bindposeAddedIndexStart + weight.boneIndex;
                weight.boneIndex = _bindposeBuilder.GetBindposeIndexByAddedOrder(bindposeAddedIndex);
                
                _data.BoneWeights[_boneWeightIndex++] = weight;
            }

            if (_buildBlendShapes)
            {
                AddBlendShapes(asset);
            }
        }
        
        /// <summary>
        /// Adds the given mesh group to the current submesh.
        /// </summary>
        public void Add(IMeshGroupAsset groupAsset)
        {
            for (int i = 0; i < groupAsset.AssetCount; ++i)
            {
                MeshAsset asset = groupAsset.GetAsset(i);
                Vector2 uvOffset = groupAsset.GetUvOffset(i);
                Vector2 uvScale  = groupAsset.GetUvScale(i);
                
                Add(asset, uvOffset, uvScale);
            }
        }
        
        /// <summary>
        /// Adds the given mesh asset to the current submesh and ends it.
        /// </summary>
        public void AddAndEndSubMesh(MeshAsset asset, Material materialOverride = null)
        {
            Add(asset, Vector2.zero, Vector2.one);
            EndSubMesh(materialOverride ? materialOverride : asset.Material);
        }

        /// <summary>
        /// Adds the given mesh asset to the current submesh and ends it.
        /// </summary>
        public void AddAndEndSubMesh(MeshAsset asset, Vector2 uvOffset, Vector2 uvScale, Material materialOverride = null)
        {
            Add(asset, uvOffset, uvScale);
            EndSubMesh(materialOverride ? materialOverride : asset.Material);
        }
        
        /// <summary>
        /// Adds the given mesh group to the current submesh and ends it.
        /// </summary>
        public void AddAndEndSubMesh(IMeshGroupAsset groupAsset, Material materialOverride = null)
        {
            Add(groupAsset);
            EndSubMesh(materialOverride ? materialOverride : groupAsset.Material);
        }
        
        /// <summary>
        /// Ends the current submesh with the given material. Meaning that any added mesh or group assets from the last
        /// call to Begin() or EndSubMesh() will be merged toguether as a single submesh, using the provided material.
        /// </summary>
        public void EndSubMesh(Material material)
        {
            int subMeshIndexCount = _triangleIndex - _subMeshIndexStart;
            var descriptor = new SubMeshDescriptor(_subMeshIndexStart, subMeshIndexCount);
            _subMeshDescriptors.Add(descriptor);
            _materials.Add(material);
            
            _subMeshIndexStart = _triangleIndex;
        }
        
        /// <summary>
        /// Ends the build returning the built <see cref="MeshData"/> instance passed in Begin(). If EndSubMesh() was
        /// never called during the build, all the added group and mesh assets will be a single submesh.
        /// </summary>
        public MeshData End()
        {
            // make sure we define at least one submesh (the entire mesh)
            if (_subMeshDescriptors.Count == 0)
            {
                EndSubMesh(material: null);
            }

            // add submesh descriptors
            if (_data.SubMeshDescriptors?.Length != _subMeshDescriptors.Count)
            {
                _data.SubMeshDescriptors = new SubMeshDescriptor[_subMeshDescriptors.Count];
            }

            for (int i = 0; i < _subMeshDescriptors.Count; ++i)
            {
                _data.SubMeshDescriptors[i] = _subMeshDescriptors[i];
            }

            // add materials
            if (_data.Materials?.Length != _materials.Count)
            {
                _data.Materials = new Material[_materials.Count];
            }

            for (int i = 0; i < _materials.Count; ++i)
            {
                _data.Materials[i] = _materials[i];
            }

            // add bindposes
            _data.Bindposes = _bindposeBuilder.CreateBindposeArray();
            
            // add blend shapes if built
            _data.BlendShapes = _buildBlendShapes ? _blendShapes.Values.ToArray() : Array.Empty<BlendShape>();
            
            // set vertex start and length
            _data.VertexStart = 0;
            _data.VertexLength = _vertexIndex;
            
            MeshData meshData = _data;
            ResetBuildState();
            return meshData;
        }
        
        public void HideTriangles(MeshAssetTriangleFlags triangles)
        {
            string targetMeshAssetId = triangles.TargetMeshAssetId;
            if (!_hiddenTrianglesByAsset.TryGetValue(targetMeshAssetId, out MeshAssetTriangleFlagsSet hiddenTriangles))
            {
                _hiddenTrianglesByAsset.Add(targetMeshAssetId, hiddenTriangles = new MeshAssetTriangleFlagsSet(targetMeshAssetId, MaxHiddenTriangleFlagsPerAsset));
            }

            hiddenTriangles.Add(triangles);
        }
        
        public void UnhideTriangles(MeshAssetTriangleFlags triangles)
        {
            if (!_hiddenTrianglesByAsset.TryGetValue(triangles.TargetMeshAssetId, out MeshAssetTriangleFlagsSet hiddenTriangles))
            {
                return;
            }

            hiddenTriangles.Remove(triangles);
            if (hiddenTriangles.Count == 0)
            {
                _hiddenTrianglesByAsset.Remove(triangles.TargetMeshAssetId);
            }
        }

        public void HideTriangles(IEnumerable<MeshAssetTriangleFlags> triangles)
        {
            foreach (MeshAssetTriangleFlags tris in triangles)
            {
                HideTriangles(tris);
            }
        }
        
        public void UnhideTriangles(IEnumerable<MeshAssetTriangleFlags> triangles)
        {
            foreach (MeshAssetTriangleFlags tris in triangles)
            {
                UnhideTriangles(tris);
            }
        }

        public void UnhideAllTriangles()
        {
            _hiddenTrianglesByAsset.Clear();
        }
        
        private void AddBlendShapes(MeshAsset mesh)
        {
            for (int shapeIndex = 0; shapeIndex < mesh.BlendShapes.Length; ++shapeIndex)
            {
                UMABlendShape assetShape = mesh.BlendShapes[shapeIndex];
                bool createFrames;

                // if blendshape already exist then we will merge
                if (_blendShapes.TryGetValue(assetShape.shapeName, out BlendShape blendShape))
                {
                    ThrowIfCantMergeBlendShapes(assetShape, blendShape);
                    createFrames = false;
                }
                // if blend shape does not exist, create it and register on dictionary
                else
                {
                    int frameCount = assetShape.frames.Length;
                    _blendShapes[assetShape.shapeName] = blendShape = new BlendShape(assetShape.shapeName, frameCount);
                    createFrames = true;
                }
                
                for (int frameIndex = 0; frameIndex < blendShape.FrameCount; ++frameIndex)
                {
                    UMABlendFrame assetFrame = assetShape.frames[frameIndex];
                    bool hasNormals = assetFrame.HasNormals();
                    bool hasTangents = assetFrame.HasTangents();
                    
                    BlendShapeFrame frame;
                    if (createFrames)
                    {
                        // instantiate blend shape frame if not already
                        var properties = BlendShapeProperties.Vertices;
                        if (hasNormals)
                        {
                            properties |= BlendShapeProperties.Normals;
                        }

                        if (hasTangents)
                        {
                            properties |= BlendShapeProperties.Tangents;
                        }

                        blendShape.Frames[frameIndex] = frame = new BlendShapeFrame(assetFrame.frameWeight, _data.Vertices.Length, properties);
                    }
                    else
                    {
                        frame = blendShape.Frames[frameIndex];
                    }
                    
                    // merge the asset frame into the blend shape frame
                    MergeIntoFrame(frame, assetFrame, mesh.Vertices.Length, hasNormals, hasTangents);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MergeIntoFrame(BlendShapeFrame frame, UMABlendFrame assetFrame, in int vertexCount, bool hasNormals, bool hasTangents)
        {
            // not pretty but is the fastest way to implement this, and we really need performance on this class
            if (hasNormals)
            {
                if (hasTangents)
                {
                    for (int i = 0; i < vertexCount; ++i)
                    {
                        int index = _vertexIndex + i;
                        frame.DeltaVertices[index] += assetFrame.deltaVertices[i];
                        frame.DeltaNormals [index] += assetFrame.deltaNormals[i];
                        frame.DeltaTangents[index] += assetFrame.deltaTangents[i];
                    }
                    
                    return;
                }
                
                for (int i = 0; i < vertexCount; ++i)
                {
                    int index = _vertexIndex + i;
                    frame.DeltaVertices[index] += assetFrame.deltaVertices[i];
                    frame.DeltaNormals [index] += assetFrame.deltaNormals[i];
                }
                
                return;
            }
            
            for (int i = 0; i < vertexCount; ++i)
            {
                frame.DeltaVertices[_vertexIndex + i] += assetFrame.deltaVertices[i];
            }
        }
        
        private void ResetBuildState()
        {
            _bindposeBuilder.Clear();
            _blendShapes.Clear();
            _subMeshDescriptors.Clear();
            _materials.Clear();
            
            _data = null;
            _vertexIndex = 0;
            _triangleIndex = 0;
            _boneWeightIndex = 0;
            _subMeshIndexStart = 0;
            _buildBlendShapes = BuildBlendShapes;
        }

        private static void ThrowIfCantMergeBlendShapes(UMABlendShape shape, BlendShape blendShape)
        {
            if (shape.frames.Length != blendShape.FrameCount)
            {
                throw new Exception($"[{nameof(MeshDataBuilder)}] cant merge blend shapes because they have different frame count: {blendShape.Name}");
            }

            for (int i = 0; i < blendShape.FrameCount; ++i)
            {
                if (shape.frames[i].frameWeight != blendShape.Frames[i].Weight)
                {
                    throw new Exception($"[{nameof(MeshDataBuilder)}] cant merge blend shapes because they have different weights on frame {i}: {blendShape.Name}");
                }
            }
        }
    }
}