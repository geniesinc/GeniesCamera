using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Animations
{
    public static class ImageExtensions
    {
        /// <summary>
        /// Animate color with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateColor(this Image target, Color endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Color startValue = target.color;

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.color = Color.Lerp(startValue, endValue, t),
                settings
            );
        }

        public static void Terminate(this Image target)
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

