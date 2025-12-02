using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains the <see cref="MeshAsset"/> combination algorithm to group them together in mergeable groups (Unity
    /// submeshes). You can add/remove <see cref="MeshAsset"/> instances and call <see cref="RecombineAssets"/> to update
    /// the groupings, which will be reflected on <see cref="GroupCount"/> and values returned by
    /// <see cref="GetAssets"/> and <see cref="RequiresTextureCombine"/>.
    /// </summary>
    public sealed class MeshAssetCombiner
    {
        /// <summary>
        /// Whether a recombination is needed from the last time <see cref="RecombineAssets"/> was called.
        /// </summary>
        public bool IsDirty    { get; private set; }
        public int  GroupCount => _groups.Count;
        
        public readonly IReadOnlyList<MeshAsset> Assets;
        
        // state
        private List<MeshAsset>      _assets;
        private List<MeshGroupData>  _groups;
        private Stack<MeshGroupData> _groupsPool;

        public MeshAssetCombiner()
        {
            _assets     = new List<MeshAsset>();
            _groups     = new List<MeshGroupData>();
            _groupsPool = new Stack<MeshGroupData>();
            
            Assets = _assets.AsReadOnly();
        }

        public IReadOnlyList<MeshAsset> GetAssets(int groupIndex)
        {
            return _groups[groupIndex].ReadOnlyAssets;
        }

        public bool RequiresTextureCombine(int groupIndex)
        {
            return _groups[groupIndex].RequiresTextureCombine;
        }

        public CombinableTextureProperty[] GetCombinableTextureProperties(int groupIndex)
        {
            return _groups[groupIndex].CombinableTextureProperties;
        }
        
        public void Add(MeshAsset asset)
        {
            if (_assets.Contains(asset))
            {
                return;
            }

            _assets.Add(asset);
            IsDirty = true;
        }

        public void Remove(MeshAsset asset)
        {
            int index = _assets.IndexOf(asset);
            if (index < 0)
            {
                return;
            }

            _assets.RemoveAt(index);
            IsDirty = true;
        }

        public void Add(IEnumerable<MeshAsset> assets)
        {
            foreach (MeshAsset asset in assets)
            {
                Add(asset);
            }
        }
        
        public void Remove(IEnumerable<MeshAsset> assets)
        {
            foreach (MeshAsset asset in assets)
            {
                Remove(asset);
            }
        }

        public void Clear()
        {
            IsDirty = _assets.Count != 0;
            
            _assets.Clear();
        }

        /// <summary>
        /// Recombines current in groups, which affects <see cref="GroupCount"/> and values returned by
        /// <see cref="GetAssets"/> and <see cref="RequiresTextureCombine"/>. Calling this will force a recombination,
        /// which is not necessary if <see cref="IsDirty"/> is false.
        /// </summary>
        public void RecombineAssets()
        {
            IsDirty = false;
            ClearGroups();
            
            /**
             * 1st pass: group all free assets and leave non-free and no-merge assets in separated single-asset groups.
             */
            foreach (MeshAsset asset in _assets)
            {
                FirstPass(asset);
            }

            /**
             * 2nd pass: try to group non-free assets. Go backwards to guarantee a consistent order with the
             * original list of assets.
             */
            for (int i = _groups.Count - 1; i >= 0; --i)
            {
                SecondPass(i);
            }

            return;
            
            void FirstPass(MeshAsset asset)
            {
                // try to group free assets together
                if (!asset.NoMerge && !asset.NoTextureCombine)
                {
                    foreach (MeshGroupData existingGroup in _groups)
                    {
                        if (existingGroup.TryAddFreeAsset(asset))
                        {
                            return;
                        }
                    }
                }
                
                NewGroup(asset);
            }

            void SecondPass(int index)
            {
                // iterate only on non-free asset groups, which at this point only have one non-free asset each
                if (_groups[index].Type is not AssetType.NonFree)
                {
                    return;
                }

                for (int i = 0; i < index; ++i)
                {
                    if (!_groups[i].TryAddNonFreeAsset(_groups[index].Assets[0]))
                    {
                        continue;
                    }

                    // the non-free asset was moved to another group so release current one since it has no assets left
                    ReleaseGroup(index);
                    return;
                }
                
                for (int i = index + 1; i < _groups.Count; ++i)
                {
                    if (!_groups[i].TryAddNonFreeAsset(_groups[index].Assets[0]))
                    {
                        continue;
                    }

                    ReleaseGroup(index);
                    return;
                }
            }
        }

        private void NewGroup(MeshAsset firstAsset)
        {
            if (!_groupsPool.TryPop(out MeshGroupData group))
            {
                group = new MeshGroupData();
            }

            group.Initialize(firstAsset);
            _groups.Add(group);
        }

        private void ReleaseGroup(int index)
        {
            MeshGroupData group = _groups[index];
            group.Assets.Clear();
            _groups.RemoveAt(index);
            _groupsPool.Push(group);
        }

        private void ClearGroups()
        {
            foreach (MeshGroupData group in _groups)
            {
                group.Assets.Clear();
                _groupsPool.Push(group);
            }
            
            _groups.Clear();
        }

        public void DisposeOnDestroy()
        {
            _assets.Clear();
            _assets = null;

            foreach (MeshGroupData group in _groups)
            {
                group.DisposeOnDestroy();
            }

            foreach (MeshGroupData group in _groupsPool)
            {
                group.DisposeOnDestroy();
            }

            _groups.Clear();
            _groupsPool.Clear();
            _groups = null;
            _groupsPool = null;
        }

        private enum AssetType
        {
            Free,    // free assets have both NoMerge and NoTextureCombine flags disabled
            NonFree, // non-free assets have NoMerge disabled and NoTextureCombine enabled (they are free to merge but with the no NoTextureCombine condition)
            NoMerge  // no-merge assets have the NoMerge flag enabled
        }

        private sealed class MeshGroupData
        {
            public bool RequiresTextureCombine { get; private set; }
            
            public AssetType                   Type                        { get; private set; }
            public List<MeshAsset>             Assets                      { get; }
            public CombinableTextureProperty[] CombinableTextureProperties { get; private set; }
            
            public readonly IReadOnlyList<MeshAsset> ReadOnlyAssets;
            
            private Material _material;
            private NonCombinableProperty[] _nonCombinableProperties;

            public MeshGroupData()
            {
                Assets = new List<MeshAsset>();
                ReadOnlyAssets = Assets.AsReadOnly();
            }

            public void Initialize(MeshAsset firstAsset)
            {
                RequiresTextureCombine = false;
                Type = firstAsset.NoMerge ? AssetType.NoMerge : firstAsset.NoTextureCombine ? AssetType.NonFree : AssetType.Free;

                Assets.Clear();
                Assets.Add(firstAsset);
                
                _material = firstAsset.Material;
                CombinableTextureProperties = MaterialCombinerUtility.GetCombinableTextureProperties(_material.shader);
                _nonCombinableProperties = MaterialCombinerUtility.GetNonCombinableProperties(_material.shader);
            }

            // assumes the given asset is free
            public bool TryAddFreeAsset(MeshAsset asset)
            {
                if (!MaterialCombinerUtility.CanCombine(_material, asset.Material, _nonCombinableProperties))
                {
                    return false;
                }

                Assets.Add(asset);
                RequiresTextureCombine |= !MaterialCombinerUtility.HaveSameCombinableTextures(_material, asset.Material, CombinableTextureProperties);
                return true;
            }

            // assumes the given asset is non-free
            public bool TryAddNonFreeAsset(MeshAsset asset)
            {
                if (RequiresTextureCombine
                    || !MaterialCombinerUtility.CanCombine(_material, asset.Material, _nonCombinableProperties)
                    || !MaterialCombinerUtility.HaveSameCombinableTextures(_material, asset.Material, CombinableTextureProperties))
                {
                    return false;
                }
                
                Assets.Add(asset);
                return true;
            }

            public void DisposeOnDestroy()
            {
                _nonCombinableProperties = null;
            }
        }
    }
}