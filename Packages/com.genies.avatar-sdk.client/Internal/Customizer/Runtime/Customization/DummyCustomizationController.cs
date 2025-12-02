using System;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using UnityEngine;

namespace Genies.Customization.Framework
{
    /// <summary>
    /// A dummy customization controller that inherits from BaseCustomizationController.
    /// This class serves as a simple example or placeholder implementation.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DummyCustomizationController", menuName = "Genies/Customizer/Controllers/Dummy Customization Controller")]
#endif
    [Serializable]
    public class DummyCustomizationController : BaseCustomizationController
    {
        /// <summary>
        /// Determines if this controller can be initialized and opened.
        /// </summary>
        /// <param name="customizer">The customizer instance</param>
        /// <returns>True if the controller can be initialized, false otherwise</returns>
        public override async UniTask<bool> TryToInitialize(Customizer customizer)
        {
            // Simple dummy implementation - always returns true
            await UniTask.CompletedTask;
            return true;
        }

        /// <summary>
        /// Starts the customization process.
        /// This method is called when the controller is opened.
        /// </summary>
        public override void StartCustomization()
        {
#if GENIES_INTERNAL
            Debug.Log("DummyCustomizationController: StartCustomization called");
#endif
        }

        /// <summary>
        /// Stops the customization process.
        /// This method is called when the controller is closed.
        /// </summary>
        public override void StopCustomization()
        {
#if GENIES_INTERNAL
            Debug.Log("DummyCustomizationController: StopCustomization called");
#endif
        }

        /// <summary>
        /// Disposes of the controller and cleans up resources.
        /// </summary>
        public override void Dispose()
        {
#if GENIES_INTERNAL
            Debug.Log("DummyCustomizationController: Dispose called");
#endif
        }
    }
}
