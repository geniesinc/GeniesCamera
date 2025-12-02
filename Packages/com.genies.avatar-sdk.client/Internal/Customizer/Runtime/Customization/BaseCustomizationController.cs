using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.Customization.Framework.ItemPicker;
using Genies.Customization.Framework.Navigation;
using UnityEngine;

namespace Genies.Customization.Framework
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "CustomizationController", menuName = "Genies/Customizer/Customization Controller")]
#endif
    [Serializable]
    public abstract class BaseCustomizationController : ScriptableObject, ICustomizationController
    {
        /// <summary>
        /// Added layout override support for Genies Party
        /// </summary>
        [SerializeField]
        protected bool useCustomLayoutConfig;
        [SerializeField]
        protected ItemPickerLayoutConfig customLayoutConfig;

        [Header("Info")]
        [Tooltip("Should be set to show a user a breadcrumb they can click back to")]
        public string _breadcrumbName;

        [SerializeField]
        private CustomizerViewConfig _viewConfig;

        /// <summary>
        /// Reference to the customizer instance, set during initialization
        /// </summary>
        protected Customizer _customizer;

        /// <summary>
        /// Default cell view prefab to use when none is specified by child classes
        /// </summary>
        [SerializeField]
        protected ItemPickerCellView _defaultCellView;

        /// <summary>
        /// Default cell size for item picker cells
        /// </summary>
        [SerializeField]
        protected Vector2 _defaultCellSize = new Vector2(88, 96);

        /// <summary>
        /// This name will be shown to the user to denote where they are in the navigation stack
        /// </summary>
        public string BreadcrumbName
        {
            get => GetCustomBreadcrumbName() ?? _breadcrumbName;
            set => _breadcrumbName = value;
        }

        protected List<string> _ids = new();
        protected Dictionary<int, object> _loadedData;
        protected IUIProvider _uiProvider;

        /// <summary>
        /// Sets the UI provider that this class will use.
        /// Uses IInventoryUIDataProvider interface instead of dynamic types for IL2CPP/iOS compatibility.
        /// </summary>
        /// <param name="provider">The provider responsible for providing UI to the controller</param>
        protected void SetUIProvider(IUIProvider provider)
        {
            _uiProvider = provider;
        }

        /// <summary>
        /// Gets the UI provider as IInventoryUIDataProvider interface.
        /// Returns null if provider is not set.
        /// </summary>
        /// <returns>The UI provider, or null if not set</returns>
        protected IUIProvider GetUIProvider()
        {
            return _uiProvider;
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


        /// <summary>
        /// View configuration for <see cref="Customizer"/>
        /// </summary>
        public CustomizerViewConfig CustomizerViewConfig
        {
            get => _viewConfig;
            set => _viewConfig = value;
        }

        /// <summary>
        /// Used to verify if this controller can be opened. Ex: Going into UGCW creation controller requires that you first pick
        /// a template or to already have an item ready to edit.
        /// </summary>
        public abstract UniTask<bool> TryToInitialize(Customizer customizer);

        /// <summary>
        /// Initialize and open the controller. Return true if the controller did open correctly.
        /// </summary>
        public abstract void StartCustomization();

        /// <summary>
        /// Close the controller and dispose
        /// </summary>
        public abstract void StopCustomization();

        public virtual void Dispose()
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

        /// <summary>
        /// Use to update your views, will be triggered from <see cref="Customizer"/>
        /// </summary>
        public virtual void OnUndoRedo()
        {

        }

        public virtual bool ItemSelectedIsValidForProcessCTA()
        {
            return true;
        }

        /// <summary>
        /// Handle submit request, this is different than saving as it involves applying pending
        /// changes without a need for confirmation.
        /// </summary>
        public virtual UniTask<bool> OnSubmit()
        {
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Resets all local changes
        /// </summary>
        public virtual void OnResetAllChanges()
        {
        }

        /// <summary>
        /// Returns true if the controller has a notification
        /// </summary>
        public virtual bool HasNotification()
        {
            return false;
        }

        public virtual string GetCustomBreadcrumbName()
        {
            return null;
        }

        /// <summary>
        /// Returns custom nav bar options
        /// </summary>
        /// <returns></returns>
        public virtual List<NavBarNodeButtonData> GetCustomNavBarOptions()
        {
            return null;
        }

        /// <summary>
        /// Return false if this controller has no save action
        /// </summary>
        public virtual bool HasSaveAction()
        {
            return false;
        }

        /// <summary>
        /// Return false if this controller has no create action
        /// </summary>
        public virtual bool HasCreateAction()
        {
            return false;
        }

        /// <summary>
        /// Return false if this controller has no discard action
        /// </summary>
        public virtual bool HasDiscardAction()
        {
            return false;
        }

        /// <summary>
        /// Action to run on save
        /// </summary>
        public virtual UniTask<bool> OnSaveAsync()
        {
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Action to run on create
        /// </summary>
        public virtual UniTask<bool> OnCreateAsync()
        {
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Action to run on discard
        /// </summary>
        public virtual UniTask<bool> OnDiscardAsync()
        {
            return UniTask.FromResult(true);
        }

        #region Item Picker Helpers

        protected void ShowPrimaryPicker(IItemPickerDataSource source)
        {
            if (_customizer != null &&
                _customizer.View != null &&
                _customizer.View.PrimaryItemPicker != null &&
                source != null)
            {
                _customizer.View.PrimaryItemPicker.Show(source).Forget();
            }
        }

        protected void HidePrimaryPicker()
        {
            if (_customizer != null &&
                _customizer.View != null &&
                _customizer.View.PrimaryItemPicker != null)
            {
                _customizer.View.PrimaryItemPicker.Hide();
            }
        }

        protected void ShowSecondaryPicker(IItemPickerDataSource source)
        {
            if (_customizer != null &&
                _customizer.View != null &&
                _customizer.View.SecondaryItemPicker != null
                && source != null)
            {
                _customizer.View.SecondaryItemPicker.Show(source).Forget();
            }
        }

        protected void HideSecondaryPicker()
        {
            if (_customizer != null &&
                _customizer.View != null &&
                _customizer.View.SecondaryItemPicker != null)
            {
                _customizer.View.SecondaryItemPicker.Hide();
            }
        }

        protected void RefreshPrimaryPickerSelection()
        {
            if (_customizer != null &&
                _customizer.View != null &&
                _customizer.View.PrimaryItemPicker != null)
            {
                _customizer.View.PrimaryItemPicker.RefreshSelection().Forget();
            }
        }

        protected void RefreshSecondaryPickerSelection()
        {
            if (_customizer != null &&
                _customizer.View != null &&
                _customizer.View.SecondaryItemPicker != null)
            {
                _customizer.View.SecondaryItemPicker.RefreshSelection().Forget();
            }
        }

        #endregion

        #region Layout and Cell Management

        /// <summary>
        /// Returns the layout configuration for the item picker.
        /// Checks for custom layout config first, then returns default.
        /// </summary>
        public virtual ItemPickerLayoutConfig GetLayoutConfig()
        {
            if (useCustomLayoutConfig)
            {
                return customLayoutConfig;
            }

            return new ItemPickerLayoutConfig()
            {
                horizontalOrVerticalLayoutConfig = new HorizontalOrVerticalLayoutConfig()
                {
                    padding = new RectOffset(16, 16, 16, 16),
                    spacing = 8
                },
                gridLayoutConfig = new GridLayoutConfig()
                {
                    cellSize = new Vector2(88, 96),
                    columnCount = 4,
                    padding = new RectOffset(16, 16, 24, 8),
                    spacing = new Vector2(16, 16)
                }
            };
        }

        public virtual ItemPickerCellView GetCellPrefab(int index)
        {
            return _defaultCellView;
        }

        public virtual Vector2 GetCellSize(int index)
        {
            return _defaultCellSize;
        }

        public virtual void DisposeCellViewAsync(ItemPickerCellView view, int index)
        {
            if (view != null)
            {
                view.Dispose();
            }
        }

        #endregion

    }
}
