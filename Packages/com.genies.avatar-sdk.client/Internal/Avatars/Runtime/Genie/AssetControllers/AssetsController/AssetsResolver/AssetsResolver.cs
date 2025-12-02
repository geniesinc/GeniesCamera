using System;
using System.Collections.Generic;
using Genies.Utilities;

namespace Genies.Avatars
{
    /// <summary>
    /// Internal utility class used by the <see cref="AssetsController{TAsset}"/>.
    /// <br/><br/>
    /// Represents a collection of equipped and resolved assets that can be adjusted through
    /// 4 different operations: set, equip, unequip and unequip all. These operations are always
    /// validated. You can fully customize the validation by setting up rules for the different
    /// phases of the process.
    /// <br/><br/>
    /// All operations are performed and validated first on the equipped assets. After that,
    /// a second set of assets (the resolved assets) is validated with their specific validation
    /// rules. This is done for some cases where the actual equipped assets may differ from
    /// the assets that are equipped to the avatar. For example, you can have sunglasses and a
    /// mask equipped at the same time, in this case you want to keep both assets equipped but
    /// only equip the mask to the avatar.
    /// </summary>
    internal sealed class AssetsResolver<TAsset> : IDisposable
        where TAsset : IAsset
    {
        public IReadOnlyCollection<string> EquippedAssetIds => _equippedAssetIds;
        public IReadOnlyCollection<string> ResolvedAssetIds => _resolvedAssetIds;
        public IReadOnlyList<TAsset> AddedResolvedAssets => _addedResolvedAssets;
        public IReadOnlyList<TAsset> RemovedResolvedAssets => _removedResolvedAssets;
        public bool EquippedAssetsAdjusted { get; private set; }
        
        /// <summary>
        /// Applied before an asset is being added to the equipped assets.
        /// </summary>
        public List<IAssetsAdjustmentRule<TAsset>> EquippingAdjustmentRules { get; }
        
        /// <summary>
        /// Applied after an asset has been removed from the equipped assets.
        /// </summary>
        public List<IAssetsAdjustmentRule<TAsset>> UnequippedAdjustmentRules { get; }
        
        /// <summary>
        /// Applied to validate the equipped assets.
        /// </summary>
        public List<IAssetsValidationRule<TAsset>> ValidationRules { get; }
        
        /// <summary>
        /// Applied to validate the equipped assets to result in the resolved assets.
        /// </summary>
        public List<IAssetsValidationRule<TAsset>> ResolutionRules { get; }
        
        // state
        private readonly HashSet<TAsset> _equippedAssets;
        private readonly TrackedHashSet<TAsset> _resolvedAssets;
        private readonly HashSet<string> _equippedAssetIds;
        private readonly HashSet<string> _resolvedAssetIds;
        private readonly List<TAsset> _addedResolvedAssets;
        private readonly List<TAsset> _removedResolvedAssets;
        private readonly HashSet<string> _lastEquippedAssetIds;

        public AssetsResolver()
        {
            EquippingAdjustmentRules = new List<IAssetsAdjustmentRule<TAsset>>();
            UnequippedAdjustmentRules = new List<IAssetsAdjustmentRule<TAsset>>();
            ValidationRules = new List<IAssetsValidationRule<TAsset>>();
            ResolutionRules = new List<IAssetsValidationRule<TAsset>>();
            
            // make sure that assets are compared and hashed by their IDs only and not by the instance
            var assetComparer = new AssetEqualityComparer<TAsset>();
            _equippedAssets = new HashSet<TAsset>(assetComparer);
            _resolvedAssets = new TrackedHashSet<TAsset>(assetComparer);
            _equippedAssetIds = new HashSet<string>();
            _resolvedAssetIds = new HashSet<string>();
            _addedResolvedAssets = new List<TAsset>();
            _removedResolvedAssets = new List<TAsset>();
            _lastEquippedAssetIds = new HashSet<string>();
            
            _resolvedAssets.BeginTracking();
        }

        /// <summary>
        /// Calculates what assets were added and removed from the resolved assets since the last call to this method.
        /// The results will be exposed in the <see cref="AddedResolvedAssets"/> and <see cref="RemovedResolvedAssets"/>
        /// properties. Also, if the equipped assets were modified, it will be reflected in <see cref="EquippedAssetsAdjusted"/>.
        /// </summary>
        public void CalculateResolvedAssetsAdjustment()
        {
            _resolvedAssets.FinishTracking();
            
            _addedResolvedAssets.Clear();
            _removedResolvedAssets.Clear();
            _addedResolvedAssets.AddRange(_resolvedAssets.AddedItems);
            _removedResolvedAssets.AddRange(_resolvedAssets.RemovedItems);
            
            _resolvedAssets.BeginTracking();
            
            // calculate if equipped assets changed
            EquippedAssetsAdjusted = !_equippedAssetIds.SetEquals(_lastEquippedAssetIds);
            _lastEquippedAssetIds.Clear();
            _lastEquippedAssetIds.UnionWith(_equippedAssetIds);
        }
        
        public void SetEquippedAssets(IEnumerable<TAsset> assets)
        {
            _equippedAssets.Clear();

            foreach (TAsset asset in assets)
            {
                if (asset?.Id != null)
                {
                    _equippedAssets.Add(asset);
                }
            }
            
            ValidateAndResolveAssets();
        }

        public void EquipAsset(TAsset asset)
        {
            if (string.IsNullOrEmpty(asset?.Id) || _equippedAssets.Contains(asset))
            {
                return;
            }

            foreach (IAssetsAdjustmentRule<TAsset> rule in EquippingAdjustmentRules)
            {
                rule.Apply(_equippedAssets, asset);
            }

            _equippedAssets.Add(asset);
            ValidateAndResolveAssets();
        }
        
        public void UnequipAsset(string assetId)
        {
            if (!TryGetEquippedAsset(assetId, out TAsset asset))
            {
                return;
            }

            _equippedAssets.Remove(asset);
            
            foreach (IAssetsAdjustmentRule<TAsset> rule in UnequippedAdjustmentRules)
            {
                rule.Apply(_equippedAssets, asset);
            }

            ValidateAndResolveAssets();
        }

        public void UnequipAllAssets()
        {
            _equippedAssets.Clear();
            ValidateAndResolveAssets();
        }

        public void UnequipAllAssetsWithoutValidation()
        {
            _equippedAssets.Clear();
            _resolvedAssets.Clear();
            _equippedAssetIds.Clear();
            _resolvedAssetIds.Clear();
        }

        public void ValidateAndResolveAssets()
        {
            // apply validation rules for the equipped assets
            foreach (IAssetsValidationRule<TAsset> rule in ValidationRules)
            {
                rule.Apply(_equippedAssets);
            }

            // set the resolved assets to the equipped assets and apply the resolved validation rules
            _resolvedAssets.Clear();
            _resolvedAssets.UnionWith(_equippedAssets);
            
            foreach (IAssetsValidationRule<TAsset> rule in ResolutionRules)
            {
                rule.Apply(_resolvedAssets);
            }

            // update asset ID lists
            _equippedAssetIds.Clear();
            _resolvedAssetIds.Clear();
            
            foreach (TAsset asset in _equippedAssets)
            {
                _equippedAssetIds.Add(asset.Id);
            }

            foreach (TAsset asset in _resolvedAssets)
            {
                _resolvedAssetIds.Add(asset.Id);
            }
        }

        public bool IsAssetEquipped(string assetId)
        {
            return assetId != null && _equippedAssetIds.Contains(assetId);
        }
        
        public bool IsAssetResolved(string assetId)
        {
            return assetId != null && _resolvedAssetIds.Contains(assetId);
        }

        public void Dispose()
        {
            _equippedAssets.Clear();
            _resolvedAssets.Clear();
            _equippedAssetIds.Clear();
            _resolvedAssetIds.Clear();
            _addedResolvedAssets.Clear();
            _removedResolvedAssets.Clear();
            _lastEquippedAssetIds.Clear();
        }

        private bool TryGetEquippedAsset(string assetId, out TAsset equippedAsset)
        {
            if (assetId is null)
            {
                equippedAsset = default;
                return false;
            }
            
            foreach (TAsset asset in _equippedAssets)
            {
                if (asset.Id != assetId)
                {
                    continue;
                }

                equippedAsset = asset;
                return true;
            }
            
            equippedAsset = default;
            return false;
        }
    }
}