using System;
using Genies.Customization.Framework.Actions;
using Genies.Customization.Framework.Navigation;
using Toolbox.Core;
using UnityEngine;

namespace Genies.Customization.Framework
{
    [Serializable]
    public class CustomizerViewConfig
    {
        [Title("Navigation Bar Config")]
        public string navBarButtonName;

        [AssetPreview()]
        public Sprite icon;

        public NavBarNodeButton customNodePrefab;

        [Tooltip("If set, the nav bar will show a create item option")]
        public bool showNavBarCreateNewItemCta;

        [Tooltip("If set, the global nav bar will show under customizer")]
        public bool showGlobalNavBar = true;

        [Title("Customizer Views Config")]
        [EnumToggles]
        [Space]
        public CustomizerViewFlags customizerViewFlags = CustomizerViewFlags.ActionBar | CustomizerViewFlags.NavBar;

        private bool HasCustomizationEditor => customizerViewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor);

        [ShowIf(nameof(HasCustomizationEditor), true)]
        public float customizationEditorHeight = 112f;

        public bool hasCustomBackgroundColor;

        [ShowIf(nameof(hasCustomBackgroundColor), true)]
        public Color backgroundColor = Color.black;

        public bool showHeaderText = false;

        [ShowIf(nameof(showHeaderText), true)]
        public string headerText;

        private bool HasActionBarFlags => customizerViewFlags.HasFlagFast(CustomizerViewFlags.ActionBar);

        [Title("Action Bar Config")]
        [EnumToggles]
        [ShowIf(nameof(HasActionBarFlags), true)]
        public ActionBarFlags actionBarFlags = ActionBarFlags.Save |
                                               ActionBarFlags.Share |
                                               ActionBarFlags.Create |
                                               ActionBarFlags.Exit |
                                               ActionBarFlags.Redo |
                                               ActionBarFlags.Undo;


        public CustomizerViewConfig Clone()
        {
            var clone = new CustomizerViewConfig
            {
                navBarButtonName = this.navBarButtonName,
                icon = this.icon, // Sprite reference is copied
                customNodePrefab = this.customNodePrefab, // Prefab reference is copied
                showNavBarCreateNewItemCta = this.showNavBarCreateNewItemCta,
                showGlobalNavBar = this.showGlobalNavBar,
                customizerViewFlags = this.customizerViewFlags,
                customizationEditorHeight = this.customizationEditorHeight,
                hasCustomBackgroundColor = this.hasCustomBackgroundColor,
                backgroundColor = this.backgroundColor,
                showHeaderText = this.showHeaderText,
                headerText = this.headerText,
                actionBarFlags = this.actionBarFlags
            };

            return clone;
        }
    }
}
