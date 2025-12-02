using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.UI.Animations;
using UIAnimator = Genies.UI.Animations.UIAnimator;

namespace Genies.UI.Transitions {
    /// <summary>
    /// Defines the available transition animation types for UI elements.
    /// </summary>
    public enum TransitionType {
        /// <summary>
        /// No transition animation.
        /// </summary>
        None,
        /// <summary>
        /// Slide transition moving to/from the left.
        /// </summary>
        SlideLeft,
        /// <summary>
        /// Slide transition moving to/from the right.
        /// </summary>
        SlideRight,
        /// <summary>
        /// Slide transition moving to/from above.
        /// </summary>
        SlideUp,
        /// <summary>
        /// Slide transition moving to/from below.
        /// </summary>
        SlideDown,
        /// <summary>
        /// Scale transition animation.
        /// </summary>
        Scale,
        /// <summary>
        /// Scale-in transition animation.
        /// </summary>
        ScaleIn,
        /// <summary>
        /// Scale-out transition animation.
        /// </summary>
        ScaleOut,
        /// <summary>
        /// Fade transition animation using alpha.
        /// </summary>
        Fade
    }

    /// <summary>
    /// Service for performing smooth transition animations on UI elements.
    /// Supports various transition types including slide, scale, and fade animations.
    /// </summary>
    public class TransitionService {
        /// <summary>
        /// The default duration for transition-in animations in seconds.
        /// </summary>
        public const float DefaultInDuration = .33f;

        /// <summary>
        /// The default duration for transition-out animations in seconds.
        /// </summary>
        public const float DefaultOutDuration = .33f;

        /// <summary>
        /// Performs a transition-in animation on the specified target element.
        /// </summary>
        /// <param name="target">The UI element to animate.</param>
        /// <param name="transitionType">The type of transition animation to perform.</param>
        /// <returns>A task that completes when the transition animation is finished.</returns>
        public async Task DoTransitionIn(ITransitionable target, TransitionType transitionType = TransitionType.None) {
            ResetState(target);
            switch (transitionType) {
                case TransitionType.SlideLeft:
                    await SlideInLeft(target);
                    break;
                case TransitionType.SlideRight:
                    await SlideInRight(target);
                    break;
                case TransitionType.SlideUp:
                    await SlideInUp(target);
                    break;
                case TransitionType.SlideDown:
                    await SlideInDown(target);
                    break;
                case TransitionType.Scale:
                    await ScaleInTransition(target);
                    break;
                case TransitionType.ScaleIn:
                    await ScaleInTransition(target);
                    break;
                case TransitionType.ScaleOut:
                    await FadeIn(target);
                    break;
                case TransitionType.Fade:
                    await FadeIn(target);
                    break;
                case TransitionType.None:
                    return;
                default:
                    return;
            }
        }

        /// <summary>
        /// Performs a transition-out animation on the specified target element.
        /// </summary>
        /// <param name="target">The UI element to animate.</param>
        /// <param name="transitionType">The type of transition animation to perform.</param>
        /// <returns>A task that completes when the transition animation is finished.</returns>
        public async Task DoTransitionOut(ITransitionable target, TransitionType transitionType = TransitionType.None) {
            ResetState(target);
            switch (transitionType) {
                case TransitionType.SlideLeft:
                    await SlideOutLeft(target);
                    break;
                case TransitionType.SlideRight:
                    await SlideOutRight(target);
                    break;
                case TransitionType.SlideUp:
                    await SlideOutUp(target);
                    break;
                case TransitionType.SlideDown:
                    await SlideOutDown(target);
                    break;
                case TransitionType.Scale:
                    await ScaleOutTransition(target);
                    break;
                case TransitionType.ScaleIn:
                    await FadeOut(target);
                    break;
                case TransitionType.ScaleOut:
                    await ScaleOutTransition(target);
                    break;
                case TransitionType.Fade:
                    await FadeOut(target);
                    break;
                case TransitionType.None:
                    return;
                default:
                    return;
            }
        }

        /// <summary>
        /// Restores the state after previous transition
        /// </summary>
        /// <param name="transitionable"></param>
        private void ResetState(ITransitionable transitionable) {
            var rectTransform = transitionable.RectTransform;
            var canvaseGroup = transitionable.CanvasGroup;
            rectTransform.anchoredPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            canvaseGroup.alpha = 1f;
        }

