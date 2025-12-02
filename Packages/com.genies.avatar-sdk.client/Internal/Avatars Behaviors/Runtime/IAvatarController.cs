using Cysharp.Threading.Tasks;
using Genies.CameraSystem.Focusable;
using Genies.Naf;
using Genies.ServiceManagement;
using Genies.UIFramework;
using UnityEngine;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Adds a layer of abstraction to the NativeUnifiedGenieController, providing a simplified interface for avatar control and interaction.
    /// This interface defines the contract for managing avatar instances including animation, camera handling, and lifecycle management.
    /// </summary>
    public interface IAvatarController
    {
        /// <summary>
        /// Gets the GameObject containing the genie (avatar) representation.
        /// </summary>
        public GameObject GenieGameObject { get; }

        /// <summary>
        /// Gets the parent GameObject that contains the avatar controller.
        /// </summary>
        public GameObject ParentGameObject { get; }

        /// <summary>
        /// Gets the underlying NativeUnifiedGenieController that handles avatar functionality.
        /// </summary>
        public NativeUnifiedGenieController Controller { get; }

        /// <summary>
        /// Gets the focusable component that allows camera targeting of this avatar.
        /// </summary>
        public IFocusable Focusable { get; }

        /// <summary>
        /// Gets or sets the camera used for animating and viewing the avatar.
        /// </summary>
        public Camera AnimatedCamera { get; set; }

        /// <summary>
        /// Gets or sets the Animator component responsible for avatar animations.
        /// </summary>
        public Animator Animator { get; set; }

        /// <summary>
        /// Sets the avatar definition to configure the avatar's appearance and properties.
        /// </summary>
        /// <param name="definition">The JSON string containing the avatar definition.</param>
        /// <returns>A task that completes when the definition has been applied.</returns>
        public UniTask SetDefinition(string definition);

        /// <summary>
        /// Gets the current avatar definition as a JSON string.
        /// </summary>
        /// <returns>The JSON representation of the current avatar definition.</returns>
        public string GetDefinition();

        /// <summary>
        /// Disposes of the avatar controller and cleans up all associated resources.
        /// </summary>
        public void Dispose();

        /// <summary>
        /// Sets the avatar to use its default animation controller.
        /// </summary>
        public void SetDefaultAvatarAnimation();

        /// <summary>
        /// Toggles the interaction capabilities of the avatar on or off.
        /// When enabled, the avatar can respond to user interactions; when disabled, interactions are ignored.
        /// </summary>
        /// <param name="status">True to enable interaction, false to disable interaction.</param>
        public void ToggleInteraction(bool status)
        {
            var interactionController = Controller.GetService<GenieInteractionController>();

            if (interactionController == null)
            {
                return;
            }

            if (status)
            {
                interactionController.Controllable = Controller.Genie.Root;
                interactionController.SmoothReset();
            }
            else
            {
                interactionController.Reset();
                interactionController.Controllable = null;
            }
        }

    }
}
