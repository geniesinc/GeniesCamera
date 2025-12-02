using System;
using Genies.CameraSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Animations.Model
{
    /// <summary>
    /// Components for an <see cref="AnimatorSwitcher"/>
    /// </summary>
    [Serializable]
    public class AnimatorSwitcherComponents
    {
        /// <summary>
        /// Triggers when animation loop has started.
        /// </summary>
        public Action OnAnimationLoopStarted;

        [Tooltip("The default animator controller in the Composer app.")]
        [FormerlySerializedAs("defaultAnimatorController")]
        [SerializeField] private RuntimeAnimatorController _defaultAnimatorController;

        [Tooltip("The runtime animator controller to override the default one for the scene creator.")]
        [FormerlySerializedAs("overrideAnimatorController")]
        [SerializeField] private RuntimeAnimatorController _overrideAnimatorController;

        [Tooltip("The runtime animator controller to override the default one for image placement.")]
        [SerializeField] private RuntimeAnimatorController _imagePlacementIdleAnimatorController;

        [Tooltip("The Animated Camera state that tells if the proxy camera is in use.")]
        [SerializeField] private AnimatedCameraState _animatedCameraState;

        /// <summary>
        /// The <see cref="Animator"/> on the Genies UMA avatar.
        /// </summary>
        public Animator UmaAnimator { get; set; }

        /// <summary>
        /// Gets the default animator controller in the Composer app.
        /// </summary>
        public RuntimeAnimatorController DefaultAnimatorController => _defaultAnimatorController;

        /// <summary>
        /// Gets the runtime animator controller to override the default one for the scene creator.
        /// </summary>
        public RuntimeAnimatorController OverrideAnimatorController => _overrideAnimatorController;

        /// <summary>
        /// Gets the runtime animator controller to override the default one for image placement.
        /// </summary>
        public RuntimeAnimatorController ImagePlacementIdleAnimatorController => _imagePlacementIdleAnimatorController;

        /// <summary>
        /// The Animated Camera state that tells if the proxy camera is in use.
        /// </summary>
        public AnimatedCameraState AnimatedCameraState => _animatedCameraState;
    }
}
