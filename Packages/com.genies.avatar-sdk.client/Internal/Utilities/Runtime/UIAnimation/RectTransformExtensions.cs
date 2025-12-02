using UnityEngine;

namespace Genies.UI.Animations
{
    public static class RectTransformExtensions
    {
        /// <summary>
        /// Animate anchored position with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateAnchorPos(this RectTransform target, Vector2 endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector2 startValue = target.anchoredPosition;

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.anchoredPosition = Vector2.Lerp(startValue, endValue, t),
                settings
            );
        }

        /// <summary>
        /// Animate anchored position X with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateAnchorPosX(this RectTransform target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector2 startValue = target.anchoredPosition;
            Vector2 endPos = new Vector2(endValue, startValue.y);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.anchoredPosition = Vector2.Lerp(startValue, endPos, t),
                settings
            );
        }

        /// <summary>
        /// Animate anchored position Y with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateAnchorPosY(this RectTransform target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector2 startValue = target.anchoredPosition;
            Vector2 endPos = new Vector2(startValue.x, endValue);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.anchoredPosition = Vector2.Lerp(startValue, endPos, t),
                settings
            );
        }

        /// <summary>
        /// Animate size delta with configuration object
        /// </summary>
        public static Animations.UIAnimatorCore<Vector2, Vector2, VectorOptions> AnimateSizeDelta(this RectTransform target, Vector2 endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector2 startValue = target.sizeDelta;

            return Animations.GeniesUIAnimation.CreateAnimationCore<Vector2, Vector2, VectorOptions>(
                host,
                duration,
                t => target.sizeDelta = Vector2.Lerp(startValue, endValue, t),
                settings
            );
        }

        /// <summary>
        /// Animate scale with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateScale(this RectTransform target, Vector3 endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector3 startValue = target.localScale;

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.localScale = Vector3.Lerp(startValue, endValue, t),
                settings
            );
        }

        /// <summary>
        /// Animate scale X with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateScaleX(this RectTransform target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector3 startValue = target.localScale;
            Vector3 endScale = new Vector3(endValue, startValue.y, startValue.z);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.localScale = Vector3.Lerp(startValue, endScale, t),
                settings
            );
        }

        /// <summary>
        /// Animate local move with configuration object
        /// </summary>
        public static Animations.UIAnimatorCore<Vector3, Vector3, VectorOptions> AnimateLocalMove(this RectTransform target, Vector3 endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector3 startValue = target.localPosition;

            return Animations.GeniesUIAnimation.CreateAnimationCore<Vector3, Vector3, VectorOptions>(
                host,
                duration,
                t => target.localPosition = Vector3.Lerp(startValue, endValue, t),
                settings
            );
        }

        /// <summary>
        /// Animate local move X with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateLocalMoveX(this RectTransform target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector3 startValue = target.localPosition;
            Vector3 endPos = new Vector3(endValue, startValue.y, startValue.z);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.localPosition = Vector3.Lerp(startValue, endPos, t),
                settings
            );
        }

        /// <summary>
        /// Animate pivot Y with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimatePivotY(this RectTransform target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }
            
            var host = GetOrAddAnimationHost(target);
            Vector2 startValue = target.pivot;
            Vector2 endPivot = new Vector2(startValue.x, endValue);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.pivot = Vector2.Lerp(startValue, endPivot, t),
                settings
            );
        }

        public static void Terminate(this RectTransform target)
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

    // Dummy type for compatibility
    public class VectorOptions { }
}

