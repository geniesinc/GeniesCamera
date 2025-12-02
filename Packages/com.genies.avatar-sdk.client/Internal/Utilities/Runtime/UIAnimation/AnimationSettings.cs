using System;
using UnityEngine;

namespace Genies.UI.Animations
{
    /// <summary>
    /// Configuration settings for animations
    /// </summary>
    public struct AnimationSettings
    {
        /// <summary>
        /// Easing function to use for the animation
        /// </summary>
        public Ease Easing;

        /// <summary>
        /// Custom animation curve (overrides Easing if provided)
        /// </summary>
        public AnimationCurve CustomCurve;

        /// <summary>
        /// Whether to use unscaled time (ignores Time.timeScale)
        /// </summary>
        public bool UseUnscaledTime;

        /// <summary>
        /// Delay before animation starts (in seconds)
        /// </summary>
        public float Delay;

        /// <summary>
        /// Whether to auto-start the animation (default: false for explicit control)
        /// </summary>
        public bool AutoStart;

        /// <summary>
        /// Callback invoked when animation completes
        /// </summary>
        public Action OnComplete;

        /// <summary>
        /// Default settings: Linear easing, no delay, auto-start enabled (for backward compatibility)
        /// </summary>
        public static AnimationSettings Default => new AnimationSettings
        {
            Easing = Ease.Linear,
            CustomCurve = null,
            UseUnscaledTime = false,
            Delay = 0f,
            AutoStart = true,  // Auto-start for backward compatibility with old API
            OnComplete = null
        };

        /// <summary>
        /// Create settings with a specific easing function
        /// </summary>
        public static AnimationSettings WithEase(Ease ease) => new AnimationSettings
        {
            Easing = ease,
            CustomCurve = null,
            UseUnscaledTime = false,
            Delay = 0f,
            AutoStart = false,
            OnComplete = null
        };

        /// <summary>
        /// Create settings with a custom animation curve
        /// </summary>
        public static AnimationSettings WithCurve(AnimationCurve curve) => new AnimationSettings
        {
            Easing = Ease.Linear,
            CustomCurve = curve,
            UseUnscaledTime = false,
            Delay = 0f,
            AutoStart = false,
            OnComplete = null
        };

        /// <summary>
        /// Builder pattern helper - set easing
        /// </summary>
        public AnimationSettings SetEasing(Ease ease)
        {
            Easing = ease;
            CustomCurve = null;
            return this;
        }

        /// <summary>
        /// Builder pattern helper - set custom curve
        /// </summary>
        public AnimationSettings SetCurve(AnimationCurve curve)
        {
            CustomCurve = curve;
            Easing = Ease.Linear;
            return this;
        }

        /// <summary>
        /// Builder pattern helper - set delay
        /// </summary>
        public AnimationSettings SetDelay(float delay)
        {
            Delay = delay;
            return this;
        }

        /// <summary>
        /// Builder pattern helper - set unscaled time
        /// </summary>
        public AnimationSettings SetUnscaledTime(bool useUnscaledTime)
        {
            UseUnscaledTime = useUnscaledTime;
            return this;
        }

        /// <summary>
        /// Builder pattern helper - set auto-start
        /// </summary>
        public AnimationSettings SetAutoStart(bool autoStart)
        {
            AutoStart = autoStart;
            return this;
        }

        /// <summary>
        /// Builder pattern helper - set completion callback
        /// </summary>
        public AnimationSettings SetOnComplete(Action onComplete)
        {
            OnComplete = onComplete;
            return this;
        }

        /// <summary>
        /// Check if settings are default/uninitialized (all fields at default values)
        /// </summary>
        public bool IsDefault()
        {
            return Easing == default(Ease) &&
                   CustomCurve == null &&
                   UseUnscaledTime == false &&
                   Delay == 0f &&
                   AutoStart == false &&
                   OnComplete == null;
        }
    }
}

