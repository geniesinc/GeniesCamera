using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Animations
{
    public static class ScrollRectExtensions
    {
        public static Animations.UIAnimator AnimateNormalizedPos(this ScrollRect target, Vector2 endValue, float duration, bool snapping = false)
        {
            var host = GetOrAddAnimationHost(target);
            Vector2 startValue = target.normalizedPosition;

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.normalizedPosition = Vector2.Lerp(startValue, endValue, t),
                Animations.Ease.Linear,
                false
            );
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

