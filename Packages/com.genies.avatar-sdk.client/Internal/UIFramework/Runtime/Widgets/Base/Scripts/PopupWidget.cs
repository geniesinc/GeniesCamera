using System;
using System.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    public class PopupWidget : MonoBehaviour
    {
        [Header("References")]
        [FormerlySerializedAs("shadowImage")] public Image ShadowImage;
        [Obsolete("Use 'ShadowImage' instead.")] public Image shadowImage => ShadowImage;
        [FormerlySerializedAs("backgroundBlocker")] public Image BackgroundBlocker; // If assigned, will block raycasts during popup
        [Obsolete("Use 'BackgroundBlocker' instead.")] public Image backgroundBlocker => BackgroundBlocker;

        [FormerlySerializedAs("panelRectTransform")] public RectTransform PanelRectTransform;
        [Obsolete("Use 'PanelRectTransform' instead.")] public RectTransform panelRectTransform => PanelRectTransform;

        [FormerlySerializedAs("fadeGroup")] public CanvasGroup FadeGroup;
        [Obsolete("Use 'FadeGroup' instead.")] public CanvasGroup fadeGroup => FadeGroup;

        [FormerlySerializedAs("contentGroup")] public CanvasGroup ContentGroup;
        [Obsolete("Use 'ContentGroup' instead.")] public CanvasGroup contentGroup => ContentGroup;

        [Header("Animation Settings")]
        [FormerlySerializedAs("showAnimTime")] public float ShowAnimTime = 0.2f;
        [Obsolete("Use 'ShowAnimTime' instead.")] public float showAnimTime => ShowAnimTime;
        [FormerlySerializedAs("hideAnimTime")] public float HideAnimTime = 0.2f;
        [Obsolete("Use 'HideAnimTime' instead.")] public float hideAnimTime => HideAnimTime;
        [FormerlySerializedAs("scaleFactor")] public float ScaleFactor = 0.8f;
        [Obsolete("Use 'ScaleFactor' instead.")] public float scaleFactor => ScaleFactor;

        [FormerlySerializedAs("showCurve")] public AnimationCurve ShowCurve;
        [Obsolete("Use 'ShowCurve' instead.")] public AnimationCurve showCurve => ShowCurve;

        [FormerlySerializedAs("hideCurve")] public AnimationCurve HideCurve;
        [Obsolete("Use 'HideCurve' instead.")] public AnimationCurve hideCurve => HideCurve;

        [FormerlySerializedAs("shadowColor")] [SerializeField]
        protected Color _shadowColor = new Color(0, 0, 0, 0.2f);
        [FormerlySerializedAs("clearColor")] [SerializeField]
        protected Color _clearColor = new Color(0f, 0f, 0f, 0f);

        [Header("Behavior")]
        public bool ResetOnShow = false;
        public bool AllowTapToDismiss = false;

        protected RectTransform _rectTransform;
        protected bool _shown = false;
        protected float _animTime = 0f;
        protected float _shownTime = 0f;
        protected float _hideAfterSeconds = 0f;

        public virtual void OnEnable()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            SetComponentsActive(_shown);
        }

        public virtual void Show(float seconds = 0f)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            SetComponentsActive(true);

            _shown = true;
            _shownTime = Time.time;

            if (ResetOnShow)
            {
                _animTime = 0f;
                UpdateState(_animTime);
            }

            _hideAfterSeconds = seconds;
        }

        public void Hide()
        {
            _shown = false;
        }

        public async Task HideAsync()
        {
            _shown = false;
            while (_animTime > 0)
            {
                await Task.Yield();
            }
        }

        protected virtual void OnShown() { }

        protected virtual void OnHidden()
        {
            SetComponentsActive(false);
        }

        private void SetComponentsActive(bool active)
        {
            if (ContentGroup != null)
            {
                ContentGroup.gameObject.SetActive(active);
            }

            if (ShadowImage != null)
            {
                ShadowImage.gameObject.SetActive(active);
            }

            if (PanelRectTransform != null)
            {
                PanelRectTransform.gameObject.SetActive(active);
            }

            if (BackgroundBlocker != null)
            {
                BackgroundBlocker.gameObject.SetActive(active);
            }
        }

        protected void UpdateState(float time)
        {
            try
            {
                float scaleTime = _shown ? ShowCurve.Evaluate(time) : HideCurve.Evaluate(time);
                _rectTransform.localScale = Vector3.LerpUnclamped(VectorFromFloat(ScaleFactor), VectorFromFloat(1f), scaleTime);

                if (ShadowImage != null)
                {
                    ShadowImage.color = Color.Lerp(_clearColor, _shadowColor, time);
                }

                if (FadeGroup != null)
                {
                    FadeGroup.alpha = time;
                }

                if (ContentGroup != null)
                {
                    ContentGroup.alpha = Mathf.InverseLerp(0.5f, 1f, time);
                }
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
        }

        private void Update()
        {
            UpdateAnim();
            HandleTapDismiss();
            HandleHideAfter();
        }

        protected void UpdateAnim()
        {
            if (_shown && _animTime < 1f)
            {
                _animTime += Time.deltaTime / ShowAnimTime;
                if (_animTime > 1f)
                {
                    _animTime = 1f;
                    OnShown();
                }

                UpdateState(_animTime);
            }
            else if (!_shown && _animTime > 0f)
            {
                _animTime -= Time.deltaTime / HideAnimTime;
                if (_animTime < 0f)
                {
                    _animTime = 0f;
                    OnHidden();
                }

                UpdateState(_animTime);
            }
        }

        protected virtual void HandleTapDismiss()
        {
            if (AllowTapToDismiss && _animTime >= 1f)
            {
                if (Application.isEditor)
                {
                    if (UnityEngine.Input.GetMouseButtonDown(0))
                    {
                        var localPos = PanelRectTransform.InverseTransformPoint(UnityEngine.Input.mousePosition);
                        if (!PanelRectTransform.rect.Contains(localPos))
                        {
                            Hide();
                        }
                    }
                }
                else
                {
                    if (UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        var touch = UnityEngine.Input.GetTouch(0);
                        var localTouchPos = PanelRectTransform.InverseTransformPoint(touch.position);
                        if (!PanelRectTransform.rect.Contains(localTouchPos))
                        {
                            Hide();
                        }
                    }
                }
            }
        }

        protected void HandleHideAfter()
        {
            if (_hideAfterSeconds != 0f && Time.time - _shownTime > _hideAfterSeconds)
            {
                Hide();
            }
        }

        protected Vector3 VectorFromFloat(float f) => new Vector3(f, f, f);
    }
}
