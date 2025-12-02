using Cysharp.Threading.Tasks;
using Genies.Looks.Models;
using Genies.Avatars.Behaviors;
using Genies.Looks.Core.Data;
using UnityEngine;

namespace Genies.Looks.Core
{
    /// <summary>
    /// Extended interface for look views that support real-time interaction and animation control.
    /// This interface provides additional functionality for runtime manipulation of avatar looks including animation control and interaction management.
    /// </summary>
    public interface IRealtimeLookView : ILookView
    {
        /// <summary>
        /// Gets or sets the avatar controller responsible for managing avatar behavior and interactions.
        /// </summary>
        public IAvatarController AvatarController { get; set; }
        
        /// <summary>
        /// Gets or sets the GameObject that represents the look view in the scene.
        /// </summary>
        public GameObject LooksViewObject { get; set; }

        /// <summary>
        /// Initializes the real-time look view with the specified look data and dependencies.
        /// This prepares the view for real-time interaction and animation playback.
        /// </summary>
        /// <param name="look">The look data to initialize with.</param>
        /// <param name="dependencies">Required dependencies for look functionality.</param>
        /// <returns>A task that completes when initialization is finished.</returns>
        public UniTask Initialize(LookData look, LooksDependencies dependencies);

        /// <summary>
        /// Toggles the animation playback lock state.
        /// When locked, animations cannot be changed or interrupted by user interaction.
        /// </summary>
        /// <param name="value">True to lock animation playback; false to unlock.</param>
        public void ToggleAnimationPlayBackLock(bool value);
        
        /// <summary>
        /// Toggles avatar interaction capabilities on or off.
        /// This controls whether users can interact with the avatar in real-time.
        /// </summary>
        /// <param name="status">True to enable avatar interaction; false to disable.</param>
        public void ToggleAvatarInteraction(bool status)
        {
            AvatarController.ToggleInteraction(status);
        }
    }
}
