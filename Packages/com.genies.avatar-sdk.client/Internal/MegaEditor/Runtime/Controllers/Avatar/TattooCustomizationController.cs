using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Avatars.Behaviors;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using Genies.Utilities.Internal;
using GnWrappers;
using Toolbox.Core;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Handles customizing the avatar tattoos
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TattooCustomizationController", menuName = "Genies/Customizer/Controllers/Tattoo Customization Controller")]
#endif
    public class TattooCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        [SerializeField]
        [Preset(nameof(_allAreas))]
        private string _bodyArea;

        /// <summary>
        /// This is an editor only value, using <see cref="PresetAttribute"/> to show which options are
        /// available for tattoo areas. <see cref="_bodyArea"/>
        /// </summary>
        private IReadOnlyList<string> _allAreas = UnifiedTattooSlot.All;

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultImageLibraryConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _loadedData = new();
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName,
                nameof(TattooCustomizationController), "open tattoo category");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.TattooCustomizationStarted);

            //Aim the camera at the body area
            ActivateCamera();

            //Show the item picker
            ShowPrimaryPicker(this);
        }

        public override void StopCustomization()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.TattooCustomizationStopped);
            ResetCamera();
            HidePrimaryPicker();
        }


        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            RefreshPrimaryPickerSelection();
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.SingleNoneCTA, noneSelectedDelegate: NoneSelectedAsync);
        }

        private async UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            var props = new AnalyticProperties();
            props.AddProperty("BodyArea", _bodyArea);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoTattooSelected, props);
            var unequipCmd = new UnequipNativeAvatarTattooCommand(GetTattooSlot(_bodyArea), CurrentCustomizableAvatar);
            await unequipCmd.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _customizer.RegisterCommand(unequipCmd);
            return true;
        }

        /// <summary>
        /// Get the index of the equipped tattoo id on the <see cref="_bodyArea"/>
        /// </summary>
        /// <returns></returns>
        public int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(id => CurrentCustomizableAvatar.IsTattooEquipped(GetTattooSlot(_bodyArea), id));
        }

        /// <summary>
        /// Get the count of all available tattoo options
        /// </summary>
        /// <returns></returns>
        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return await LoadMoreItemsBaseAsync(cancellationToken, "tattoo", null);
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return await InitializeAndGetCountBaseAsync(cancellationToken, "tattoo");
        }


        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "TattooCustomization");
        }

        /// <summary>
        /// Business logic for what happens when a cell is clicked.
        /// </summary>
        /// <param name="index"> Index of the cell </param>
        /// <param name="clickedCell"> The view of the cell that was clicked </param>
        /// <param name="wasSelected"> If it was already selected </param>
        /// <param name="cancellationToken"> Cancellation token </param>
        /// <returns></returns>
        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            //Get loaded data
            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return false;
            }

            // performance monitoring
            string currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,data.Item.AssetId,$"{_bodyArea} tattoo asset id");
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _previousSpan = currentPoseSpan;

            //Go to editing if needed (tattoos don't have that currently but maybe in the future.)
            if (wasSelected && data.Item.IsEditable)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            //Equip tattoo command
            var command = new EquipNativeAvatarTattooCommand(data.Item.AssetId, GetTattooSlot(_bodyArea), CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Dispatch analytics events
            var props = new AnalyticProperties();
            props.AddProperty("name",   data.Item.AssetId);
            props.AddProperty("action", "EquipItem");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.TattooPresetClickEvent, props);

            _customizer.RegisterCommand(command);

            return true;
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

        private static MegaSkinTattooSlot GetTattooSlot(string bodyArea)
        {
            switch (bodyArea)
            {
                case UnifiedTattooSlot.LeftTopForearm:        return MegaSkinTattooSlot.LeftTopForearm;
                case UnifiedTattooSlot.LeftTopOuterArm:       return MegaSkinTattooSlot.LeftTopOuterArm;
                case UnifiedTattooSlot.RightSideThigh:        return MegaSkinTattooSlot.RightSideThigh;
                case UnifiedTattooSlot.RightSideAboveTheKnee: return MegaSkinTattooSlot.RightSideAboveTheKnee;
                case UnifiedTattooSlot.LeftSideCalf:          return MegaSkinTattooSlot.LeftSideCalf;
                case UnifiedTattooSlot.LeftSideBelowKnee:     return MegaSkinTattooSlot.LeftSideBelowKnee;
                case UnifiedTattooSlot.LowerBack:             return MegaSkinTattooSlot.LowerBack;
                case UnifiedTattooSlot.LowerStomach:          return MegaSkinTattooSlot.LowerStomach;
                default: throw new ArgumentException($"Invalid body area: {bodyArea}");
            }
        }
    }
}