        private async Task FadeIn(ITransitionable transitionable) {
            await DoFadeTransition(transitionable, DefaultInDuration, false);
        }

        private async Task FadeOut(ITransitionable transitionable) {
            await DoFadeTransition(transitionable, DefaultOutDuration, true);
        }

        private async Task DoFadeTransition(ITransitionable transitionable, float duration, bool finishOffscreen) {
            var canvasGroup = transitionable.CanvasGroup;
            UIAnimator fadeTween = null;

            if (finishOffscreen) {
                fadeTween = canvasGroup.AnimationFade(0, duration);

            } else {
                canvasGroup.alpha = 0;
                fadeTween = canvasGroup.AnimationFade(1, duration);
            }

            await fadeTween.AsyncWaitForCompletion();
        }

        private async Task SlideInLeft(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.left, DefaultInDuration, false);
        }

        private async Task SlideOutLeft(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.left, DefaultOutDuration, true);
        }

        private async Task SlideInRight(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.right, DefaultInDuration, false);
        }

        private async Task SlideOutRight(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.right, DefaultOutDuration, true);
        }

        private async Task SlideInUp(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.up, DefaultInDuration, false);
        }

        private async Task SlideOutUp(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.up, DefaultOutDuration, true);
        }

        private async Task SlideInDown(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.down, DefaultInDuration, false);
        }

        private async Task SlideOutDown(ITransitionable transitionable) {
            await DoingSlideTransition(transitionable, Vector3.down, DefaultOutDuration, true);
        }

        private IEnumerator DoingSlideTransition(
            ITransitionable transitionable,
            Vector3 direction,
            float duration,
            bool finishOffscreen)
        {
            var rectTransform = transitionable.RectTransform;
            var canvasGroup = transitionable.CanvasGroup;

            float screenDimension =
                (Vector3.Scale(direction, new Vector3(rectTransform.rect.width, rectTransform.rect.height))).magnitude;

            float t = 0;
            if (finishOffscreen)
            {
                Vector3 fromAnchoredPos = Vector3.zero;
                Vector3 targetAnchoredPos = direction * screenDimension;

                while (t < 1f)
                {
                    if (rectTransform == null)
                    {
                        break;
                    }

                    t += Time.deltaTime / duration;
                    t = Mathf.Clamp01(t);
                    var easeTime = AnimateVirtual.EasedValue(0f, 1f, t, Ease.InSine);
                    var fadeTime = Mathf.InverseLerp(0.8f, 1f, t);


                    rectTransform.anchoredPosition = Vector3.Lerp(fromAnchoredPos, targetAnchoredPos, easeTime);
                    canvasGroup.alpha = 1f - fadeTime;

                    yield return null;
                }
            }
            else
            {
                Vector3 fromAnchoredPos = -direction * screenDimension;
                Vector3 targetAnchoredPos = Vector3.zero;

                while (t < 1f)
                {
                    if (rectTransform == null)
                    {
                        break;
                    }

                    t += Time.deltaTime / duration;
                    t = Mathf.Clamp01(t);
                    var easeTime = AnimateVirtual.EasedValue(0f, 1f, t, Ease.OutSine);
                    var fadeTime = Mathf.InverseLerp(0f, 0.2f, t);

                    rectTransform.anchoredPosition = Vector3.Lerp(fromAnchoredPos, targetAnchoredPos, easeTime);
                    canvasGroup.alpha = fadeTime;

                    yield return null;
                }
            }
        }

        private async Task ScaleInTransition(ITransitionable transitionable) {
            await DoScaleTransition(transitionable.RectTransform, Vector3.zero, Vector3.one, DefaultInDuration);
        }

        private async Task ScaleOutTransition(ITransitionable transitionable) {
            await DoScaleTransition(transitionable.RectTransform, Vector3.one, Vector3.zero, DefaultOutDuration);
        }

        private async Task DoScaleTransition(RectTransform target, Vector3 startScale, Vector3 endScale, float duration) {
            target.localScale = startScale;
            UIAnimator tween = target.AnimateScale(endScale, duration).SetEase(Ease.InQuad);
            await tween.AsyncWaitForCompletion();
            target.localScale = endScale;
        }
    }
}
