using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.CustomWearables.View;
using Genies.Looks.Customization.UI;
using Genies.Looks.Customization.Utils.PatternCustomization;
using Genies.MegaEditor;
using Genies.Ugc;
using Genies.UIFramework;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Forward declaration for ICustomizableUgcWearable
    /// The actual implementation will be provided by the UGC package
    /// </summary>
    public abstract class ICustomizableUgcWearable
    {
        // Core Properties
        public abstract string CurrentCategory { get; set; }
        public abstract string CurrentSubcategory { get; set; }
        public abstract string WearableId { get; set; }
        public abstract WearableTemplate Template { get; set; }
        public abstract string CurrentSelectedTemplateCategory { get; set; }
        public abstract IStyleCustomizationView.CustomizationOption StyleCustomizationOption { get; set; }
        public abstract PatternCache PatternCache { get; }

        // Wearable Property
        public abstract Wearable Wearable { get; set; }

        // Index Properties
        public abstract int CurrentSplitIndex { get; set; }
        public abstract int CurrentElementIdIndex { get; set; }
        public abstract int CurrentRegionIndex { get; set; }
        public abstract string CurrentElementId { get; }

        // Events
        public abstract event Action WearableUpdated;

        // Core Methods
        public abstract void NotifyWearableUpdate();
        public abstract void Dispose();

        // Split/Region/Style Methods
        public abstract Split GetCurrentSplit();
        public abstract Region GetCurrentRegion();
        public abstract Style GetCurrentStyle();
        public abstract Pattern GetCurrentPattern();
        public abstract SplitTemplate GetCurrentSplitTemplate();
        public abstract List<RegionTemplate> GetCurrentRegionTemplates();
        public abstract RegionTemplate GetCurrentRegionTemplate();

        // Wearable Operations
        public abstract UniTask<byte[]> GetIconRenderAsByteArray();
        public abstract UniTask<string> SaveWearableAsync();
        public abstract UniTask<string> DuplicateWearableAsync(Wearable wearableOrigin, byte[] iconBytes);
        public abstract UniTask<bool> DeleteWearableAsync(string wearableId);
        public abstract void RandomizeWearableGeometry();
    }

    /// <summary>
    /// Forward declaration for UgcPatternItemPickerDataSource
    /// The actual implementation will be provided by the UGC package
    /// </summary>
    public abstract class IUgcPatternItemPickerDataSource
    {
        // Public Methods
        public abstract void SetSelectedPatternId(string patternId);
        public abstract void SetFilters(IEnumerable<string> filters);
        public abstract ItemPickerCtaConfig GetCtaConfig();
        public abstract void RemovePattern(string customPatternId);
    }

    /// <summary>
    /// Forward declaration for StyleCustomizationView
    /// The actual implementation will be provided by the UGC package
    /// </summary>
    public abstract class IStyleCustomizationView : MonoBehaviour
    {
        /// <summary>
        /// Customization options for style editing
        /// </summary>
        public enum CustomizationOption
        {
            Pattern,
            Color,
        }

        // Public Properties
        public abstract IUgcPatternItemPickerDataSource PatternSource { get; }

        // Public Methods
        public abstract void Initialize(
            Customizer customizer,
            IPatternPickerController patternController,
            IColorPickerController colorController);

        public abstract void StartCustomization();
        public abstract void StopCustomization();
        public abstract void RefreshData();
        public abstract UniTask<Pattern> ImportPatternAsync();


    }

    /// <summary>
    /// Forward declaration for StyleOptionsMenu
    /// The actual implementation will be provided by the UGC package
    /// </summary>
    public abstract class IStyleOptionsMenu
    {
        // Events
        public abstract event Action StyleApplied;
        public abstract event Action<Color> PasteColorClicked;

        // Public Methods
        public abstract void StartCustomization(
            Customizer customizer,
            GeniesButton optionsButton,
            MultiOptionPopupWidget optionsPopup,
            //StylesGallery stylesGallery,
            ToastMessage toastMessage);
        public abstract void StopCustomization();
        public abstract void OnStylesGalleryStyleSelected(Style style);
        public abstract void OnStylesGalleryCloseAndDiscardRequested(Style style);
        public abstract void OnStylesGalleryCloseAndSubmitRequested(Style style);
    }
}
