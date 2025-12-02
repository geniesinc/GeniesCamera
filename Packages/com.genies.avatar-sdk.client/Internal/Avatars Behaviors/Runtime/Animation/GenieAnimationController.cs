using Genies.ServiceManagement;
using Genies.UIFramework;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Defines the different animation modes available for avatars.
    /// </summary>
    public enum AvatarAnimationMode
    {
        /// <summary>
        /// No animation mode is active.
        /// </summary>
        None,

        /// <summary>
        /// Avatar uses idle animation controller for automatic idle animations.
        /// </summary>
        IdleAnimator,

        /// <summary>
        /// Avatar is in directed animation mode where animations are manually controlled.
        /// </summary>
        Directed
    }

    /// <summary>
    /// Controls avatar animations by managing different animation modes and animation controllers.
    /// This component handles switching between idle animations, directed animations, and custom animation clips.
    /// </summary>
    [RequireComponent(typeof(IGenie))]
    public class GenieAnimationController : MonoBehaviour
    {
        private GenieInteractionController GenieController => this.GetService<GenieInteractionController>();

        [Header("References")]
        /// <summary>
        /// The RuntimeAnimatorController used for unified idle animations.
        /// </summary>
        public RuntimeAnimatorController UnifiedIdleAnimator;

        [Header("Options")]
        /// <summary>
        /// The current animation mode being used by the avatar.
        /// </summary>
        public AvatarAnimationMode CurrentMode;

        private IGenie _genie;
        private Animator _animator;

        private void Start()
        {
            _genie = GetComponentInChildren<IGenie>();
            _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Sets the animation mode for the avatar and updates the appropriate animation controller.
        /// This method also manages the genie interaction controller based on the selected mode.
        /// </summary>
        /// <param name="mode">The animation mode to set for the avatar.</param>
        public void SetAnimationMode(AvatarAnimationMode mode)
        {
            CurrentMode = mode;
            if (GenieController != null)
            {
                GenieController.SetEnabled(mode != AvatarAnimationMode.Directed);
            }

            UpdateAnimatorForSpecies(_genie.Species);
        }

        private void UpdateAnimatorForSpecies(string species)
        {
            switch (CurrentMode)
            {
                case AvatarAnimationMode.None:
                    SetAnimationController(null);
                    break;

                case AvatarAnimationMode.IdleAnimator:
                    var controller = IdleAnimatorForSpecies(species);
                    SetAnimationController(controller);
                    break;

                case AvatarAnimationMode.Directed:
                    return;
            }
        }

        /// <summary>
        /// Loads a single animation clip for direct playback control.
        /// This method switches the avatar to Directed animation mode and creates a SingleClipPlayable for manual animation control.
        /// </summary>
        /// <param name="clip">The animation clip to load for playback.</param>
        /// <returns>A SingleClipPlayable instance that can be used to control the animation playback.</returns>
        public virtual SingleClipPlayable LoadSingleClip(AnimationClip clip)
        {
            SetAnimationMode(AvatarAnimationMode.Directed);
            return new SingleClipPlayable(this, _animator, clip);
        }

        private void SetAnimationController(RuntimeAnimatorController animatorController)
        {
            _animator.runtimeAnimatorController = animatorController;
        }

        private RuntimeAnimatorController IdleAnimatorForSpecies(string species)
        {
            switch (species)
            {
                case GenieSpecies.Unified:
                    return UnifiedIdleAnimator;
                default:
                    return UnifiedIdleAnimator;
            }
        }
    }
}
