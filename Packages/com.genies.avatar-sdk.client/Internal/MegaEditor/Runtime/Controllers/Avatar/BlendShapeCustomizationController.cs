using System;
using System.Collections.Generic;
using System.Linq;
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
using Genies.PerformanceMonitoring;
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
    /// Handles customizing the avatar blendshapes (nose, lips, etc..)
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapeCustomizationController", menuName = "Genies/Customizer/Controllers/Blend Shape Customization Controller")]
#endif
    public class BlendShapeCustomizationController : InventoryCustomizationController, IItemPickerDataSource
    {
        [SerializeField] private AvatarBaseCategory _blendShapeSubcategory;

        /// <summary>
        /// Connected Chaos customization node's controller
        /// Used to reset all custom vector values when equipped preset has changed
        /// </summary>
        [SerializeField] private FaceVectorCustomizationController _chaosCustomizer;
        private string _stringSubcategory;

        private readonly Dictionary<string, AvatarBaseCategory> _subcategoryMap = new()
        {
            { "eyes", AvatarBaseCategory.Eyes },
            { "jaw", AvatarBaseCategory.Jaw },
            { "lips", AvatarBaseCategory.Lips },
            { "nose", AvatarBaseCategory.Nose },
        };

        private string _lastSelectedBlendShape = "None";

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;
        private bool _isEditable;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultAvatarBaseConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _stringSubcategory = _blendShapeSubcategory.ToString().ToLower();
            _isEditable = _blendShapeSubcategory == AvatarBaseCategory.Eyes;

            _loadedData = new();
            _ids = new();
            return UniTask.FromResult(true);
        }

        public async UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return await InitializeAndGetCountBaseAsync(cancellationToken, null, _stringSubcategory);
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName,
                nameof(BlendShapeCustomizationController), $"open face - {_stringSubcategory} category");

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BlendShapeCustomizationStarted);
            //Aim the camera at the body area
            ActivateCamera();
            ShowPrimaryPicker(this);
        }

        public override void StopCustomization()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BlendShapeCustomizationStopped);
            //Aim the camera at the body area
            ResetCamera();
            HidePrimaryPicker();
        }

        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            RefreshPrimaryPickerSelection();
        }


        public int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(id => CurrentCustomizableAvatar.IsAssetEquipped($"{id}"));
        }

        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return await LoadMoreItemsBaseAsync(cancellationToken, null, _stringSubcategory);
        }


        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "BlendShapeCustomization");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.CustomizeCTA, noneSelectedDelegate: CustomizeSelectedAsync);
        }

        private UniTask<bool> CustomizeSelectedAsync(CancellationToken cancellationToken)
        {
            if (_subcategoryMap.TryGetValue(_stringSubcategory, out var category))
            {
                CurrentDnaCustomizationViewState = category;
            }
            else
            {
                CrashReporter.LogError($"Invalid Customization Selection '{_stringSubcategory}'");
            }

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ChaosFaceCustomSelectEvent);
            _customizer.RemoveLastSelectedChildForCurrentNode();
            _customizer.GoToCreateItemNode();
            return UniTask.FromResult(true);
        }

        private async UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            var props = new AnalyticProperties();
            props.AddProperty("LastSelectedBlendShape", _lastSelectedBlendShape);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoBlendShapeSelected, props);

            var equippedId = _ids.FirstOrDefault(id => CurrentCustomizableAvatar.IsAssetEquipped($"{id}"));
            if (string.IsNullOrEmpty(equippedId))
            {
                return false;
            }

            var unequipCmd = new UnequipNativeAvatarAssetCommand($"{equippedId}", CurrentCustomizableAvatar);
            await unequipCmd.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _customizer.RegisterCommand(unequipCmd);
            return true;
        }

        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return false;
            }

            // performance monitoring
            string currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,data.Item.AssetId, $"{_stringSubcategory} asset id");
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _previousSpan = currentPoseSpan;

            if (wasSelected && _isEditable)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            if (!wasSelected && _chaosCustomizer != null)
            {
                _chaosCustomizer.ResetAllValues();
            }

            var command = new EquipNativeAvatarAssetCommand($"{data.Item.AssetId}", CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);
            _lastSelectedBlendShape = data.Item?.DisplayName;

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Fire analytics
            var props = new AnalyticProperties();
            props.AddProperty("name", $"{data.Item?.DisplayName}");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BlendShapeClickEvent, props);

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
    }
}
