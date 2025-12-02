using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars.Behaviors;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using Genies.Utilities;
using Genies.Utilities.Internal;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Handles select the avatar face presets
    /// </summary>
    public class FacePresetCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        private string _lastSelectedFacePreset = "None";

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultAvatarBaseConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _loadedData = new();
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.FacePresetCustomizationStarted);
            //Aim the camera at the body area
            ActivateCamera();
            ShowPrimaryPicker(this);
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.FacePresetCustomizationStopped);
            //Aim the camera at the body area
            ResetCamera();
            HidePrimaryPicker();
        }

        public override void OnUndoRedo()
        {
            RefreshPrimaryPickerSelection();
        }


        public int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return await LoadMoreItemsBaseAsync(cancellationToken, "facepreset", null);
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return await InitializeAndGetCountBaseAsync(cancellationToken, "facepreset");
        }


        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "FacePresetCustomization");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.SingleNoneCTA, noneSelectedDelegate: NoneSelectedAsync);
        }

        private UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            var props = new AnalyticProperties();
            props.AddProperty("LastSelectedFacePreset", _lastSelectedFacePreset);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoFacePresetSelected, props);

            Debug.LogError("Face presets are not supported by NAF");
            return UniTask.FromResult(false);

            // var unequipCmd = new UnequipAvatarFacePresetCommand(CurrentCustomizableAvatar);
            // await unequipCmd.ExecuteAsync(cancellationToken);

            // if (cancellationToken.IsCancellationRequested)
            // {
            //     return false;
            // }
            //
            // _customizer.RegisterCommand(unequipCmd);
            // return true;
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
            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return UniTask.FromResult(false);
            }

            if (wasSelected && data.Item.IsEditable)
            {
                _customizer.GoToEditItemNode();
                return UniTask.FromResult(true);
            }

            CrashReporter.LogError("Face presets are not supported by NAF");
            return UniTask.FromResult(false);

            // var command = new EquipAvatarFacePresetCommand(data.Item.AssetId, CurrentCustomizableAvatar);
            // await command.ExecuteAsync(cancellationToken);
            // _lastSelectedFacePreset = data.Item.DisplayName;
            //
            // if (cancellationToken.IsCancellationRequested)
            // {
            //     return false;
            // }
            //
            // var props = new AnalyticProperties();
            // props.AddProperty("name", $"{data.Item.DisplayName}");
            // AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.FacePresetClickEvent, props);
            //
            // _customizer.RegisterCommand(command);
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

    }
}
