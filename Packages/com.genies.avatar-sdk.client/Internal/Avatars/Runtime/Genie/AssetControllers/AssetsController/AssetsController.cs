using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Base class that provides a generic a implementation for <see cref="IAssetsController{TAsset}"/> interface.
    /// It takes care of caching all the asset references so child classes must only implement the specific logic for
    /// loading the assets and equipping/unequipping them.
    /// <br/><br/>
    /// This implementation assumes that equipping/unequipping operations are not just simple additions or removals
    /// to the equipped assets. For example, equipping an asset can result in other assets being unequipped.
    /// <br/><br/>
    /// It is also assumed that equipped assets may differ from the assets that are really equipped to the avatar
    /// (the resolved assets). For example, you may have not explicitly equipped underwear assets but you may want
    /// to enforce them when there is no equipped asset covering certain parts of the body.
    /// <br/><br/>
    /// In order to customize the rules for these operations this class exposes 4 protected lists of validation rules
    /// that should fulfill any use case.
    /// </summary>
    public abstract class AssetsController<TAsset> : IAssetsController<TAsset>, IDisposable
        where TAsset : IAsset
    {
        // child class must only define how to load an asset from its ID and what to do when an asset is equipped/unequipped
        protected abstract UniTask<Ref<TAsset>> LoadAssetAsync(string assetId);
        protected abstract UniTask OnAssetEquippedAsync(TAsset asset);
        protected abstract UniTask OnAssetUnequippedAsync(TAsset asset);

        public IReadOnlyCollection<string> EquippedAssetIds => _assetsResolver.EquippedAssetIds;

        public event Action Updated;

        /// <summary>
        /// Asset IDs that are really equipped to the avatar (may differ from the equipped assets).
        /// </summary>
        public IReadOnlyCollection<string> ResolvedAssetIds => _assetsResolver.ResolvedAssetIds;

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
                ReleaseUnequippedAssets();
            }
        }

        public bool IsDisposed { get; private set; }

        // rules (they are protected because some implementations may have fixed rules and there is no need to expose them)
        protected List<IAssetsAdjustmentRule<TAsset>> EquippingAdjustmentRules => _assetsResolver.EquippingAdjustmentRules;
        protected List<IAssetsAdjustmentRule<TAsset>> UnequippedAdjustmentRules => _assetsResolver.UnequippedAdjustmentRules;
        protected List<IAssetsValidationRule<TAsset>> ValidationRules => _assetsResolver.ValidationRules;
        protected List<IAssetsValidationRule<TAsset>> ResolutionRules => _assetsResolver.ResolutionRules;

        // state
        private readonly AssetReferencesCache<TAsset> _assetReferencesCache;
        private readonly AssetsResolver<TAsset> _assetsResolver;
        private UniTaskCompletionSource _adjustmentOperation;
        private bool _releaseUnequippedAssetsAutomatically;

        // helpers
        private readonly Func<TAsset, UniTask> _equipResolvedAssetDelegate;
        private readonly Func<TAsset, UniTask> _unequipResolvedAssetDelegate;

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private string _rootTransactionName => CustomInstrumentationOperations.LoadAvatarTransaction;

        protected AssetsController()
        {
            _assetReferencesCache = new AssetReferencesCache<TAsset>();
            _assetsResolver = new AssetsResolver<TAsset>();
            _releaseUnequippedAssetsAutomatically = true;

            // allocate delegate instances to this methods once so they are not allocated each time we use them with Linq in the AdjustResolvedAssetsAsync method
            _equipResolvedAssetDelegate = EquipResolvedAssetAsync;
            _unequipResolvedAssetDelegate = UnequipResolvedAssetAsync;
        }

        public async UniTask LoadAndSetEquippedAssetsAsync(IEnumerable<string> assetIds)
        {
            string rootSpan = _InstrumentationManager.StartChildSpanUnderTransaction(_rootTransactionName,
                "[AssetsController] LoadAndSetEquippedAssetsAsync");
            if (IsDisposedWithLog())
            {
                return;
            }

            // load all assets
            Ref<TAsset>[] assetRefs = await UniTask.WhenAll(assetIds.Select(async id =>
            {
                string loadAssetSpan = _InstrumentationManager.StartChildSpanUnderSpan(rootSpan, id);
                Ref<TAsset> assetRef = await LoadAssetAsync(id);
                _InstrumentationManager.FinishChildSpan(loadAssetSpan);
                return assetRef;
            }));

            // if the controller was disposed while trying to load the assets then dispose all references and return
            if (IsDisposed)
            {
                foreach (Ref<TAsset> assetRef in assetRefs)
                {
                    assetRef.Dispose();
                }

                return;
            }

            await SetEquippedAssetsAsync(assetRefs);

            _InstrumentationManager.FinishChildSpan(rootSpan);
        }

        public UniTask SetEquippedAssetsAsync(IEnumerable<Ref<TAsset>> assetRefs)
        {
            if (IsDisposedWithLog())
            {
                foreach (Ref<TAsset> assetRef in assetRefs)
                {
                    assetRef.Dispose();
                }

                return UniTask.CompletedTask;
            }

            var assets = new List<TAsset>();

            // cache alive references and get the asset instances
            foreach (Ref<TAsset> assetRef in assetRefs)
            {
                if (!assetRef.IsAlive || assetRef.Item?.Id is null)
                {
                    assetRef.Dispose();
                    continue;
                }

                // cache the asset ref
                _assetReferencesCache.Cache(assetRef);
                assets.Add(assetRef.Item);
            }

            // set equipped assets to the resolver and adjust the resolved assets
            _assetsResolver.SetEquippedAssets(assets);
            return AdjustResolvedAssetsAsync();
        }

        public async UniTask LoadAndEquipAssetAsync(string assetId)
        {
            if (IsDisposedWithLog() || assetId is null)
            {
                return;
            }

            // if the asset is already cached, then just equip it to the resolver and adjust
            if (_assetReferencesCache.TryGetAsset(assetId, out TAsset asset))
            {
                _assetsResolver.EquipAsset(asset);
                await AdjustResolvedAssetsAsync();
                return;
            }

            // try to load the asset from the ID
            Ref<TAsset> assetRef = await LoadAssetAsync(assetId);
            if (!assetRef.IsAlive)
            {
                return;
            }

            // if the controller was disposed while trying to load the asset then dispose the reference and do nothing
            if (IsDisposed || assetRef.Item?.Id is null)
            {
                assetRef.Dispose();
                return;
            }

            if (assetRef.Item.Id != assetId)
            {
                Debug.LogWarning($"[AssetsController] loaded asset with ID {assetId} but the loaded instance has ID {assetRef.Item.Id}");
            }

            // equip the asset to the resolver and adjust the resolved assets
            _assetReferencesCache.Cache(assetRef);
            _assetsResolver.EquipAsset(assetRef.Item);
            await AdjustResolvedAssetsAsync();
        }

        public UniTask EquipAssetAsync(Ref<TAsset> assetRef)
        {
            if (IsDisposedWithLog() || !assetRef.IsAlive)
            {
                assetRef.Dispose();
                return UniTask.CompletedTask;
            }

            // equip the asset to the resolver and adjust the resolved assets
            _assetReferencesCache.Cache(assetRef);
            _assetsResolver.EquipAsset(assetRef.Item);
            return AdjustResolvedAssetsAsync();
        }

        public async UniTask UnequipAssetAsync(string assetId)
        {
            if (IsDisposedWithLog() || assetId is null)
            {
                return;
            }

            // unequip the asset from the resolver and adjust the resolved assets
            _assetsResolver.UnequipAsset(assetId);
            await AdjustResolvedAssetsAsync();
        }

        public UniTask UnequipAllAssetsAsync()
        {
            if (IsDisposedWithLog())
            {
                return UniTask.CompletedTask;
            }

            _assetsResolver.UnequipAllAssets();
            return AdjustResolvedAssetsAsync();
        }

        public bool IsAssetEquipped(string assetId)
        {
            return !IsDisposedWithLog() && _assetsResolver.IsAssetEquipped(assetId);
        }

        public bool IsAssetResolved(string assetId)
        {
            return !IsDisposedWithLog() && _assetsResolver.IsAssetResolved(assetId);
        }

        public bool TryGetEquippedAsset(string assetId, out Ref<TAsset> assetRef)
        {
            if (!IsDisposedWithLog())
            {
                return _assetReferencesCache.TryGetNewReference(assetId, out assetRef);
            }

            assetRef = default;
            return false;
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

            InternalReleaseUnequippedAssets();
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Updated = null; // make sure we don't trigger any updated events since we have disposed
            IsDisposed = true;
            DisposeAsync().Forget();
        }

        /// <summary>
        /// Use this method when modifying rules if you want them to be reapplied.
        /// </summary>
        protected UniTask ValidateAndResolveAssetsAsync()
        {
            _assetsResolver.ValidateAndResolveAssets();
            return AdjustResolvedAssetsAsync();
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

            Debug.LogError($"[AssetController] controller has been disposed but you are still trying to access it");
            return true;
        }

        // equips and unequips assets so the current resolved assets are synced with the validated ones
        private async UniTask AdjustResolvedAssetsAsync()
        {
            // prevent multiple adjustment operations to run at the same time
            if (_adjustmentOperation != null)
            {
                await _adjustmentOperation.Task;
                return;
            }

            _adjustmentOperation = new UniTaskCompletionSource();

            // calculate what resolved assets were added and removed since the last adjustment
            _assetsResolver.CalculateResolvedAssetsAdjustment();
            bool equippedAssetsAdjusted = _assetsResolver.EquippedAssetsAdjusted;

            // loop while there are adjustments to perform
            while (_assetsResolver.AddedResolvedAssets.Count > 0 || _assetsResolver.RemovedResolvedAssets.Count > 0)
            {
                await UniTask.WhenAll(_assetsResolver.RemovedResolvedAssets.Select(_unequipResolvedAssetDelegate));
                await UniTask.WhenAll(_assetsResolver.AddedResolvedAssets.Select(_equipResolvedAssetDelegate));

                // after the previous async operations there could be new adjustments to do
                _assetsResolver.CalculateResolvedAssetsAdjustment();
                equippedAssetsAdjusted |= _assetsResolver.EquippedAssetsAdjusted;
            }

            // release unequipped assets
            if (_releaseUnequippedAssetsAutomatically)
            {
                InternalReleaseUnequippedAssets();
            }

            // finish the adjustment operation
            UniTaskCompletionSource operation = _adjustmentOperation;
            _adjustmentOperation = null;
            operation.TrySetResult();

            // if the equipped assets were adjusted at least once, then fire the updated event
            if (equippedAssetsAdjusted)
            {
                Updated?.Invoke();
            }
        }

        private async UniTask EquipResolvedAssetAsync(TAsset asset)
        {
            try
            {
                await OnAssetEquippedAsync(asset);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AssetController] an exception was thrown when trying to equip the asset {asset.Id}.\n{exception}");
            }
        }

        private async UniTask UnequipResolvedAssetAsync(TAsset asset)
        {
            try
            {
                await OnAssetUnequippedAsync(asset);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AssetController] an exception was thrown when trying to unequip the asset {asset.Id}.\n{exception}");
            }
        }

        // same as ReleaseUnequippedAssets but it doesn't check if the class is disposed or if we allow automatic release
        private void InternalReleaseUnequippedAssets()
        {
            // iterate over all currently cached asset IDs and release those that are not currently equipped or resolved
            string[] cachedAssetIds = _assetReferencesCache.GetCachedIds();
            foreach (string assetId in cachedAssetIds)
            {
                if (!_assetsResolver.IsAssetEquipped(assetId) && !_assetsResolver.IsAssetResolved(assetId))
                {
                    _assetReferencesCache.Release(assetId);
                }
            }
        }

        private async UniTaskVoid DisposeAsync()
        {
            // unequip all assets
            _assetsResolver.UnequipAllAssetsWithoutValidation();
            await AdjustResolvedAssetsAsync();

            _assetReferencesCache.ReleaseAllReferences();
            _assetsResolver.Dispose();
        }
    }
}
