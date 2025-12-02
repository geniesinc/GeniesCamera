using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    /// <summary>
    /// Advanced button component with customizable animations for various interaction states.
    /// Supports enable/disable, click, selection/deselection, and long press animations with configurable curves and timings.
    /// </summary>
    public class GeniesButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Unity event triggered when the button is clicked.
        /// </summary>
        public Button.ButtonClickedEvent onClick;

        [FormerlySerializedAs("canvasGroup")] [Header("References")]
        /// <summary>
        /// The CanvasGroup component used for alpha animations.
        /// </summary>
        public CanvasGroup CanvasGroup;
        
        [FormerlySerializedAs("buttonGroupRT")] 
        /// <summary>
        /// The RectTransform component used for scale and position animations.
        /// </summary>
        public RectTransform ButtonGroupRT;

        [Header("On Enable Settings")]
        /// <summary>
        /// Whether to animate the button when it becomes enabled.
        /// </summary>
        public bool AnimateOnEnable = false;
        
        [FormerlySerializedAs("onEnableAnimTime")] 
        /// <summary>
        /// Duration of the enable animation in seconds.
        /// </summary>
        public float OnEnableAnimTime = 0.66f;
        
        [FormerlySerializedAs("onEnableScaleRange")] 
        /// <summary>
        /// Scale range for the enable animation (from X to Y).
        /// </summary>
        public Vector2 OnEnableScaleRange = new Vector2(0f, 1f);
        
        /// <summary>
        /// Animation curve controlling the scale animation when enabled.
        /// </summary>
        public AnimationCurve OnEnableScaleCurve;
        
        /// <summary>
        /// Animation curve controlling the alpha animation when enabled.
        /// </summary>
        public AnimationCurve OnEnableAlphaCurve;

        [Header("On Disable Settings")]
        /// <summary>
        /// Whether to animate the button when it becomes disabled.
        /// </summary>
        public bool AnimateOnDisable = false;
        
        [FormerlySerializedAs("onDisableAnimTime")] 
        /// <summary>
        /// Duration of the disable animation in seconds.
        /// </summary>
        public float OnDisableAnimTime = 0.66f;
        
        [FormerlySerializedAs("onDisableScaleRange")] 
        /// <summary>
        /// Scale range for the disable animation (from X to Y).
        /// </summary>
        public Vector2 OnDisableScaleRange = new Vector2(0f, 1f);
        
        /// <summary>
        /// Animation curve controlling the scale animation when disabled.
        /// </summary>
        public AnimationCurve OnDisableScaleCurve;
        
        /// <summary>
        /// Animation curve controlling the alpha animation when disabled.
        /// </summary>
        public AnimationCurve OnDisableAlphaCurve;

        [Header("On Click Settings")]
        /// <summary>
        /// Whether to animate the button when clicked.
        /// </summary>
        public bool AnimateOnClick = false;
        
        /// <summary>
        /// Whether to trigger the click event at the start of the animation rather than at the end.
        /// </summary>
        public bool RushAnimateClickEvent = false;
        
        [FormerlySerializedAs("onClickAnimTime")] 
        /// <summary>
        /// Duration of the click animation in seconds.
        /// </summary>
        public float OnClickAnimTime = 0.66f;
        
        [FormerlySerializedAs("onClickScaleRange")] 
        /// <summary>
        /// Scale range for the click animation (from X to Y).
        /// </summary>
        public Vector2 OnClickScaleRange = new Vector2(0f, 1f);
        
        /// <summary>
        /// Animation curve controlling the scale animation when clicked.
        /// </summary>
        public AnimationCurve OnClickScaleCurve;

        [Header("On Selection Settings")]
        public bool AnimateOnSelection = false;
        [FormerlySerializedAs("onSelectionAnimTime")] public float OnSelectionAnimTime = 0.66f;
        [FormerlySerializedAs("onSelectionScaleRange")] public Vector2 OnSelectionScaleRange = new Vector2(0f, 1f);
        [FormerlySerializedAs("onSelectionAlphaRange")] public Vector2 OnSelectionAlphaRange = new Vector2(0.5f, 1f);
        public AnimationCurve OnSelectionScaleCurve;
        public AnimationCurve OnSelectionAlphaCurve;

        [Header("On Deselection Settings")]
        public bool AnimateOnDeselection = false;
        [FormerlySerializedAs("onDeselectionAnimTime")] public float OnDeselectionAnimTime = 0.66f;
        [FormerlySerializedAs("onDeselectionScaleRange")] public Vector2 OnDeselectionScaleRange = new Vector2(0f, 1f);
        [FormerlySerializedAs("onDeselectionAlphaRange")] public Vector2 OnDeselectionAlphaRange = new Vector2(1f, 0.5f);
        public AnimationCurve OnDeselectionScaleCurve;
        public AnimationCurve OnDeselectionAlphaCurve;

        [FormerlySerializedAs("enableLongPress")] [Header("Long Press")]
        /// <summary>
        /// Whether long press functionality is enabled for this button.
        /// </summary>
        public bool EnableLongPress;
        
        [FormerlySerializedAs("holdDuration")] 
        /// <summary>
        /// Duration in seconds that the button must be held to trigger a long press.
        /// </summary>
        public float HoldDuration = 1.0f;

        [FormerlySerializedAs("onLongPress")] 
        /// <summary>
        /// Unity event triggered when the button is long pressed.
        /// </summary>
        public UnityEvent OnLongPress;

        private Coroutine _holdCoroutine;
        private bool _hasLongPressed;
        private bool _isPointerInsideButton;
        private bool _isPointerDown;

        private bool _isSelected;
        /// <summary>
        /// Gets a value indicating whether the button is currently selected.
        /// </summary>
        public bool IsSelected => _isSelected;

        private bool _interactable = true;
        /// <summary>
        /// Gets a value indicating whether the button is currently interactable.
        /// </summary>
        public bool Interactable => _interactable;

        protected virtual void OnEnable()
        {
            if (AnimateOnEnable)
            {
                StartCoroutine(DoingOnEnableAnimation());
            }
        }

        private void OnValidate()
        {
            if (HoldDuration < 0)
            {
                HoldDuration = 0;
            }
        }

