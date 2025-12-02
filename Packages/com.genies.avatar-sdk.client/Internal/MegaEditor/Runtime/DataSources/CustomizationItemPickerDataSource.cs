using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.CrashReporting;
using Genies.Inventory.UIData;
using Genies.Refs;

namespace Genies.Customization.Framework.ItemPicker
{
    public abstract class CustomizationItemPickerDataSource : ScriptableItemPickerDataSource
    {
        protected Customizer _customizer { get; private set; }

        protected EditOrDeleteCustomColorController _editOrDeleteController;
        protected Dictionary<int, object> _loadedData;
        protected IUIProvider _uiProvider;

        /// <summary>
        /// Gets the UI Provider for this data source. See <see cref="UIDataProviderConfigs"/> for a list
        /// of configs that UI Providers can use
        /// </summary>
        /// <typeparam name="TAsset">The type of asset represented by the UI</typeparam>
        /// <typeparam name="TUI">The UI type that wraps specific information about the asset</typeparam>
        /// <returns>The UI Provider</returns>
        protected InventoryUIDataProvider<TAsset, TUI> GetUIProvider<TAsset, TUI>()
            where TUI : IAssetUiData
        {
            return (InventoryUIDataProvider<TAsset, TUI>)_uiProvider;
        }

        protected void SetUIProvider<TAsset, TUI>(
            InventoryUIDataProviderConfig<TAsset, TUI> config,
            IAssetsService service)
            where TUI : IAssetUiData
        {
            _uiProvider = new InventoryUIDataProvider<TAsset, TUI>(config, service);
        }

        protected bool TryGetLoadedData<TUI>(int index, out Ref<TUI> result)
        {
            if (_loadedData != null &&
                _loadedData.TryGetValue(index, out var obj) &&
                obj is Ref<TUI> typed)
            {
                result = typed;
                return true;
            }

            result = default;
            return false;
        }

        protected virtual string GetAssetTypeString()
        {
            return string.Empty;
        }

        protected virtual UniTask<List<string>> GetCustomIdsAsync(CancellationToken token)
            => UniTask.FromResult<List<string>>(null);

        protected virtual async UniTask<List<string>> GetPresetIdsAsync(CancellationToken token)
        {
            if (_uiProvider == null)
            {
                return new List<string>();
            }

            try
            {
                return await _uiProvider.GetAllAssetIds(GetAssetTypeString()).AttachExternalCancellation(token) ??
                       new List<string>();
            }
            catch (OperationCanceledException)
            {
                return new List<string>();
            }
        }

        protected virtual void OnAfterInitialized() { }

        // Called by framework
        public void Initialize(Customizer customizer)
        {
            _customizer = customizer;
            _ = OnInitialized();
        }

        /// <summary>
        /// Base initialization logic, can be overridden but usually call base.
        /// </summary>
        protected virtual UniTask OnInitialized()
        {
            SetProviderWithConfig();
            _loadedData = new();
            _editOrDeleteController = _customizer.View.EditOrDeleteController;
            OnAfterInitialized();
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Method should get the correct UI provider config and assign the
        /// UI provider with it
        /// </summary>
        protected abstract void ConfigureProvider();

        protected void SetProviderWithConfig()
        {
            if (_uiProvider != null)
            {
                return;
            }

            ConfigureProvider();

            if (_uiProvider == null)
            {
                CrashReporter.LogWarning($"{GetType().Name}: ConfigureProvider did not assign a UI provider.");
            }
        }

        /// <summary>
        /// Initializes the data source while getting the total count of items to display
        /// </summary>
        /// <param name="token">token to cancel the operation</param>
        /// <returns>The total count of items</returns>
        public override async UniTask<int> InitializeAndGetCountAsync(CancellationToken token)
        {
           SetProviderWithConfig();

           try
           {
               // Fetch ids
               var ids = await GetPresetIdsAsync(token).AttachExternalCancellation(token);

               // Fetch optional custom ids
               var customIds = await GetCustomIdsAsync(token).AttachExternalCancellation(token);

               // Combine if any custom ids exist
               if (customIds is { Count: > 0 })
               {
                   var ordered = new List<string>(customIds);
                   ordered.AddRange(ids);
                   _ids = ordered;
               }
               else
               {
                   _ids = ids;
               }
           }
           catch (OperationCanceledException)
           {
               _ids = new();
           }

           return _ids.Count;
        }

        /// <summary>
        /// Base helper method for GetDataForIndexAsync implementations.
        /// Handles common caching pattern used across data sources.
        /// </summary>
        protected virtual async UniTask<Ref<TUI>> GetDataForIndexBaseAsync<TAsset, TUI>(
            int index,
            string errorContext = null)
            where TUI : class, IAssetUiData
        {
            string id = string.Empty;
            try
            {
                // Check cache
                if (TryGetLoadedData<TUI>(index, out var data))
                {
                    return data;
                }

                // Validate index
                if (index < 0 || _ids == null || index >= _ids.Count)
                {
                    return default;
                }

                // Load from provider
                id = _ids[index];
                if (string.IsNullOrEmpty(id))
                {
                    return default;
                }

                var uiData = await GetUIProvider<TAsset, TUI>().GetDataForAssetId(id);
                var newDataRef = CreateRef.FromDependentResource(uiData);

                // Cache
                _loadedData ??= new();
                _loadedData[index] = newDataRef;

                return newDataRef;
            }
            catch (Exception e)
            {
                var context = errorContext ?? GetType().Name;
                CrashReporter.Log($"{context}'s GetDataForIndexAsync with index {index} & id {id} can't be retrieved: {e}", LogSeverity.Error);
                return CreateRef.From<TUI>(null);
            }
        }

        /// <summary>
        /// Base helper method for InitializeCellViewAsync implementations with simple GenericItemPickerCellView.
        /// Gets data and sets debugging label. Child classes should handle sprite setting for their specific data types.
        /// </summary>
        /// <returns>The data ref if successful, null otherwise</returns>
        protected virtual async UniTask<bool> InitializeCellViewBaseAsync<TAsset, TUI>(
            ItemPickerCellView view,
            int index,
            CancellationToken cancellationToken)
            where TUI : class, IAssetUiData
        {
            var dataRef = await GetDataForIndexBaseAsync<TAsset, TUI>(index);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (dataRef.Item == null)
            {
                return false;
            }

            var genericView = view as GenericItemPickerCellView;
            if (genericView != null)
            {
                if (dataRef is Ref<BasicInventoryUiData> basicDataRef)
                {
                    genericView.thumbnail.sprite = basicDataRef.Item.Thumbnail.Item;
                }

                genericView.SetDebuggingAssetLabel(dataRef.Item.AssetId);
            }

            return true;
        }

        public override void Dispose()
        {
            if (_loadedData != null)
            {
                foreach (var data in _loadedData)
                {
                    if (data.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _loadedData.Clear();
                _loadedData = null;
            }

            _uiProvider?.Dispose();
            _uiProvider = null;

            _ids?.Clear();
            _ids = null;
        }


        // Still abstract lifecycle entry points for subclasses
        public abstract void StartCustomization();
        public abstract void StopCustomization();
    }
}
