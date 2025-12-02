using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory.UIData;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;

using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    public class AnimationCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultAnimationLibraryConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _loadedData = new();

            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName,
                nameof(AnimationCustomizationController), "open poses category");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.AnimationCustomizationStarted);
            //Show the item picker and set this controller as the data source.
            ShowPrimaryPicker(this);

            //Stop the Avatar Rotation
            CurrentRealtimeLookView?.ToggleAnimationPlayBackLock(false);
            CurrentRealtimeLookView?.ToggleAvatarInteraction(false);
        }

        public override void StopCustomization()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.AnimationCustomizationStopped);
            //Hide the item picker
            HidePrimaryPicker();

            //Activate the Avatar Rotation
            CurrentRealtimeLookView?.ToggleAnimationPlayBackLock(true);
            CurrentRealtimeLookView?.ToggleAvatarInteraction(true);
        }

        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            //When an undo/redo happens we want to refresh the current selection
            RefreshPrimaryPickerSelection();
        }


        /// <summary>
        /// Return which outfit asset index is currently selected
        /// </summary>
        /// <returns></returns>
        public int GetCurrentSelectedIndex()
        {
            return -1;
        }

        /// <summary>
        /// Get how many outfit assets are we going to show/>
        /// </summary>
        /// <returns></returns>
        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return await LoadMoreItemsBaseAsync(cancellationToken, "animation", null);
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return await InitializeAndGetCountBaseAsync(cancellationToken, "animation");
        }

        /// <summary>
        /// Get which cell view we're showing at a specific index. In this case all
        /// items will have the same cell view.
        /// </summary>
        /// <param name="index"> Index of the item </param>
        /// <returns></returns>

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "AnimationCustomization");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.SingleNoneCTA, noneSelectedDelegate: NoneSelectedAsync);
        }

        private UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromResult(false);
            }

            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Business logic for what happens when a cell is clicked.
        /// </summary>
        /// <param name="index"> Index of the cell </param>
        /// <param name="clickedCell"> The view of the cell that was clicked </param>
        /// <param name="wasSelected"> If it was already selected </param>
        /// <param name="cancellationToken"> Cancellation token </param>
        /// <returns></returns>
        public UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            // Prevents duplicate selections
            if (wasSelected)
            {
                return UniTask.FromResult(false);
            }

            string animId = string.Empty;

            try
            {
                if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
                {
                    return UniTask.FromResult(false);
                }

                animId = data.Item.AssetId;

                // performance monitoring
                string currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,
                    animId, "animation asset id");
                _InstrumentationManager.FinishChildSpan(_previousSpan);
                _previousSpan = currentPoseSpan;

                if (cancellationToken.IsCancellationRequested)
                {
                    return UniTask.FromResult(false);
                }

                // Analytics
                //TODO:: Add analytics
                var props = new AnalyticProperties();
                props.AddProperty("name", data.Item.DisplayName);
                AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.AnimationSelected, props);
                //NOTE: The Undo/Redo system can cause this Register command
                //      to allow the user to rotate the Avatar when an animation is playing.
                //Register the command for undo/redo
                //_customizer.RegisterCommand(command);

                return UniTask.FromResult(true);
            }
            catch (Exception e)
            {
                CrashReporter.Log($"AnimationCustomization's OnItemClickedAsync with index {index} & id {animId} can't be retrieved: {e}", LogSeverity.Error);
                return UniTask.FromResult(false);
            }
        }

        /// <summary>
        /// Initialize the cell view when its visible.
        /// </summary>
        /// <param name="view"> The view to initialize </param>
        /// <param name="index"> Cell index </param>
        /// <param name="isSelected"> If its already selected </param>
        /// <param name="cancellationToken"> The cancellation token </param>
        /// <returns></returns>
        public async UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            return await InitializeCellViewBaseAsync<BasicInventoryUiData>(view, index, isSelected, cancellationToken);
        }


        public override void Dispose()
        {
            base.Dispose();
            _categorySpan = null;
            _previousSpan = null;
        }
    }
}
