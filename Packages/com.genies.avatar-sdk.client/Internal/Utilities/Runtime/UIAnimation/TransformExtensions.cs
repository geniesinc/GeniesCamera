using UnityEngine;

namespace Genies.UI.Animations
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Animate move Y with configuration object
        /// </summary>
        public static Animations.UIAnimator AnimateMoveY(this Transform target, float endValue, float duration, AnimationSettings settings = default)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }

            var host = GetOrAddAnimationHost(target);
            Vector3 startValue = target.position;
            Vector3 endPos = new Vector3(startValue.x, endValue, startValue.z);

            return Animations.GeniesUIAnimation.CreateAnimation(
                host,
                duration,
                t => target.position = Vector3.Lerp(startValue, endPos, t),
                settings
            );
        }

        /// <summary>
        /// Animate local move with configuration object
        /// </summary>
        public static Animations.UIAnimatorCore<Vector3, Vector3, VectorOptions> AnimateLocalMove(this Transform target, Vector3 endValue, float duration, AnimationSettings settings = default)
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
        public static Animations.UIAnimator AnimateLocalMoveX(this Transform target, float endValue, float duration, AnimationSettings settings = default)
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
        /// Animate rotation with quaternion
        /// Uses centralized UIAnimationManager instead of adding components to the target
        /// </summary>
        public static Animations.UIAnimator AnimateRotateQuaternion(this Transform target, Quaternion endValue, float duration)
        {
            var manager = Animations.UIAnimationManager.Instance;
            Quaternion startValue = target.rotation;

            var animator = Animations.GeniesUIAnimation.CreateAnimation(
                manager,
                duration,
                t => target.rotation = Quaternion.Slerp(startValue, endValue, t),
                AnimationSettings.Default
            );

            return animator;
        }

        /// <summary>
        /// Animate rotation with quaternion using configuration object (optional)
        /// Uses centralized UIAnimationManager instead of adding components to the target
        /// </summary>
        public static Animations.UIAnimator AnimateRotateQuaternion(this Transform target, Quaternion endValue, float duration, AnimationSettings settings)
        {
            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }

            var manager = Animations.UIAnimationManager.Instance;
            Quaternion startValue = target.rotation;

            return Animations.GeniesUIAnimation.CreateAnimation(
                manager,
                duration,
                t => target.rotation = Quaternion.Slerp(startValue, endValue, t),
                settings
            );
        }

        /// <summary>
        /// Terminate all animations on this Transform
        /// </summary>
        public static void Terminate(this Transform target)
        {
            // Check if there's an AnimationHost component
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

