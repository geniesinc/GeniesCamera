using System;
using System.Collections.Generic;
using System.Linq;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Handles a list of <see cref="IMeshGroupAsset"/> that is built from a given <see cref="MeshAssetCombiner"/>. It
    /// takes care generating combined materials and UV transforms for each group and handles the lifecycle of the
    /// generated resources (dispose the builder to release them).
    /// </summary>
    public sealed class MeshGroupAssetBuilder : IDisposable
    {
        public IReadOnlyList<IMeshGroupAsset> GroupAssets { get; }
        public IReadOnlyList<MeshAsset>       Assets      => _meshCombiner.Assets;
        
        /// <summary>
        /// Whether a rebuild is needed from the last time <see cref="Rebuild"/> was called.
        /// </summary>
        public bool IsDirty => _meshCombiner.IsDirty || (_hasCombinedTextures && TextureSettings.GetSettingsHashCode() != _lastTextureSettingsHashCode);
        
        /// <summary>
        /// <see cref="Genies.Utils.TextureSettings"/> used for building the combined group materials.
        /// </summary>
        public TextureSettings TextureSettings;
        
        /// <summary>
        /// When enabled, <see cref="SurfacePixelDensity"/> will be used to optimize the size of the asset textures
        /// in the final atlas based on the mesh surface from each asset (low surface assets will have smaller textures
        /// on the atlas).
        /// </summary>
        public bool UseMeshSurfaceAtlasOptimization = false;
        
        /// <summary>
        /// The surface pixel density to aim for each asset textures when building the atlas. Does nothing if
        /// <see cref="UseMeshSurfaceAtlasOptimization"/> is disabled.
        /// </summary>
        public SurfacePixelDensity SurfacePixelDensity = new()
        {
            targetDensity = 1024.0f,
            minPixelArea = 0,
            maxPixelArea = 1024,
            snappingMethod = ValueSnapping.Method.None,
        };
        
        /// <summary>
        /// If enabled, any assets generated will be cached for the next rebuild and reused if possible. Any cached
        /// assets not used for a rebuild are released after the rebuild finishes. Disable this if you want combined
        /// materials to be always rebuilt.
        /// </summary>
        public bool UseCache = true;
        
        private readonly MeshAssetCombiner           _meshCombiner;
        private List<GroupAsset>            _groupAssets;
        private List<GroupAsset>            _cachedGroupAssets;
        private readonly List<MaterialCombiner.Item> _materialItems;
        
        private int             _lastTextureSettingsHashCode; // used texture settings hash on the last build
        private bool            _areCombinedTexturesDirty; // whether combined textures are currently dirty (need rebuild)
        private bool            _hasCombinedTextures; // whether combined textures were generated in the last build

        public MeshGroupAssetBuilder()
        {
            _meshCombiner      = new MeshAssetCombiner();
            _groupAssets       = new List<GroupAsset>();
            _cachedGroupAssets = new List<GroupAsset>();
            _materialItems     = new List<MaterialCombiner.Item>();
            GroupAssets        = _groupAssets.AsReadOnly();
        }
        
        public void Add(MeshAsset asset)
            => _meshCombiner.Add(asset);
        public void Remove(MeshAsset asset)
            => _meshCombiner.Remove(asset);
        public void Add(IEnumerable<MeshAsset> assets)
            => _meshCombiner.Add(assets);
        public void Remove(IEnumerable<MeshAsset> assets)
            => _meshCombiner.Remove(assets);
        public void Clear()
            => _meshCombiner.Clear();

        /// <summary>
        /// Rebuilds the <see cref="GroupAssets"/> for the current groups on the given <see cref="meshCombiner"/>.
        /// </summary>
        public void Rebuild()
        {
            // prepares mesh combiner, groups and groups cache for a rebuild
            PrepareForRebuild();
            
            for (int i = 0; i < _meshCombiner.GroupCount; ++i)
            {
                // get group assets from the mesh combiner
                IReadOnlyList<MeshAsset> assets = _meshCombiner.GetAssets(i);
                
                // PrepareForRebuild() will prepare cache so that only reusable groups are cached
                if (TryPopCachedGroupAsset(assets, out GroupAsset groupAsset))
                {
                    _hasCombinedTextures |= groupAsset.HasCombinedTextures;
                    _groupAssets.Add(groupAsset);
                    continue;
                }
                
                // build the combined material
                bool requiresTextureCombine = _meshCombiner.RequiresTextureCombine(i);
                CombinableTextureProperty[] combinableTextureProperties = _meshCombiner.GetCombinableTextureProperties(i);
                PrepareMaterialItems(assets, requiresTextureCombine, combinableTextureProperties);
                MaterialCombiner.Result result = MaterialCombiner.CombineMaterials(_materialItems, TextureSettings, requiresTextureCombine);
                
                // create and add a new group asset
                groupAsset = new GroupAsset(assets.ToArray(), result, requiresTextureCombine);
                _hasCombinedTextures |= groupAsset.HasCombinedTextures;
                _groupAssets.Add(groupAsset);
            }
            
            // dispose all remaining cached groups
            foreach (GroupAsset asset in _cachedGroupAssets)
            {
                asset.Dispose();
            }

            _materialItems.Clear();
            _cachedGroupAssets.Clear();
        }
        
        public void Dispose()
        {
            // dispose but not clear the lists so the mesh assets within them are available to dispose later

            foreach (GroupAsset asset in _groupAssets)
            {
                asset.Dispose();
            }

            foreach (GroupAsset asset in _cachedGroupAssets)
            {
                asset.Dispose();
            }
        }

        public void DisposeOnDestroy()
        {
            foreach (GroupAsset asset in _groupAssets)
            {
                asset.Dispose();
            }

            foreach (GroupAsset asset in _cachedGroupAssets)
            {
                asset.Dispose();
            }

            _groupAssets.Clear();
            _cachedGroupAssets.Clear();
            _groupAssets = null;
            _cachedGroupAssets = null;

            _meshCombiner.DisposeOnDestroy();
        }

        private void PrepareForRebuild()
        {
            if (!TextureSettings)
            {
                throw new Exception($"[{nameof(MeshGroupAssetBuilder)}] no texture settings was set for building combined materials");
            }

            // recombine mesh assets (just the algorithm that groups the mesh assets into submeshes)
            _meshCombiner.RecombineAssets();
            
            // update texture settings hash code and check if it changed from the last rebuild
            int textureSettingsHashCode = TextureSettings.GetSettingsHashCode();
            bool textureSettingsChanged = textureSettingsHashCode != _lastTextureSettingsHashCode;
            _lastTextureSettingsHashCode = textureSettingsHashCode;
            
            _hasCombinedTextures = false;

            foreach (GroupAsset asset in _cachedGroupAssets)
            {
                asset.Dispose();
            }

            _cachedGroupAssets.Clear();

            // if not using caching then just dispose all groups
            if (!UseCache)
            {
                foreach (GroupAsset asset in _groupAssets)
                {
                    asset.Dispose();
                }

                _groupAssets.Clear();
                return;
            }
            
            // move current groups to the cached groups
            foreach (GroupAsset asset in _groupAssets)
            {
                // dispose groups that we know that we cannot reuse for this rebuild (they have combined textures and texture settings changed)
                if (textureSettingsChanged && asset.HasCombinedTextures)
                {
                    asset.Dispose();
                }
                else
                {
                    _cachedGroupAssets.Add(asset);
                }
            }
            
            _groupAssets.Clear();
        }

        private void PrepareMaterialItems(IReadOnlyList<MeshAsset> assets, bool requiresTextureCombine, CombinableTextureProperty[] combinableProperties)
        {
            // generate the materials list from the assets
            _materialItems.Clear();

            if (!requiresTextureCombine)
            {
                foreach (MeshAsset asset in assets)
                {
                    _materialItems.Add(new MaterialCombiner.Item { Material = asset.Material });
                }

                return;
            }
            
            foreach (MeshAsset asset in assets)
            {
                var item = new MaterialCombiner.Item
                {
                    Material = asset.Material,
                    TextureSize = GetCombinableTextureSize(asset, combinableProperties),
                };
                
                _materialItems.Add(item);
            }
        }

        private Vector2Int GetCombinableTextureSize(MeshAsset asset, CombinableTextureProperty[] combinableProperties)
        { 
            Vector2Int textureSize = MaterialCombinerUtility.GetCombinableTextureSize(asset.Material, combinableProperties);
            if (!UseMeshSurfaceAtlasOptimization)
            {
                return textureSize;
            }

            // calculate the required texture size to meet the target density
            float smpsu = asset.GetSquareMetersPerSquareUvs();
            float targetArea = SurfacePixelDensity.CalculateTexturePixelArea(smpsu);
            Vector2 size = textureSize;
            float aspect = size.x / size.y;
            size.x = Mathf.Sqrt(aspect * targetArea);
            size.y = size.x / aspect;
            int textureSizeX = Mathf.RoundToInt(size.x);
            int textureSizeY = Mathf.RoundToInt(size.y);
            
            // clamp the result, so we never upscale the original texture (only need to check one dimension since the aspect ratio is the same)
            if (textureSizeX < textureSize.x)
            {
                textureSize = new Vector2Int(textureSizeX, textureSizeY);
            }

            return textureSize;
        }

        private bool TryPopCachedGroupAsset(IReadOnlyList<MeshAsset> assets, out GroupAsset groupAsset)
        {
            for (int i = 0; i < _cachedGroupAssets.Count; ++i)
            {
                if (!_cachedGroupAssets[i].AssetsEquals(assets))
                {
                    continue;
                }

                groupAsset = _cachedGroupAssets[i];
                _cachedGroupAssets.RemoveAt(i);
                return true;
            }
            
            groupAsset = null;
            return false;
        }
        
        private sealed class GroupAsset : IMeshGroupAsset, IDisposable
        {
            public Material Material   => _combinerResult.Material;
            public int      AssetCount => _assets.Length;
            
            public readonly bool HasCombinedTextures;
            
            private readonly MeshAsset[]    _assets;
            private MaterialCombiner.Result _combinerResult;

            public GroupAsset(MeshAsset[] assets, MaterialCombiner.Result combinerResult, bool hasCombinedTextures)
            {
                _assets = assets;
                _combinerResult = combinerResult;
                HasCombinedTextures = hasCombinedTextures;
            }

            public MeshAsset GetAsset(int assetIndex)
            {
                return _assets[assetIndex];
            }

            public Vector2 GetUvOffset(int assetIndex)
            {
                return _combinerResult.UvOffsets[assetIndex];
            }

            public Vector2 GetUvScale(int assetIndex)
            {
                return _combinerResult.UvScales[assetIndex];
            }

            public bool AssetsEquals(IReadOnlyList<MeshAsset> assets)
            {
                if (assets.Count != _assets.Length)
                {
                    return false;
                }

                for (int i = 0; i < assets.Count; ++i)
                {
                    if (assets[i] != _assets[i])
                    {
                        return false;
                    }
                }
                
                return true;
            }

            public void Dispose()
            {
                _combinerResult.Dispose();
            }
        }
    }
}