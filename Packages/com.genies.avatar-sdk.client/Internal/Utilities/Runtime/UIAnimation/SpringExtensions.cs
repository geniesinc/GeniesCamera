using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Animations
{
    /// <summary>
    /// Spring-based animation extensions for UI components
    /// Use physics instead of easing curves for more natural, organic motion
    /// </summary>
    public static class SpringExtensions
    {
        private static MonoBehaviour GetOrAddAnimationHost(Component target)
        {
            var host = target.GetComponent<AnimationHost>();
            if (host == null)
            {
                host = target.gameObject.AddComponent<AnimationHost>();
            }
            return host;
        }

        #region RectTransform Springs

        /// <summary>
        /// Spring to target anchored position
        /// </summary>
        public static Animations.SpringUIAnimator SpringAnchorPos(this RectTransform target, Vector2 endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Vector2 startPos = target.anchoredPosition;
            Vector2 velocity = Vector2.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringVector2(
                        ref startPos,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.anchoredPosition = startPos;
                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target anchored X position
        /// </summary>
        public static Animations.SpringUIAnimator SpringAnchorPosX(this RectTransform target, float endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            float startX = target.anchoredPosition.x;
            float velocity = 0f;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;

                    var state = new SpringPhysics.SpringState(startX, endValue)
                    {
                        Velocity = velocity
                    };

                    bool settled = SpringPhysics.UpdateSpring(state, springConfig, deltaTime);

                    startX = state.Position;
                    velocity = state.Velocity;

                    var pos = target.anchoredPosition;
                    pos.x = startX;
                    target.anchoredPosition = pos;

                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target anchored Y position
        /// </summary>
        public static Animations.SpringUIAnimator SpringAnchorPosY(this RectTransform target, float endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            float startY = target.anchoredPosition.y;
            float velocity = 0f;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;

                    var state = new SpringPhysics.SpringState(startY, endValue)
                    {
                        Velocity = velocity
                    };

                    bool settled = SpringPhysics.UpdateSpring(state, springConfig, deltaTime);

                    startY = state.Position;
                    velocity = state.Velocity;

                    var pos = target.anchoredPosition;
                    pos.y = startY;
                    target.anchoredPosition = pos;

                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target scale
        /// </summary>
        public static Animations.SpringUIAnimator SpringScale(this RectTransform target, Vector3 endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Vector3 startScale = target.localScale;
            Vector3 velocity = Vector3.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringVector3(
                        ref startScale,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.localScale = startScale;
                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target local position
        /// </summary>
        public static Animations.SpringUIAnimator SpringLocalPosition(this RectTransform target, Vector3 endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Vector3 startPos = target.localPosition;
            Vector3 velocity = Vector3.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringVector3(
                        ref startPos,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.localPosition = startPos;
                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target pivot Y
        /// </summary>
        public static Animations.SpringUIAnimator SpringPivotY(this RectTransform target, float endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            float startY = target.pivot.y;
            float velocity = 0f;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;

                    var state = new SpringPhysics.SpringState(startY, endValue)
                    {
                        Velocity = velocity
                    };

                    bool settled = SpringPhysics.UpdateSpring(state, springConfig, deltaTime);

                    startY = state.Position;
                    velocity = state.Velocity;

                    var pivot = target.pivot;
                    pivot.y = startY;
                    target.pivot = pivot;

                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target sizeDelta
        /// </summary>
        public static Animations.SpringUIAnimator SpringSizeDelta(this RectTransform target, Vector2 endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Vector2 startSize = target.sizeDelta;
            Vector2 velocity = Vector2.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringVector2(
                        ref startSize,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.sizeDelta = startSize;
                    return settled;
                },
                springConfig,
                false
            );
        }

        #endregion

        #region Image Springs

        /// <summary>
        /// Spring to target color
        /// </summary>
        public static Animations.SpringUIAnimator SpringColor(this Image target, Color endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Color startColor = target.color;
            Vector4 velocity = Vector4.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringColor(
                        ref startColor,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.color = startColor;
                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target alpha
        /// </summary>
        public static Animations.SpringUIAnimator SpringFade(this Image target, float endAlpha, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            float startAlpha = target.color.a;
            float velocity = 0f;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;

                    // Use spring state for single float
                    var state = new SpringPhysics.SpringState(startAlpha, endAlpha)
                    {
                        Velocity = velocity
                    };

                    bool settled = SpringPhysics.UpdateSpring(state, springConfig, deltaTime);

                    startAlpha = state.Position;
                    velocity = state.Velocity;

                    var color = target.color;
                    color.a = Mathf.Clamp01(startAlpha);
                    target.color = color;

                    return settled;
                },
                springConfig,
                false
            );
        }

        #endregion

        #region CanvasGroup Springs

        /// <summary>
        /// Spring to target alpha
        /// </summary>
        public static Animations.SpringUIAnimator SpringFade(this CanvasGroup target, float endAlpha, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            float startAlpha = target.alpha;
            float velocity = 0f;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;

                    var state = new SpringPhysics.SpringState(startAlpha, endAlpha)
                    {
                        Velocity = velocity
                    };

                    bool settled = SpringPhysics.UpdateSpring(state, springConfig, deltaTime);

                    startAlpha = state.Position;
                    velocity = state.Velocity;

                    target.alpha = Mathf.Clamp01(startAlpha);

                    return settled;
                },
                springConfig,
                false
            );
        }

        #endregion

        #region TextMeshPro Springs

        /// <summary>
        /// Spring to target color (TextMeshProUGUI)
        /// </summary>
        public static Animations.SpringUIAnimator SpringColor(this TMPro.TextMeshProUGUI target, Color endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Color startColor = target.color;
            Vector4 velocity = Vector4.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringColor(
                        ref startColor,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.color = startColor;
                    return settled;
                },
                springConfig,
                false
            );
        }

        #endregion

        #region Transform Springs

        /// <summary>
        /// Spring to target rotation
        /// </summary>
        public static Animations.SpringUIAnimator SpringRotation(this Transform target, Quaternion endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Quaternion startRotation = target.rotation;
            Vector3 angularVelocity = Vector3.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringQuaternion(
                        ref startRotation,
                        ref angularVelocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.rotation = startRotation;
                    return settled;
                },
                springConfig,
                false
            );
        }

        /// <summary>
        /// Spring to target position
        /// </summary>
        public static Animations.SpringUIAnimator SpringPosition(this Transform target, Vector3 endValue, SpringPhysics.SpringConfig? config = null)
        {
            var host = GetOrAddAnimationHost(target);
            var springConfig = config ?? SpringPhysics.Presets.Smooth;

            Vector3 startPos = target.position;
            Vector3 velocity = Vector3.zero;

            return Animations.GeniesUIAnimation.CreateSpringAnimation(
                host,
                () => {
                    float deltaTime = Time.deltaTime;
                    bool settled = SpringPhysics.UpdateSpringVector3(
                        ref startPos,
                        ref velocity,
                        endValue,
                        springConfig,
                        deltaTime
                    );
                    target.position = startPos;
                    return settled;
                },
                springConfig,
                false
            );
        }

        #endregion
    }
}

