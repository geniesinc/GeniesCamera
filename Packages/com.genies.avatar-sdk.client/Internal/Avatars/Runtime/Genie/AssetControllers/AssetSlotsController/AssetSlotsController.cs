using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Base class that provides a generic a implementation for <see cref="IAssetSlotsController{TAsset}"/> interface.
    /// It takes care of caching all the asset references so child classes must only implement the specific logic for
    /// loading the assets and equipping/unequipping them from slots.
    /// </summary>
    public abstract class AssetSlotsController<TAsset> : IAssetSlotsController<TAsset>, IDisposable
        where TAsset : IAsset
    {
        // child class must only define what slots are valid, how to load an asset from its ID and what to do when an asset is equipped/unequipped from a slot
        protected abstract bool IsSlotValid(string slotId);
        protected abstract UniTask<Ref<TAsset>> LoadAssetAsync(string assetId, string slotId);
        protected abstract UniTask OnAssetEquippedAsync(TAsset asset, string slotId);
        protected abstract UniTask OnAssetUnequippedAsync(TAsset asset, string slotId);

        public IReadOnlyCollection<string> EquippedAssetIds => _equippedAssetIds;

        public event Action Updated;

        /// <summary>
        /// If false, you must call <see cref="ReleaseUnequippedAssets"/> manually to release assets that are
        /// no longer equipped. Useful if you don't want to release assets as soon as they are unequipped.
        /// </summary>
        public bool ReleaseUnequippedAssetsAutomatically
        {
            get => _releaseUnequippedAssetsAutomatically;
            set
            {
                _releaseUnequippedAssetsAutomatically = value;
                foreach (AssetSlot<TAsset> slot in _assetSlots.Values)
                {
                    slot.ReleaseUnequippedAssetsAutomatically = value;
                }
            }
        }

        public bool IsDisposed { get; private set; }

        // state
        private readonly Dictionary<string, AssetSlot<TAsset>> _assetSlots;
        private readonly HashSet<string> _equippedAssetIds;
        private bool _releaseUnequippedAssetsAutomatically;

        // sentry instrumentation
        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private string _rootTransactionName => CustomInstrumentationOperations.LoadAvatarTransaction;
        private string _rootSpan;

        protected AssetSlotsController()
        {
            _assetSlots = new Dictionary<string, AssetSlot<TAsset>>();
            _equippedAssetIds = new HashSet<string>();

            _releaseUnequippedAssetsAutomatically = true;
        }

        public async UniTask LoadAndSetEquippedAssetsAsync(IEnumerable<(string assetId, string slotId)> assets)
        {
            if (IsDisposedWithLog() || assets is null)
            {
                return;
            }

            _rootSpan = _InstrumentationManager.StartChildSpanUnderTransaction(_rootTransactionName,
                "[AssetSlotsController] LoadAndSetEquippedAssetsAsync", assets);
            var loadAndEquipTasks = ListPool<UniTask>.Get();
            var slotsToClear = HashSetPool<string>.Get();
            slotsToClear.UnionWith(_assetSlots.Keys);

            foreach ((string assetId, string slotId) in assets)
            {
                if (assetId is null || slotId is null || !IsSlotValid(slotId))
                {
                    continue;
                }

                // start a load and equip task for this asset and slot and prevent this slot from being cleared after
                UniTask loadAndEquipTask = LoadAndEquipAssetAsync(assetId, slotId);
                loadAndEquipTasks.Add(loadAndEquipTask);
                slotsToClear.Remove(slotId);
            }

            // load and equip all given assets in parallel
            await UniTask.WhenAll(loadAndEquipTasks);

            // clear all slots that were not included in the given assets
            if (!IsDisposed)
            {
                await UniTask.WhenAll(slotsToClear.Select(ClearSlotAsync));
            }

            ListPool<UniTask>.Release(loadAndEquipTasks);
            HashSetPool<string>.Release(slotsToClear);

            _InstrumentationManager.FinishChildSpan(_rootSpan);
        }

        public async UniTask SetEquippedAssetsAsync(IEnumerable<(Ref<TAsset> assetRef, string slotId)> assets)
        {
            if (IsDisposedWithLog() || assets is null)
            {
                return;
            }

            var equippingTasks = ListPool<UniTask>.Get();
            var slotsToClear = HashSetPool<string>.Get();
            slotsToClear.UnionWith(_assetSlots.Keys);

            foreach ((Ref<TAsset> assetRef, string slotId) in assets)
            {
                if (slotId is null || !IsSlotValid(slotId))
                {
                    assetRef.Dispose();
                    continue;
                }

                // equip the asset and prevent this slot from being cleared after
                UniTask loadAndEquipTask = EquipAssetAsync(assetRef, slotId);
                equippingTasks.Add(loadAndEquipTask);
                slotsToClear.Remove(slotId);
            }

            // await all equipping tasks in parallel
            await UniTask.WhenAll(equippingTasks);

            // clear all slots that were not included in the given assets
            if (!IsDisposed)
            {
                await UniTask.WhenAll(slotsToClear.Select(ClearSlotAsync));
            }

            ListPool<UniTask>.Release(equippingTasks);
            HashSetPool<string>.Release(slotsToClear);
        }

        public async UniTask LoadAndEquipAssetAsync(string assetId, string slotId)
        {
            if (IsDisposedWithLog() || assetId is null || slotId is null || !IsSlotValid(slotId))
            {
                return;
            }

            string assetSpan =
                _InstrumentationManager.StartChildSpanUnderSpan(_rootSpan, assetId, $"slot id is {slotId}");
            Ref<TAsset> assetRef = await LoadAssetAsync(assetId, slotId);
            _InstrumentationManager.FinishChildSpan(assetSpan);

            // if the controller was disposed while we were loading the asset then dispose the reference and return
            if (IsDisposed || assetRef.Item?.Id is null)
            {
                assetRef.Dispose();
                return;
            }

            if (assetRef.Item.Id != assetId)
            {
                Debug.LogWarning($"[AssetSlotsController] loaded asset with ID {assetId} but the loaded instance has ID {assetRef.Item.Id}");
            }

            await EquipAssetAsync(assetRef, slotId);
        }

        public UniTask EquipAssetAsync(Ref<TAsset> assetRef, string slotId)
        {
            if (IsDisposedWithLog() || !TryGetAssetSlot(slotId, out AssetSlot<TAsset> slotHandler))
            {
                return UniTask.CompletedTask;
            }

            return slotHandler.EquipAssetAsync(assetRef);
        }

        public UniTask ClearSlotAsync(string slotId)
        {
            if (IsDisposedWithLog() || !TryGetAssetSlot(slotId, out AssetSlot<TAsset> slotHandler))
            {
                return UniTask.CompletedTask;
            }

            return slotHandler.ClearSlotAsync();
        }

        public UniTask ClearAllSlotsAsync()
        {
            if (IsDisposedWithLog())
            {
                return UniTask.CompletedTask;
            }

            return UniTask.WhenAll(_assetSlots.Values.Select(assetSlot => assetSlot.ClearSlotAsync()));
        }

        public bool IsAssetEquipped(string assetId)
        {
            if (IsDisposedWithLog() || assetId is null)
            {
                return false;
            }

            foreach (AssetSlot<TAsset> slotHandler in _assetSlots.Values)
            {
                if (slotHandler.IsEquipped && slotHandler.EquippedAssetId == assetId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAssetEquipped(string assetId, string slotId)
        {
            return !IsDisposedWithLog() && TryGetAssetSlot(slotId, out AssetSlot<TAsset> slotHandler)
                                        && slotHandler.IsEquipped && slotHandler.EquippedAssetId == assetId;
        }

        public bool IsSlotEquipped(string slotId)
        {
            return !IsDisposedWithLog() && TryGetAssetSlot(slotId, out AssetSlot<TAsset> slotHandler) && slotHandler.IsEquipped;
        }

        public bool TryGetEquippedAssetId(string slotId, out string assetId)
        {
            if (!IsDisposedWithLog() && TryGetAssetSlot(slotId, out AssetSlot<TAsset> slotHandler))
            {
                return slotHandler.TryGetEquippedAssetId(out assetId);
            }

            assetId = null;
            return false;
        }

        public bool TryGetEquippedAsset(string slotId, out string assetId, out Ref<TAsset> assetRef)
        {
            if (!IsDisposedWithLog() && TryGetAssetSlot(slotId, out AssetSlot<TAsset> slotHandler))
            {
                return slotHandler.TryGetEquippedAsset(out assetId, out assetRef);
            }

            assetId = default;
            assetRef = default;
            return false;
        }

        public void GetSlotIdsWhereEquipped(string assetId, ICollection<string> slotIds)
        {
            if (IsDisposedWithLog() || assetId is null || slotIds is null)
            {
                return;
            }

            foreach (AssetSlot<TAsset> slotHandler in _assetSlots.Values)
            {
                if (slotHandler.IsEquipped && slotHandler.EquippedAssetId == assetId)
                {
                    slotIds.Add(slotHandler.SlotId);
                }
            }
        }

        /// <summary>
        /// If <see cref="ReleaseUnequippedAssetsAutomatically"/> is set to false, then you must call
        /// this regularly to make sure that unequipped assets get released.
        /// </summary>
        public void ReleaseUnequippedAssets()
        {
            if (IsDisposedWithLog() || _releaseUnequippedAssetsAutomatically)
            {
                return;
            }

            foreach (AssetSlot<TAsset> assetSlot in _assetSlots.Values)
            {
                assetSlot.ReleaseUnequippedAssets();
            }
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Updated = null;

            foreach (AssetSlot<TAsset> assetSlot in _assetSlots.Values)
            {
                assetSlot.Updated -= OnAnyAssetSlotUpdated;
                assetSlot.Dispose();
            }

            IsDisposed = true;
        }

        /// <summary>
        /// Use this to check if it is disposed and automatically log an error if it is. If you just want to check
        /// if the instance is disposed without logging, use the <see cref="IsDisposed"/> property.
        /// </summary>
        protected bool IsDisposedWithLog()
        {
            if (!IsDisposed)
            {
                return false;
            }

            Debug.LogError($"[AssetSlotsController] controller has been disposed but you are still trying to access it");
            return true;
        }

        private bool TryGetAssetSlot(string slotId, out AssetSlot<TAsset> assetSlot)
        {
            if (slotId is null || !IsSlotValid(slotId))
            {
                assetSlot = null;
                return false;
            }

            if (_assetSlots.TryGetValue(slotId, out assetSlot))
            {
                return true;
            }

            assetSlot = new AssetSlot<TAsset>(slotId, OnAssetEquippedAsync, OnAssetUnequippedAsync);
            assetSlot.ReleaseUnequippedAssetsAutomatically = _releaseUnequippedAssetsAutomatically;
            assetSlot.Updated += OnAnyAssetSlotUpdated;
            _assetSlots[slotId] = assetSlot;
            return true;
        }

        private void OnAnyAssetSlotUpdated()
        {
            // update the currently equipped asset IDs
            _equippedAssetIds.Clear();
            foreach (AssetSlot<TAsset> assetSlot in _assetSlots.Values)
            {
                if (assetSlot.IsEquipped)
                {
                    _equippedAssetIds.Add(assetSlot.EquippedAssetId);
                }
            }

            Updated?.Invoke();
        }
    }
}
