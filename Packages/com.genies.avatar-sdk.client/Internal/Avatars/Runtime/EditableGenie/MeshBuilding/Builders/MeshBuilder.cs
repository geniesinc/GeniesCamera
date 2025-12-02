using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using UMA;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Builds meshes from <see cref="MeshAsset"/> instances. It automatically merges mesh assets into groups and
    /// performs material combining (texture atlases). It also holds all the resources generated for each rebuild and
    /// must be disposed to release them from memory. You can do things like applying the currently built mesh to a
    /// render or building the GameObject skeleton hierarchy.
    /// <br/><br/>
    /// This is the highest level utility to build <see cref="MeshAsset"/> into a full mesh. The implementation is
    /// highly optimized to reuse resources when possible, like not rebuilding the combined material when a mesh asset
    /// group doesn't change from last rebuild. It also uses the <see cref="BlendShapeBuilder"/> to build the blend
    /// shapes in the most efficient way.
    /// </summary>
    public sealed class MeshBuilder : IDisposable
    {
        /// <summary>
        /// Current mesh assets added to the builder.
        /// </summary>
        public IReadOnlyList<MeshAsset> Assets => _meshGroupBuilder.Assets;
        
        /// <summary>
        /// Whether a rebuild is needed from the last time <see cref="Rebuild"/> was called.
        /// </summary>
        public bool IsDirty => _meshGroupBuilder.IsDirty;

        /// <summary>
        /// <see cref="TextureSettings"/> used for building the combined group materials.
        /// </summary>
        public TextureSettings TextureSettings
        {
            get => _meshGroupBuilder.TextureSettings;
            set => _meshGroupBuilder.TextureSettings = value;
        }
        
        /// <summary>
        /// When enabled, <see cref="SurfacePixelDensity"/> will be used to optimize the size of the asset textures
        /// in the final atlas based on the mesh surface from each asset (low surface assets will have smaller textures
        /// on the atlas).
        /// </summary>
        public bool UseMeshSurfaceAtlasOptimization
        {
            get => _meshGroupBuilder.UseMeshSurfaceAtlasOptimization;
            set => _meshGroupBuilder.UseMeshSurfaceAtlasOptimization = value;
        }
        
        /// <summary>
        /// The surface pixel density to aim for each asset textures when building the atlas. Does nothing if
        /// <see cref="UseMeshSurfaceAtlasOptimization"/> is disabled.
        /// </summary>
        public SurfacePixelDensity SurfacePixelDensity
        {
            get => _meshGroupBuilder.SurfacePixelDensity;
            set => _meshGroupBuilder.SurfacePixelDensity = value;
        }
        
        /// <summary>
        /// If enabled, any assets generated will be cached for the next rebuild and reused if possible. Any cached
        /// assets not used for a rebuild are released after the rebuild finishes. Disable this if you want combined
        /// materials to be always rebuilt.
        /// </summary>
        public bool UseCache
        {
            get => _meshGroupBuilder.UseCache;
            set => _meshGroupBuilder.UseCache = value;
        }
        
        /// <summary>
        /// Enable this to create one submesh per asset instead of having combinable ones merged together.
        /// </summary>
        public bool DontMergeAssets = false;
        
        // data generated on each rebuild
        public Mesh                        Mesh      => _mesh;
        public IReadOnlyList<Material>     Materials => _materials;
        public ICollection<UMATransform>   Bones     => _bones;
        public IReadOnlyList<BindposeData> Bindposes => _bindposes;
        
        private MeshGroupAssetBuilder _meshGroupBuilder;
        private MeshDataBuilder       _meshDataBuilder;
        private BlendShapeBuilder _blendShapeBuilder;
        
        private Mesh                      _mesh;
        private Material[]                _materials;
        private ICollection<UMATransform> _bones;
        private BindposeData[]            _bindposes;
        
        public MeshBuilder()
        {
            _meshGroupBuilder  = new MeshGroupAssetBuilder();
            _meshDataBuilder   = new MeshDataBuilder { BuildBlendShapes = false };
            _blendShapeBuilder = new BlendShapeBuilder();
            _materials         = Array.Empty<Material>();
            _bones             = Array.Empty<UMATransform>();
            _bindposes         = Array.Empty<BindposeData>();
        }

        public void Add(MeshAsset asset)
            => _meshGroupBuilder.Add(asset);
        public void Remove(MeshAsset asset)
            => _meshGroupBuilder.Remove(asset);
        public void Add(IEnumerable<MeshAsset> assets)
            => _meshGroupBuilder.Add(assets);
        public void Remove(IEnumerable<MeshAsset> assets)
            => _meshGroupBuilder.Remove(assets);
        public void Clear()
            => _meshGroupBuilder.Clear();
        public void HideTriangles(MeshAssetTriangleFlags triangles)
            => _meshDataBuilder.HideTriangles(triangles);
        public void UnhideTriangles(MeshAssetTriangleFlags triangles)
            => _meshDataBuilder.UnhideTriangles(triangles);
        public void HideTriangles(IEnumerable<MeshAssetTriangleFlags> triangles)
            => _meshDataBuilder.HideTriangles(triangles);
        public void UnhideTriangles(IEnumerable<MeshAssetTriangleFlags> triangles)
            => _meshDataBuilder.UnhideTriangles(triangles);
        public void UnhideAllTriangles()
            => _meshDataBuilder.UnhideAllTriangles();

        /// <summary>
        /// Rebuilds the mesh and associated data.
        /// </summary>
        public void Rebuild()
        {
            // rebuild mesh group assets from currently added mesh assets (this includes combining the assets in groups and building the combined materials with texture atlases)
            if (!DontMergeAssets)
            {
                _meshGroupBuilder.Rebuild();
            }

            // build mesh data from the produced mesh group assets
            if (DontMergeAssets)
            {
                // the mesh data builder is set to not build the blend shapes, so we can do it separately with the blend shape builder (it has better performance)
                _blendShapeBuilder.Rebuild(_meshGroupBuilder.Assets);
                _meshDataBuilder.Begin(_meshGroupBuilder.Assets, Allocator.Temp);
                foreach (MeshAsset asset in _meshGroupBuilder.Assets)
                {
                    _meshDataBuilder.AddAndEndSubMesh(asset);
                }
            }
            else
            {
                _blendShapeBuilder.Rebuild(_meshGroupBuilder.GroupAssets);
                _meshDataBuilder.Begin(_meshGroupBuilder.GroupAssets, Allocator.Temp);
                foreach (IMeshGroupAsset group in _meshGroupBuilder.GroupAssets)
                {
                    _meshDataBuilder.AddAndEndSubMesh(group);
                }
            }
            
            using MeshData meshData = _meshDataBuilder.End();

            // update current state with the built mesh data
            meshData.ApplyToMesh(_mesh ??= new Mesh());
            _blendShapeBuilder.ApplyTo(_mesh).Forget(); // reuse mesh data vertices and normals arrays for blend shape operations
            _materials = meshData.Materials;
            _bones = meshData.BonesByHash.Values;
            _bindposes = meshData.Bindposes;
        }
        
        /// <summary>
        /// Rebuilds the mesh and associated data, spread out over a few frames
        /// </summary>
        /// <remarks> Doesn't work when clothing has been changed </remarks>
        public async UniTask RebuildOverFrames()
        {
            // rebuild mesh group assets from currently added mesh assets (this includes combining the assets in groups and building the combined materials with texture atlases)
            if (!DontMergeAssets)
            {
                _meshGroupBuilder.Rebuild();
            }

            await UniTask.DelayFrame(1);
            
            // build mesh data from the produced mesh group assets
            if (DontMergeAssets)
            {
                // the mesh data builder is set to not build the blend shapes, so we can do it separately with the blend shape builder (it has better performance)
                _blendShapeBuilder.Rebuild(_meshGroupBuilder.Assets);
                _meshDataBuilder.Begin(_meshGroupBuilder.Assets, Allocator.Persistent);
                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (MeshAsset asset in _meshGroupBuilder.Assets)
                    {
                        _meshDataBuilder.AddAndEndSubMesh(asset);
                    }
                });
            }
            else
            {
                _blendShapeBuilder.Rebuild(_meshGroupBuilder.GroupAssets);
                _meshDataBuilder.Begin(_meshGroupBuilder.GroupAssets, Allocator.Persistent);
                await UniTask.RunOnThreadPool(() =>
                {
                    foreach (IMeshGroupAsset group in _meshGroupBuilder.GroupAssets)
                    {
                        _meshDataBuilder.AddAndEndSubMesh(group);
                    }
                });
            }
            
            using MeshData meshData = _meshDataBuilder.End();

            // update current state with the built mesh data
            var meshTemp = new Mesh();
            meshData.ApplyToMesh(meshTemp);

            await _blendShapeBuilder.ApplyTo(meshTemp, spreadCompute: true); // reuse mesh data vertices and normals arrays for blend shape operations
            _mesh = meshTemp;
            _materials = meshData.Materials;
            _bones = meshData.BonesByHash.Values;
            _bindposes = meshData.Bindposes;
        }
        
        /// <summary>
        /// Applies the current mesh and materials to the given renderer and builds the skeleton hierarchy.
        /// </summary>
        public void ApplyToRenderer(SkinnedMeshRenderer renderer, Transform skeletonRoot)
        {
            renderer.sharedMesh = _mesh;
            renderer.sharedMaterials = _materials;
            renderer.bones = CreateSkeletonHierarchy(skeletonRoot);
            renderer.localBounds = _mesh.bounds;
        }

        /// <summary>
        /// Creates the skeleton hierarchy from the current mesh data nested into the given skeleton root and returns
        /// the bones array for the skinned mesh renderer.
        /// </summary>
        public Transform[] CreateSkeletonHierarchy(Transform skeletonRoot)
        {
            return CreateSkeletonHierarchy(_bones, _bindposes, skeletonRoot);
        }

        public void Dispose()
        {
            if (_mesh)
            {
                Object.Destroy(_mesh);
            }

            _meshGroupBuilder?.Dispose();
            _meshDataBuilder.UnhideAllTriangles();

        }

        public void DisposeOnDestroy()
        {
            if (_mesh)
            {
                Object.Destroy(_mesh);
            }

            _meshDataBuilder.UnhideAllTriangles();
            _meshGroupBuilder?.DisposeOnDestroy();
            _blendShapeBuilder.Dispose();

            _meshGroupBuilder = null;
            _blendShapeBuilder = null;
            _meshDataBuilder = null;
        }
        
        public static void ApplyMeshDataToRenderer(MeshData data, SkinnedMeshRenderer renderer, Transform skeletonRoot)
        {
            Mesh mesh = data.CreateMesh();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = data.Materials;
            renderer.bones = CreateSkeletonHierarchy(data.BonesByHash.Values, data.Bindposes, skeletonRoot);
            renderer.localBounds = mesh.bounds;
        }

        public static Transform[] CreateSkeletonHierarchy(ICollection<UMATransform> bones, IList<BindposeData> bindposes, Transform skeletonRoot)
        {
            var transformsByHash = new Dictionary<int, (Transform transform, UMATransform umaTransform)>();

            // create bone GameObjects
            foreach (UMATransform bone in bones)
            {
                Transform transform = new GameObject(bone.name).transform;
                transformsByHash.TryAdd(bone.hash, (transform, bone));
            }
            
            // apply hierarchy (we can't do it on the same loop for the GameObject creation since we need the transformsByHash fully populated)
            foreach ((Transform transform, UMATransform umaTransform) in transformsByHash.Values)
            {
                if (transformsByHash.TryGetValue(umaTransform.parent, out (Transform transform, UMATransform _) parent))
                {
                    transform.SetParent(parent.transform, worldPositionStays: false);
                }
                else
                {
                    transform.SetParent(skeletonRoot, worldPositionStays: false);
                }

                transform.localPosition = umaTransform.position;
                transform.localRotation = umaTransform.rotation;
                transform.localScale = umaTransform.scale;
            }
            
            // create the bindpose transforms array
            var boneTransforms = new Transform[bindposes.Count];

            for (int i = 0; i < bindposes.Count; ++i)
            {
                boneTransforms[i] = transformsByHash[bindposes[i].BoneHash].transform;
            }

            return boneTransforms;
        }
    }
}