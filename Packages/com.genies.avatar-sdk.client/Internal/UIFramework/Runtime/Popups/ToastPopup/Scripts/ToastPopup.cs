using System;
using System.Collections;
using Genies.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UIFramework
{

    /// <summary>
    /// Configuration to create/show a toast popup.
    /// </summary>
    [Serializable]
    public class ToastConfig
    {
        /// <summary>
        /// The background color(optional)
        /// </summary>
        [FormerlySerializedAs("backgroundColor")] public Color BackgroundColor = Color.white;
        /// <summary>
        /// The background sprite (optional).
        /// </summary>
        [FormerlySerializedAs("backgroundSprite")] public Sprite BackgroundSprite;

        /// <summary>
        /// The foreground color(optional)
        /// </summary>
        [FormerlySerializedAs("foregroundColor")] public Color ForegroundColor = Color.white;
        /// <summary>
        /// The foreground sprite(optional).
        /// </summary>
        [FormerlySerializedAs("foregroundSprite")] public Sprite ForegroundSprite;

        /// <summary>
        /// Main message text for the toast.
        /// </summary>
        [FormerlySerializedAs("message")] public string Message;

        /// <summary>
        /// Optional action that will be triggered when the text is clicked. It'll hide the toast after the action is executed.
        /// </summary>
        public Action OnTextClicked;

        /// <summary>
        /// Optional action that will be triggered when the toast is closed.
        /// By default, it will just hide the toast if not specified.
        /// </summary>
        public Action OnClose;

        [FormerlySerializedAs("duration")] public float Duration = 4f;

        [FormerlySerializedAs("slideInDuration")] public float SlideInDuration = 0.4f;

        [FormerlySerializedAs("slideOutDuration")] public float SlideOutDuration = 0.4f;

        [FormerlySerializedAs("onScreenPosition")] public Vector2 OnScreenPosition = new Vector2(0f, 712);

        [FormerlySerializedAs("offScreenPosition")] public Vector2 OffScreenPosition = new Vector2(0f, 1094);
    }

    public class ToastPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _foregroundImage;

        [SerializeField] private Button _textButton;
        [SerializeField] private TextMeshProUGUI _toastMessage;
        [SerializeField] private Button _closeButton;

        private RectTransform _rectTransform;
        private Coroutine _currentRoutine;
        private Coroutine _hideCoroutine;
        private ToastConfig _currentConfig;
        private bool _isClosing; // to prevent multiple close animations overlapping

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (!_rectTransform)
            {
                Debug.LogError(
                    $"No RectTransform found on {gameObject.name}. The Toast script requires a RectTransform.");
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show/update the toast with a new configuration.
        /// Slides in from the bottom, waits for duration, then slides out (auto-hide).
        /// </summary>
        public void Show(ToastConfig config)
        {
            // Stop any ongoing routine (slide in, slide out, auto-hide, etc.)
            if (_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
            }

            // Reset isClosing flag
            _isClosing = false;

            // Store config
            _currentConfig = config;

            // Set up UI
            SetupUI(config);

            // Immediately set the toast to the off-screen position & make it active
            _rectTransform.anchoredPosition = config.OffScreenPosition;
            gameObject.SetActive(true);

            // Start a new routine that:
            //   1) Slides in
            //   2) Waits for duration
            //   3) Slides out (auto-hide)
            _currentRoutine = StartCoroutine(SlideInThenWaitThenAutoClose());
        }

        /// <summary>
        /// Manually close the toast with a slide-out animation.
        /// This is typically called by the close button or externally.
        /// </summary>
        public void Hide()
        {
            if (_isClosing)
            {
                return; // Already closing/closed
            }

            _isClosing = true;

            // Stop any other running routine (so we don't auto-hide twice, etc.)
            if (_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
            }

            // Start the slide-out animation
            _currentRoutine = StartCoroutine(SlideOutAndHide());
        }

        /// <summary>
        /// Sets up the UI elements according to the config.
        /// </summary>
        private void SetupUI(ToastConfig config)
        {
            // Set background and foreground images
            if (_backgroundImage != null)
            {
                _backgroundImage.color = config.BackgroundColor;

                if (config.BackgroundSprite != null)
                {
                    _backgroundImage.sprite = config.BackgroundSprite;
                }
            }

            if (_foregroundImage != null)
            {
                if (config.ForegroundSprite != null)
                {
                    _foregroundImage.sprite = config.ForegroundSprite;
                    _foregroundImage.gameObject.SetActive(true);
                    _foregroundImage.color = config.ForegroundColor;
                }
                else
                {
                    _foregroundImage.gameObject.SetActive(false);
                }
            }

            // Text and onTextClicked
            if (_toastMessage != null)
            {
                _toastMessage.text = config.Message;
            }

            if (_textButton != null)
            {
                _textButton.onClick.RemoveAllListeners();
                if (config.OnTextClicked != null)
                {
                    _textButton.onClick.AddListener(() =>
                    {
                        config.OnTextClicked.Invoke();
                        Hide();
                    });
                }
            }

            // Close button
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Hide);
            }
        }

        /// <summary>
        /// 1) Slides the toast in
        /// 2) Waits for the duration
        /// 3) Slides the toast out
        /// </summary>
        private IEnumerator SlideInThenWaitThenAutoClose()
        {
            // Slide in using DOTween
            float slideInDur = _currentConfig.SlideInDuration <= 0 ? 0.5f : _currentConfig.SlideInDuration;
            if (slideInDur > 0)
            {
                // Animate to on-screen position and wait for completion
                yield return _rectTransform
                    .AnimateAnchorPos(_currentConfig.OnScreenPosition, slideInDur)
                    .SetEase(Ease.OutCubic)
                    .WaitForCompletion();
            }
            else
            {
                // If duration is 0 or negative, jump instantly
                _rectTransform.anchoredPosition = _currentConfig.OnScreenPosition;
            }

            // Wait for the display duration
            float duration = _currentConfig.Duration <= 0 ? 5f : _currentConfig.Duration;
            yield return new WaitForSeconds(duration);

            // If still visible (not manually closed), auto-close
            if (!_isClosing)
            {
                _isClosing = true;
                yield return SlideOutAndHide();
            }
        }

        /// <summary>
        /// Slides the toast from the current position to off-screen, then disables it.
        /// </summary>
        private IEnumerator SlideOutAndHide()
        {
            var slideOutDur = _currentConfig.SlideOutDuration <= 0 ? 0.5f : _currentConfig.SlideOutDuration;

            if (slideOutDur > 0)
            {
                // Animate to off-screen and wait for completion
                yield return _rectTransform
                    .AnimateAnchorPos(_currentConfig.OffScreenPosition, slideOutDur)
                    .SetEase(Ease.InCubic)
                    .WaitForCompletion();
            }
            else
            {
                _rectTransform.anchoredPosition = _currentConfig.OffScreenPosition;
            }

            // Deactivate and hide
            gameObject.SetActive(false);
            _currentConfig.OnClose?.Invoke();

            // self destroy
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Destroy(_foregroundImage.sprite);
            Destroy(_backgroundImage.sprite);
        }
    }
}
