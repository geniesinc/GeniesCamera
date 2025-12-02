using UnityEngine;
using TMPro;

namespace Genies.UI.Animations
{
    /// <summary>
    /// Extension methods for TextMeshProUGUI components
    /// </summary>
    public static class TextMeshProExtensions
    {
        /// <summary>
        /// Animate color with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimationColor(this TextMeshProUGUI target, Color endValue, float duration, AnimationSettings settings = default)
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

        /// <summary>
        /// Animate fade (alpha) with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimationFade(this TextMeshProUGUI target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }

            var host = GetOrAddAnimationHost(target);
            Color startColor = target.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, endValue);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.color = Color.Lerp(startColor, endColor, t),
                settings
            );
        }

        public static void Terminate(this TextMeshProUGUI target)
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

