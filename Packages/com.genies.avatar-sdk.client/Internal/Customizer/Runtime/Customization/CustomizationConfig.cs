using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework.Navigation;
using UnityEngine;

namespace Genies.Customization.Framework
{
    /// <summary>
    /// Configures a customization
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "CustomizationConfig", menuName = "Genies/Customizer/Customization Config")]
#endif
    [Serializable]
    public class CustomizationConfig : ScriptableObject, ICustomizationConfig
    {
        [Header("Info")]
        [Tooltip("Should be set to show a user a breadcrumb they can click back to")]
        public string breadcrumbName;

        [SerializeField]
        private BaseCustomizationController _customizationController;

        [SerializeField]
        private CustomizerViewConfig _viewConfig;

        /// <summary>
        /// This name will be shown to the user to denote where they are in the navigation stack
        /// </summary>
        public string BreadcrumbName => _customizationController?.GetCustomBreadcrumbName() ?? breadcrumbName;

        /// <summary>
        /// View configuration for <see cref="Customizer"/>
        /// </summary>
        public CustomizerViewConfig CustomizerViewConfig => _viewConfig;

        /// <summary>
        /// The <see cref="CustomizationConfig"/> model.
        /// </summary>
        public ICustomizationController CustomizationController
        {
            get => _customizationController;
            set
            {
                _customizationController = value as BaseCustomizationController;
            }
        }

        /// <summary>
		/// Used to verify if this customization can be started. Ex: Ugcw customization requires
		/// that you first pick a template or to already have an item ready to edit.
		/// </summary>
		public UniTask<bool> TryToInitialize(Customizer customizer)
		{
    		return _customizationController?.TryToInitialize(customizer) ?? UniTask.FromResult(true);
		}

        /// <summary>
        /// Start the customization flow.
        /// </summary>
        public void StartCustomization()
        {
            _customizationController?.StartCustomization();
        }

        /// <summary>
        /// Stop the customization flow and cleanup.
        /// </summary>
        public void StopCustomization()
        {
            _customizationController?.StopCustomization();
        }

        public void Dispose()
        {
            _customizationController?.Dispose();
        }

        /// <summary>
        /// Get dynamic navigation options for this controller.
        /// </summary>
        public List<NavBarNodeButtonData> GetCustomNavBarOptions()
        {
            return _customizationController?.GetCustomNavBarOptions();
        }

        /// <summary>
        /// Return false if this customization has no save action
        /// </summary>
        public bool HasSaveAction()
        {
            return _customizationController?.HasSaveAction() ?? false;
        }

        /// <summary>
        /// Return false if this customization has no create action
        /// </summary>
        public bool HasCreateAction()
        {
            return _customizationController?.HasCreateAction() ?? false;
        }

        /// <summary>
        /// Return false if this customization has no discard action
        /// </summary>
        public bool HasDiscardAction()
        {
            return _customizationController?.HasDiscardAction() ?? false;
        }

        /// <summary>
        /// Action to run on save
        /// </summary>
        public UniTask<bool> OnSaveAsync()
        {
            return _customizationController?.OnSaveAsync() ?? UniTask.FromResult(true);
        }

        // <summary>
        /// Action to run on create
        /// </summary>
        public UniTask<bool> OnCreateAsync()
        {
            return _customizationController?.OnCreateAsync() ?? UniTask.FromResult(true);
        }

        /// <summary>
        /// Action to run on discard
        /// </summary>
        public UniTask<bool> OnDiscardAsync()
        {
            return _customizationController?.OnDiscardAsync() ?? UniTask.FromResult(true);
        }

        /// <summary>
        /// Use to update your views, will be triggered from <see cref="Customizer"/>
        /// </summary>
        public void OnUndoRedo()
        {
            _customizationController?.OnUndoRedo();
        }

        /// <summary>
        /// Handle submit request, this is different than saving as it involves applying pending
        /// changes without a need for confirmation.
        /// </summary>
        public UniTask<bool> OnSubmit()
        {
            return _customizationController?.OnSubmit() ?? UniTask.FromResult(true);
        }

        /// <summary>
        /// Resets all local changes
        /// </summary>
        public void OnResetAllChanges()
        {
            _customizationController?.OnResetAllChanges();
        }

        /// <summary>
        /// Returns true if the customization has a notification
        /// </summary>
        public bool HasNotification()
        {
            return _customizationController?.HasNotification() ?? false;
        }

        /// <summary>
        /// Returns a custom name for the customization
        /// </summary>
        /// <returns></returns>
        public string GetCustomBreadcrumbName()
        {
            return _customizationController?.GetCustomBreadcrumbName();
        }

    }
}
