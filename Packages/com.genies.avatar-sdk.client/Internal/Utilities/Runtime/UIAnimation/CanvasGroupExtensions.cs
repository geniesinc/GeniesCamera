using UnityEngine;

namespace Genies.UI.Animations
{
    public static class CanvasGroupExtensions
    {
        /// <summary>
        /// Animate fade (alpha) with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimationFade(this CanvasGroup target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            float startValue = target.alpha;

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.alpha = Mathf.Lerp(startValue, endValue, t),
                settings
            );
        }

        public static void Terminate(this CanvasGroup target)
        {
            var host = target.GetComponent<AnimationHost>();
            if (host != null)
            {
                Animations.GeniesUIAnimation.TerminateAnimations(host);
            }
        }

        private static MonoBehaviour GetOrAddAnimationHost(Component target)
        {
            var host = target.GetComponent<AnimationHost>();
            if (host == null)
            {
                host = target.gameObject.AddComponent<AnimationHost>();
            }
            return host;
        }
    }
}

