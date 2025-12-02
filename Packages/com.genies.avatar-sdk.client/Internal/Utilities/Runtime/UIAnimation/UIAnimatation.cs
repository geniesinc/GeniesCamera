using System;
using UnityEngine;

namespace Genies.UI.Animations
{
    /// <summary>
    /// UIAnimation - Main animation control class (compatibility wrapper)
    /// </summary>
    public static class UIAnimatation
    {
        public static bool IsAnimating(Transform target)
        {
            return Animations.GeniesUIAnimation.IsAnimating(target);
        }

        public static Animations.UIAnimator To(Func<float> getter, Action<float> setter, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            return Animations.AnimateVirtual.Float(getter(), endValue, duration, setter, settings);
        }

        public static Animations.UIAnimator To(Func<Vector2> getter, Action<Vector2> setter, Vector2 endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            return Animations.AnimateVirtual.Vector2(getter(), endValue, duration, setter, settings);
        }

        /// <summary>
        /// Create a new AnimationGroup for chaining animations
        /// </summary>
        public static Animations.AnimationGroup CreateGroup()
        {
            return Animations.GeniesUIAnimation.CreateGroup(null);
        }

        /// <summary>
        /// Sequence() - Alias for CreateGroup() for compatibility
        /// </summary>
        public static Animations.Sequence Sequence()
        {
            return (Animations.Sequence)CreateGroup();
        }
    }
}

