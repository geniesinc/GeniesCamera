using UnityEngine;

namespace Genies.Avatars.Behaviors 
{
    /// <summary>
    /// Provides functionality to override a single animation clip in an avatar's animator controller.
    /// This class manages the creation and manipulation of AnimatorOverrideController to replace specific animations.
    /// </summary>
    public class SingleClipOverrider
    {
        /// <summary>
        /// Gets the AnimatorOverrideController that manages the clip overrides.
        /// </summary>
        public AnimatorOverrideController OverrideController { get; }
        
        /// <summary>
        /// The current animation clip being used as an override.
        /// </summary>
        public AnimationClip AnimationClip;
        
        private IGenie _genie;
        private RuntimeAnimatorController _originalAnimatorController;
        private string _singleClipKey;

        /// <summary>
        /// Initializes a new instance of the SingleClipOverrider class with the specified parameters.
        /// </summary>
        /// <param name="animatorController">The original RuntimeAnimatorController to create an override for.</param>
        /// <param name="genie">The IGenie instance that owns the animator.</param>
        /// <param name="clip">Optional animation clip to immediately load as an override.</param>
        public SingleClipOverrider(RuntimeAnimatorController animatorController, IGenie genie, AnimationClip clip = null)
        {
            this.OverrideController = new AnimatorOverrideController();
            this._genie = genie;
            this._originalAnimatorController = animatorController;
            this._singleClipKey = animatorController.animationClips[0].name;

            OverrideController.runtimeAnimatorController = animatorController;
            LoadClip(clip);
        }

        /// <summary>
        /// Loads a new animation clip as an override for the single clip key.
        /// If a clip is provided, it replaces the original clip in the animator controller.
        /// </summary>
        /// <param name="clip">The animation clip to use as an override, or null to clear the override.</param>
        public void LoadClip(AnimationClip clip)
        {
            AnimationClip = clip;

            if(clip != null) {
                OverrideController[_singleClipKey] = clip;
                _genie.Animator.runtimeAnimatorController = OverrideController;
            }
        }

        /// <summary>
        /// Resets the animator controller to use the original RuntimeAnimatorController, removing all overrides.
        /// </summary>
        public void ResetAnimatorController()
        {
            _genie.Animator.runtimeAnimatorController = _originalAnimatorController;
        }

        /// <summary>
        /// Destroys the AnimatorOverrideController and cleans up resources.
        /// </summary>
        public void Destroy()
        {
            Object.Destroy(OverrideController);
        }
    }
}
