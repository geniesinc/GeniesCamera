using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework.Navigation;

// ReSharper disable once CheckNamespace
namespace Genies.Customization.Framework
{
    /// <summary>
    /// The model for the <see cref="CustomizationConfig"/>
    /// </summary>
    public interface ICustomizationController
    {
        /// <summary>
        /// Used to verify if this controller can be opened. Ex: Going into UGCW creation controller requires that you first pick
        /// a template or to already have an item ready to edit.
        /// </summary>
        UniTask<bool> TryToInitialize(Customizer customizer);

        /// <summary>
        /// Initialize and open the controller. Return true if the controller did open correctly.
        /// </summary>
        void StartCustomization();

        /// <summary>
        /// Close the controller
        /// </summary>
        void StopCustomization();

        /// <summary>
        /// Dispose any held resources
        /// </summary>
        void Dispose();

        /// <summary>
        /// Get dynamic navigation options for this controller.
        /// </summary>
        List<NavBarNodeButtonData> GetCustomNavBarOptions();

        /// <summary>
        /// Return false if this controller has no save action
        /// </summary>
        public bool HasSaveAction();

        /// <summary>
        /// Return false if this controller has no create action
        /// </summary>
        public bool HasCreateAction();

        /// <summary>
        /// Return false if this controller has no discard action
        /// </summary>
        public bool HasDiscardAction();

        /// <summary>
        /// Action to run on save
        /// </summary>
        UniTask<bool> OnSaveAsync();

        /// <summary>
        /// Action to run on create
        /// </summary>
        UniTask<bool> OnCreateAsync();

        /// <summary>
        /// Action to run on discard
        /// </summary>
        UniTask<bool> OnDiscardAsync();

        /// <summary>
        /// Use to update your views, will be triggered from <see cref="Customizer"/>
        /// </summary>
        void OnUndoRedo();

        /// <summary>
        /// Handle submit request, this is different than saving as it involves applying pending
        /// changes without a need for confirmation.
        /// </summary>
        UniTask<bool> OnSubmit();

        /// <summary>
        /// Resets all local changes
        /// </summary>
        void OnResetAllChanges();

        /// <summary>
        /// Returns true if the controller has a notification
        /// </summary>
        bool HasNotification();

        /// <summary>
        /// Returns a custom name for the controller
        /// </summary>
        /// <returns></returns>
        string GetCustomBreadcrumbName();

        /// <summary>
        /// This name will be shown to the user to denote where they are in the navigation stack
        /// </summary>
        public string BreadcrumbName { get; }

        /// <summary>
        /// The view config of the <see cref="Customizer"/>.
        /// </summary>
        public CustomizerViewConfig CustomizerViewConfig { get; }
        
 }
}
