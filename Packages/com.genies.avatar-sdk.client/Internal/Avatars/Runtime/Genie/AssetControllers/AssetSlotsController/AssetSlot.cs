using System;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Internal utility class used by <see cref="AssetSlotsController{TAsset}"/> to handle equipping/unequipping assets from each slot.
    /// </summary>
    internal sealed class AssetSlot<TAsset> : IDisposable
        where TAsset : IAsset
    {
        public string SlotId { get; }
        public string EquippedAssetId => _equippedAssetId;
        public bool IsEquipped => _equippedAssetId != null;
        
        public bool ReleaseUnequippedAssetsAutomatically
        {
            get => _releaseUnequippedAssetsAutomatically;
            set
            {
                _releaseUnequippedAssetsAutomatically = value;
                ReleaseUnequippedAssets();
            }
        }
        
        public event Action Updated;
        
        // state
        private readonly Func<TAsset, string, UniTask> _onAssetEquippedAsync;
        private readonly Func<TAsset, string, UniTask> _onAssetUnequippedAsync;
        private readonly AssetReferencesCache<TAsset> _assetReferencesCache;
        private string _equippedAssetId; // this is the currently equipped asset ID
        private string _pendingEquippedAssetId; // this is the latest state of the slot, pending to be adjusted
        private UniTaskCompletionSource _pendingResolutionOperation;
        private bool _releaseUnequippedAssetsAutomatically;

        public AssetSlot(string slotId, Func<TAsset, string, UniTask> onAssetEquippedAsync, Func<TAsset, string, UniTask> onAssetUnequippedAsync)
        {
            SlotId = slotId;
            _onAssetEquippedAsync = onAssetEquippedAsync;
            _onAssetUnequippedAsync = onAssetUnequippedAsync;
            _assetReferencesCache = new AssetReferencesCache<TAsset>();
            _releaseUnequippedAssetsAutomatically = true;
        }

        public UniTask EquipAssetAsync(Ref<TAsset> assetRef)
        {
            if (!assetRef.IsAlive || assetRef.Item?.Id is null)
            {
                return UniTask.CompletedTask;
            }

            // update the cache with the given ref
            _assetReferencesCache.Cache(assetRef);
            
            // if this asset is already equipped or pending to be equipped then return
            if (assetRef.Item.Id == _pendingEquippedAssetId)
            {
                return UniTask.CompletedTask;
            }

            _pendingEquippedAssetId = assetRef.Item.Id;
            return ResolvePendingEquippedAssetAsync();
        }

        public UniTask ClearSlotAsync()
        {
            // if the is already no equipped asset or the slot is pending to be cleared return
            if (_pendingEquippedAssetId is null)
            {
                return UniTask.CompletedTask;
            }

            _pendingEquippedAssetId = null;
            return ResolvePendingEquippedAssetAsync();
        }

        public bool TryGetEquippedAssetId(out string assetId)
        {
            assetId = _equippedAssetId;
            return IsEquipped;
        }
        
        public bool TryGetEquippedAsset(out string assetId, out Ref<TAsset> assetRef)
        {
            if (!IsEquipped)
            {
                assetId = null;
                assetRef = default;
                return false;
            }
            
            assetId = _equippedAssetId;
            return _assetReferencesCache.TryGetNewReference(_equippedAssetId, out assetRef);
        }
        
        public void ReleaseUnequippedAssets()
        {
            if (!_releaseUnequippedAssetsAutomatically)
            {
                InternalReleaseUnequippedAssets();
            }
        }
        
        public void Dispose()
        {
            DisposeAsync().Forget();
        }
        
        // will loop while the pending equipped asset differs from the currently equipped one until both are synced
        private async UniTask ResolvePendingEquippedAssetAsync()
        {
            // prevent multiple resolving operations to run at the same time
            if (_pendingResolutionOperation != null)
            {
                await _pendingResolutionOperation.Task;
                return;
            }
            
            _pendingResolutionOperation = new UniTaskCompletionSource();
            
            // loop while the currently equipped asset ID differs from the pending one
            while (_pendingEquippedAssetId != _equippedAssetId)
            {
                string lastEquippedAssetId = _equippedAssetId; 
                string equippingAssetId = _pendingEquippedAssetId;
                bool needsEquip = _assetReferencesCache.TryGetAsset(equippingAssetId, out TAsset assetToEquip);
                bool needsUnequip = _assetReferencesCache.TryGetAsset(_equippedAssetId, out TAsset assetToUnequip);

                // unequip first
                if (needsUnequip)
                {
                    await OnAssetUnequippedAsync(assetToUnequip);
                    _equippedAssetId = null;
                }
                
                // equip second
                if (needsEquip)
                {
                    await OnAssetEquippedAsync(assetToEquip);
                    _equippedAssetId = equippingAssetId;
                }
                
                // trigger the update event if there were any changes
                if (lastEquippedAssetId != _equippedAssetId)
                {
                    Updated?.Invoke();
                }
            }
            
            // release unequipped assets
            if (_releaseUnequippedAssetsAutomatically)
            {
                InternalReleaseUnequippedAssets();
            }

            // finish the resolving operation
            UniTaskCompletionSource operation = _pendingResolutionOperation;
            _pendingResolutionOperation = null;
            operation.TrySetResult();
        }

        private async UniTask OnAssetEquippedAsync(TAsset asset)
        {
            try
            {
                await _onAssetEquippedAsync(asset, SlotId);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AssetSlot] an exception was thrown when trying to equip the asset {asset.Id} to the slot {SlotId}.\n{exception}");
            }
        }
        
        private async UniTask OnAssetUnequippedAsync(TAsset asset)
        {
            try
            {
                await _onAssetUnequippedAsync(asset, SlotId);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AssetSlot] an exception was thrown when trying to unequip the asset {asset.Id} from the slot {SlotId}.\n{exception}");
            }
        }
        
        public void InternalReleaseUnequippedAssets()
        {
            if (_equippedAssetId is null)
            {
                _assetReferencesCache.ReleaseAllReferences();
                return;
            }
            
            // release all cached assets except the currently equipped one
            string[] cachedAssetIds = _assetReferencesCache.GetCachedIds();
            foreach (string assetId in cachedAssetIds)
            {
                if (assetId != _equippedAssetId)
                {
                    _assetReferencesCache.Release(assetId);
                }
            }
        }

        private async UniTaskVoid DisposeAsync()
        {
            await ClearSlotAsync();
            _equippedAssetId = null;
            _pendingEquippedAssetId = null;
            _assetReferencesCache.ReleaseAllReferences();
        }
    }
}