#region Long Press
        /// <summary> Logic of whether this button can be long pressed. </summary>
        /// <returns> true if it can be long pressed, false otherwise.</returns>
        protected virtual bool CanLongPress()
        {
            return _interactable && EnableLongPress && _isPointerInsideButton && _isSelected;
        }

        /// <summary>
        /// Handles pointer down events for long press detection and animation.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanLongPress() || _holdCoroutine != null)
            {
                return;
            }

            _isPointerDown = true;
            _holdCoroutine = StartCoroutine(HoldCoroutine());
        }

        /// <summary>
        /// Handles pointer enter events for hover state management.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInsideButton = true;
        }

        /// <summary>
        /// Handles pointer exit events for hover state management.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanLongPress() || _holdCoroutine == null)
            {
                return;
            }

            _isPointerInsideButton = false;
            StopHoldCoroutine();
        }

        private IEnumerator HoldCoroutine()
        {
            yield return new WaitForSeconds(HoldDuration);

            if (!_isPointerDown || !_isPointerInsideButton)
            {
                yield break;
            }

            _hasLongPressed = true;
            OnLongPress?.Invoke();
        }

        private void StopHoldCoroutine()
        {
            if (_holdCoroutine == null)
            {
                return;
            }

            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }

#endregion

        /// <summary>
        /// Handles pointer click events and triggers appropriate animations and click events.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }

            if (EnableLongPress)
            {
                _isPointerDown = false;
                StopHoldCoroutine();
            }

            if (_hasLongPressed)
            {
                _hasLongPressed = false;
            }
            else
            {
                if (AnimateOnClick)
                {
                    if (RushAnimateClickEvent)
                    {
                        onClick.Invoke();
                    }

                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(DoingClickAnimation());
                    }
                }
                else
                {
                    onClick.Invoke();
                }

                SetButtonSelected(true);
            }
        }

        /// <summary>
        /// Sets the button's enabled state with optional animation.
        /// </summary>
        /// <param name="isEnabled">Whether the button should be enabled.</param>
        /// <param name="disableAnimation">Whether to skip the enable/disable animation.</param>
        public virtual void SetButtonEnabled(bool isEnabled, bool disableAnimation = false)
        {
            _interactable = isEnabled;

            if (disableAnimation)
            {
                return;
            }

            if (_interactable)
            {
                if (AnimateOnEnable && gameObject.activeInHierarchy)
                {
                    StartCoroutine(DoingOnEnableAnimation());
                }
            }
            else
            {
                if (AnimateOnDisable && gameObject.activeInHierarchy)
                {
                    StartCoroutine(DoingOnDisableAnimation());
                }
            }
        }

        /// <summary>
        /// Asynchronously sets the button's enabled state with optional animation.
        /// </summary>
        /// <param name="isEnabled">Whether the button should be enabled.</param>
        /// <param name="disableAnimation">Whether to skip the enable/disable animation.</param>
        /// <returns>A task that completes when the enable/disable operation is finished.</returns>
        public virtual async Task SetButtonEnabledAsync(bool isEnabled, bool disableAnimation = false)
        {
            _interactable = isEnabled;

            if (disableAnimation)
            {
                return;
            }

            if (_interactable)
            {
                if (AnimateOnEnable && gameObject.activeInHierarchy)
                {
                    await DoingOnEnableAnimationAsync();
                }
            }
            else
            {
                if (AnimateOnDisable && gameObject.activeInHierarchy)
                {
                    await DoingOnDisableAnimationAsync();
                }
            }
        }

        /// <summary>
        /// Sets the button's selected state and triggers appropriate selection/deselection animations.
        /// </summary>
        /// <param name="isSelected">Whether the button should be selected.</param>
        public virtual void SetButtonSelected(bool isSelected)
        {
            _isSelected = isSelected;
            if (_isSelected)
            {
                if (AnimateOnSelection && gameObject.activeInHierarchy)
                {
                    StartCoroutine(DoingOnSelectedAnimation());
                }
            }
            else
            {
                if (AnimateOnDeselection && gameObject.activeInHierarchy)
                {
                    StartCoroutine(DoingOnDeselectedAnimation());
                }
            }
        }

        protected virtual IEnumerator DoingOnEnableAnimation()
        {
            SetButtonEnabled(false, true);

            float t = 0;
            while (t < 1f)
            {
                t = DoScaleOpacityAnimation(t, OnEnableAnimTime, OnEnableScaleRange, new Vector2(0f, 1f), OnEnableScaleCurve, OnEnableAlphaCurve);
                yield return null;
            }

            SetButtonEnabled(true, true);
        }

        protected virtual async Task DoingOnEnableAnimationAsync()
        {
            await SetButtonEnabledAsync(false, true);

            float t = 0;
            while (t < 1f)
            {
                t = DoScaleOpacityAnimation(t, OnEnableAnimTime, OnEnableScaleRange, new Vector2(0f, 1f), OnEnableScaleCurve, OnEnableAlphaCurve);
                await Task.Yield();
            }

            await SetButtonEnabledAsync(true, true);
        }

        protected virtual IEnumerator DoingOnDisableAnimation()
        {
            SetButtonEnabled(false, true);

            float t = 0;
            while (t < 1f)
            {
                t = DoScaleOpacityAnimation(t, OnDisableAnimTime, OnDisableScaleRange, new Vector2(0f, 1f), OnDisableScaleCurve, OnDisableAlphaCurve);
                yield return null;
            }
        }

        protected virtual async Task DoingOnDisableAnimationAsync()
        {
            await SetButtonEnabledAsync(false, true);

            float t = 0;
            while (t < 1f)
            {
                t = DoScaleOpacityAnimation(t, OnDisableAnimTime, OnDisableScaleRange, new Vector2(0f, 1f), OnDisableScaleCurve, OnDisableAlphaCurve);
                await Task.Yield();
            }
        }

        protected virtual IEnumerator DoingOnSelectedAnimation()
        {
            SetButtonEnabled(false, true);

            float t = 0;
            while (t < 1f)
            {
                t = DoScaleOpacityAnimation(t, OnSelectionAnimTime, OnSelectionScaleRange, OnSelectionAlphaRange, OnSelectionScaleCurve, OnSelectionAlphaCurve);
                yield return null;
            }

            SetButtonEnabled(true, true);
        }

        protected virtual IEnumerator DoingOnDeselectedAnimation()
        {
            SetButtonEnabled(false, true);

            float t = 0;
            while (t < 1f)
            {
                t = DoScaleOpacityAnimation(t, OnDeselectionAnimTime, OnDeselectionScaleRange, OnDeselectionAlphaRange, OnDeselectionScaleCurve, OnDeselectionAlphaCurve);
                yield return null;
            }

            SetButtonEnabled(true, true);
        }

        private float DoScaleOpacityAnimation(float t, float animTime, Vector2 scaleRange, Vector2 alphaRange, AnimationCurve scaleCurve, AnimationCurve alphaCurve)
        {
            t += Time.deltaTime / animTime;
            t = Mathf.Clamp01(t);

            if (ButtonGroupRT != null)
            {
                ButtonGroupRT.localScale = Vector3.LerpUnclamped(Vector3.one * scaleRange.x, Vector3.one * scaleRange.y, scaleCurve.Evaluate(t));
            }

            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = Mathf.Lerp(alphaRange.x, alphaRange.y, alphaCurve.Evaluate(t));
            }

            return t;
        }

        protected virtual IEnumerator DoingClickAnimation()
        {
            SetButtonEnabled(false, true);

            if (!AnimateOnSelection)
            {
                float t = 0;
                while (t < 1f)
                {
                    t = DoClickAnimation(t);

                    yield return null;
                }
            }
            else
            {
                SetButtonSelected(true);
            }

            SetButtonEnabled(true, true);
            EventSystem.current.SetSelectedGameObject(gameObject);

            if (!RushAnimateClickEvent)
            {
                onClick.Invoke();
            }
        }

        protected virtual float DoClickAnimation(float t)
        {
            t += Time.deltaTime / OnClickAnimTime;
            t = Mathf.Clamp01(t);

            if (ButtonGroupRT != null)
            {
                ButtonGroupRT.localScale = Vector3.LerpUnclamped(Vector3.one * OnClickScaleRange.x, Vector3.one * OnClickScaleRange.y, OnClickScaleCurve.Evaluate(t));
            }

            return t;
        }

        private void OnDisable()
        {
            SetButtonEnabled(true, true);
        }
    }
}
