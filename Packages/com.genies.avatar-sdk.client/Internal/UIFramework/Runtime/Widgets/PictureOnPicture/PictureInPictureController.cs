using System;
using System.Collections;
using Genies.ServiceManagement;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Genies.UI.Animations;

namespace Genies.UIFramework.Widgets
{
    /// <summary>
    /// Provide methods to display the Picture in Picture window,
    /// to preview Avatars when editing UI takes full-screen
    /// </summary>
    public class PictureInPictureController : MonoBehaviour
    {
        private InputProvider _InputProvider => this.GetService<InputProvider>();
        private PictureInPictureCameraProvider _CameraProvider => this.GetService<PictureInPictureCameraProvider>();
        private InteractionController _InteractionController => this.GetService<GenieInteractionController>();

        [Header("Override Interaction Controller")]
        public InteractionController pipInteractionController;

        [Header("Change Size Button")]
        public GeniesButton changeSizeButton;
        public RectTransform changeSizeRT;
        public Image backgroundImage;
        public Image iconImage;
        public Sprite enlargeIcon;
        public Sprite shrinkIcon;
        public Color32 enlargeBGColor = new Color32(75, 73, 74, 255);
        public Color32 enlargeIconColor = new Color32(255, 255, 255, 255);
        public Color32 shrinkBGColor = new Color32(255, 255, 255, 255);
        public Color32 shrinkIconColor = new Color32(0, 0, 0, 255);
        public Vector2 bigButtonPosition = new Vector2(-27.5f, -27.5f);
        public Vector2 smallButtonPosition = new Vector2(-10f, -10f);
        public float iconTransitionTime = 0.5f;

        [Header("References")]
        public RectTransform canvasRT;
        public RectTransform pictureRT;
        public RectTransform previewRT;
        public Image previewImage;
        public RectTransform videoRT;
        public RawImage videoImage;

        [SerializeField] private Camera _pipCamera;


        [Header("Options")]
        public Color32 ClearColor = new Color32(255, 255, 255, 0);
        public VideoTextureRendererComponents VideoTextureRendererComponents;
        public float padding = 25f;
        public float resizeMultiplier = 2f;
        public float peekabooDetectionMultiplier = 0.5f;
        public float hiddenPercentage = 0.75f;

        [Header("Rounded Edges")]
        public float pixelsPerUnitSmall = 17.5f;
        public float pixelsPerUnitBig = 8f;

        [Header("Configs")]
        public float anchoringDuration = 0.25f;

        public float resizeDuration = 0.5f;
        public float showDuration = 0.75f;

        private readonly Color32 _renderColor = new Color32(255, 255, 255, 255);

        private VideoTextureRenderer _videoTextureRenderer;

        private Vector2 _canvasSizeDelta;
        private Vector2 _pictureSizeDelta;
        private Vector2 _videoSizeDelta;

        private bool _isPointInsideRectTransform;

        private bool _isDragging;
        private Coroutine _dragCoroutine;
        private UIAnimator _dragAnchoringAnimation;

        private PictureInPictureSizeMode _currentSizeMode;
        private UIAnimator _pictureResizeAnimation;
        private UIAnimator _videoResizeAnimation;

        private UIAnimator _pictureTransformAnimation;
        private UIAnimator _videoTransformAnimation;

        private Vector2 _pictureAnchoredPosition;
        private Vector2 _targetPosition;
        private Vector2 _currentPictureSizeDelta;

        private Vector2 _lastPosition;
        private Transform _originalPiPCameraParent;

        public event Action<PictureInPictureSizeMode> OnSizeModeChanged;

        private bool _isEnabled;
        public bool IsEnabled => _isEnabled;

        /// <summary>
        ///  Exposing for the consumers, a way to control when the component can be disabled
        /// the flag in false blocks to disable the component
        /// </summary>
        public bool canBeDisabled;

        private bool _isTransitioning;

        private void Awake()
        {
            ServiceManager.RegisterService(this);
        }
        private void Start()
        {
            _isEnabled = false;
            _isTransitioning = false;
            canBeDisabled = true;

            Assert.IsNotNull(_pipCamera);

            // Set initial size delta of Rect Transforms
            _canvasSizeDelta = canvasRT.sizeDelta;
            _pictureSizeDelta = pictureRT.sizeDelta;
            _videoSizeDelta = videoRT.sizeDelta;

            // Subscribe to Input Provider events
            // When first contact is initiated
            _InputProvider.TapAndHold().performed += _ =>
            {
                _isPointInsideRectTransform = _InputProvider.CheckIfTapIsInRectTransform(videoRT);
                BeginDrag();
            };

            // When first contact ends
            _InputProvider.TapAndHold().canceled += _ =>
            {
                EndDrag();
            };

            // Hide when first starting
            videoImage.color = ClearColor;
            pictureRT.sizeDelta = Vector2.zero;
            videoRT.sizeDelta = Vector2.zero;

            CalculatePosition(false, true);
        }

        private void BeginDrag()
        {
            if (_isPointInsideRectTransform && _currentSizeMode == PictureInPictureSizeMode.Small)
            {
                if (_dragCoroutine != null)
                {
                    StopCoroutine(_dragCoroutine);
                }

                _dragCoroutine = StartCoroutine(DragUpdate(pictureRT));
            }
        }

        private IEnumerator DragUpdate(RectTransform rectTransform)
        {
            _isDragging = true;

            var directionFromCenter = (Vector2)rectTransform.position - _InputProvider.ScreenPosition();

            while (_isDragging)
            {
                rectTransform.position = _InputProvider.ScreenPosition() + directionFromCenter;
                yield return null;
            }
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            CalculatePosition();
        }

        /// <summary>
        ///  Initialize the component behavior including:
        ///  Show in the view (PictureInPictureSizeMode)
        ///  Initialize the render and controllers
        /// </summary>
        public void Enable()
        {
            // Terminate all existing animations first to prevent conflicts
            if (this._videoTransformAnimation != null)
            {
                this._videoTransformAnimation.Terminate();
            }
            _pictureTransformAnimation?.Terminate();
            _pictureResizeAnimation?.Terminate();
            _videoResizeAnimation?.Terminate();
            _dragAnchoringAnimation?.Terminate();

            this.gameObject.SetActive(true);

            changeSizeButton.onClick.AddListener(ChangeSize);

            // Move the UI element to the last child so it renders first than other UI elements
            transform.SetAsLastSibling();

            if (!_isEnabled)
            {
                _isEnabled = true;
            }

            // TODO: Doing null checks here might cause this to exit early, we should check whats it's assigning values to
            // _InteractionController and _InteractionController.Controllable
            // Initialize Controllable - maybe change this to a method to support other use cases.
            pipInteractionController.Controllable = _InteractionController.Controllable;

            if (_pipCamera != null)
            {
                // Parent PiP camera to the transform of the active camera
                _originalPiPCameraParent = _pipCamera.transform.parent;
                Transform cameraTransform = _CameraProvider.Camera.transform;
                _pipCamera.gameObject.transform.SetParent(cameraTransform);
                _pipCamera.transform.localPosition = Vector3.zero;
                _pipCamera.transform.localRotation = Quaternion.identity;

                StartRender();

                _pipCamera.gameObject.SetActive(true);
            }

            // Enable first and do the show animation
            if (!previewRT.gameObject.activeSelf)
            {
                previewRT.gameObject.SetActive(true);
            }

            // Set size mode first so CalculatePosition uses the correct target size
            _currentSizeMode = PictureInPictureSizeMode.Small;

            pictureRT.sizeDelta = _pictureSizeDelta;
            videoRT.sizeDelta = _videoSizeDelta;
            _currentPictureSizeDelta = _pictureSizeDelta;

            // Force layout update to ensure anchored position is accurate
            Canvas.ForceUpdateCanvases();

            _pictureAnchoredPosition = pictureRT.anchoredPosition;

            // If anchored position is zero/uninitialized, default to bottom-left corner
            if (_pictureAnchoredPosition == Vector2.zero)
            {
                _pictureAnchoredPosition = new Vector2(-1f, -1f);
            }

            MoveToCorner();
            if (_currentSizeMode == PictureInPictureSizeMode.Small)
            {
                TriggerPeekabooState();
            }

            // Set position immediately to target (no animation) to prevent bouncing
            pictureRT.localPosition = _targetPosition;
            _lastPosition = _targetPosition;

            // Update UI elements for small mode
            iconImage.sprite = enlargeIcon;
            iconImage.SpringColor(enlargeIconColor, SpringPhysics.Presets.Smooth);
            backgroundImage.SpringColor(enlargeBGColor, SpringPhysics.Presets.Smooth);
            changeSizeRT.SpringAnchorPos(smallButtonPosition, SpringPhysics.Presets.Snappy);

            AnimateVirtual.Float(pixelsPerUnitBig, pixelsPerUnitSmall, iconTransitionTime, value =>
            {
                previewImage.pixelsPerUnitMultiplier = value;
            });
        }

        public void Disable()
        {
            if (!canBeDisabled)
            {
                return;
            }

            // Terminate all active animations to prevent conflicts
            _dragAnchoringAnimation?.Terminate();
            _pictureResizeAnimation?.Terminate();
            _videoResizeAnimation?.Terminate();

            if (_pipCamera != null)
            {
                if (_originalPiPCameraParent != null)
                {
                    _pipCamera.gameObject.transform.SetParent(_originalPiPCameraParent);
                }

                _pipCamera.gameObject.SetActive(false);
            }

            changeSizeButton.onClick.RemoveAllListeners();

            StopRender();

            // Disable the Interaction Controller first
            pipInteractionController.SetEnabled(false);
            pipInteractionController.Controllable = null;

            if (_isTransitioning && _videoTransformAnimation != null)
            {
                _pictureTransformAnimation?.Terminate();
            }

            SetIsTransitioning(true);

            // Ensure GameObject stays active during animation
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Begin animation to disable PiP Controller - use Slow preset for visible, gradual close
            // This matches the normal close animation behavior
            _pictureTransformAnimation = pictureRT.SpringSizeDelta(Vector2.zero, SpringPhysics.Presets.Slow);

            _videoTransformAnimation = videoRT.SpringSizeDelta(Vector2.zero, SpringPhysics.Presets.Slow);
            // add a callback to the animation to disable the preview when the animation is finished or killed.
            _videoTransformAnimation.Terminated += OnTransitionTerminated;
            _videoTransformAnimation.Completed += OnTransitionComplete;
        }

        private void OnTransitionComplete()
        {
            _videoTransformAnimation.Completed -= OnTransitionComplete;

            SetIsTransitioning(false);
            _isEnabled = false;
            this.gameObject.SetActive(false);
        }

        private void OnTransitionTerminated()
        {
            _videoTransformAnimation.Terminated -= OnTransitionTerminated;

            SetIsTransitioning(false);
            this.gameObject.SetActive(false);
        }

        public void SetSizeMode(PictureInPictureSizeMode newMode)
        {
            _pictureResizeAnimation?.Terminate();
            _videoResizeAnimation?.Terminate();

            switch (newMode)
            {
                case PictureInPictureSizeMode.Big:
                    pipInteractionController.SetEnabled(true);

                    // Gentle springs for subtle size change
                    _pictureResizeAnimation = pictureRT.SpringSizeDelta(_pictureSizeDelta * resizeMultiplier, SpringPhysics.Presets.Gentle);
                    _videoResizeAnimation = videoRT.SpringSizeDelta(_videoSizeDelta * resizeMultiplier, SpringPhysics.Presets.Gentle);

                    iconImage.sprite = shrinkIcon;
                    iconImage.SpringColor(shrinkIconColor, SpringPhysics.Presets.Smooth);
                    backgroundImage.SpringColor(shrinkBGColor, SpringPhysics.Presets.Smooth);

                    changeSizeRT.SpringAnchorPos(bigButtonPosition, SpringPhysics.Presets.Snappy);

                    AnimateVirtual.Float(pixelsPerUnitSmall, pixelsPerUnitBig, iconTransitionTime, value =>
                    {
                        previewImage.pixelsPerUnitMultiplier = value;
                    });

                    break;
                case PictureInPictureSizeMode.Small:
                    pipInteractionController.SetEnabled(false);

                    // Gentle springs for subtle size change
                    _pictureResizeAnimation = pictureRT.SpringSizeDelta(_pictureSizeDelta, SpringPhysics.Presets.Gentle);
                    _videoResizeAnimation = videoRT.SpringSizeDelta(_videoSizeDelta, SpringPhysics.Presets.Gentle);

                    iconImage.sprite = enlargeIcon;
                    iconImage.SpringColor(enlargeIconColor, SpringPhysics.Presets.Smooth);
                    backgroundImage.SpringColor(enlargeBGColor, SpringPhysics.Presets.Smooth);

                    changeSizeRT.SpringAnchorPos(smallButtonPosition, SpringPhysics.Presets.Snappy);

                    AnimateVirtual.Float(pixelsPerUnitBig, pixelsPerUnitSmall, iconTransitionTime, value =>
                    {
                        previewImage.pixelsPerUnitMultiplier = value;
                    });

                    break;
            }

            _currentSizeMode = newMode;
            CalculatePosition();
        }

        private void SetIsTransitioning(bool state)
        {
            _isTransitioning = state;
        }

        private void StartRender()
        {
            videoImage.color = _renderColor;

            _videoTextureRenderer = new VideoTextureRenderer(_pipCamera, VideoTextureRendererComponents);
        }

        private void StopRender()
        {
            if (_videoTextureRenderer == null)
            {
                return;
            }

            _videoTextureRenderer.CleanCamera();
        }

        private void CalculatePosition(bool allowPeekaboo = true, bool immediate = false)
        {
            // End the tween if it's still running
            _dragAnchoringAnimation?.Terminate();

            // Get anchored position of the Picture-in-Picture Rect Transform
            _pictureAnchoredPosition = pictureRT.anchoredPosition;

            // Check current size mode
            switch (_currentSizeMode)
            {
                case PictureInPictureSizeMode.Small:
                    _currentPictureSizeDelta = _pictureSizeDelta;
                    break;
                case PictureInPictureSizeMode.Big:
                    _currentPictureSizeDelta = _pictureSizeDelta * resizeMultiplier;
                    break;
            }

            MoveToCorner();

            if (_currentSizeMode == PictureInPictureSizeMode.Small && allowPeekaboo)
            {
                TriggerPeekabooState();
            }

            // Move the Picture-in-Picture to target position - smooth spring for natural feel
            if (immediate)
            {
                pictureRT.localPosition = _targetPosition;
            }
            else
            {
                _dragAnchoringAnimation = pictureRT.SpringLocalPosition(_targetPosition, SpringPhysics.Presets.Smooth);
            }

            // Save last position to static field
            _lastPosition = _targetPosition;
        }

        private void MoveToCorner()
        {
            // Calculate target position in X & Y axis
            var xValue = (_currentPictureSizeDelta.x - _canvasSizeDelta.x) * 0.5f;
            var yValue = (_currentPictureSizeDelta.y - _canvasSizeDelta.y) * 0.5f;

            switch (_pictureAnchoredPosition.x < 0f)
            {
                // Bottom Left
                case true when _pictureAnchoredPosition.y < 0f:
                    _targetPosition = new Vector2(xValue + padding, yValue + padding);
                    break;
                // Top Left
                case true when _pictureAnchoredPosition.y >= 0f:
                    _targetPosition = new Vector2(xValue + padding, -yValue - padding);
                    break;
                default:
                    switch (_pictureAnchoredPosition.x >= 0f)
                    {
                        // Bottom Right
                        case true when _pictureAnchoredPosition.y <= 0f:
                            _targetPosition = new Vector2(-xValue - padding, yValue + padding);
                            break;
                        // Top Right
                        case true when _pictureAnchoredPosition.y > 0f:
                            _targetPosition = new Vector2(-xValue - padding, -yValue - padding);
                            break;
                    }

                    break;
            }
        }

        private void TriggerPeekabooState()
        {
            switch (_pictureAnchoredPosition.x < 0f)
            {
                // Bottom Left
                case true when _pictureAnchoredPosition.y < 0f:
                    if (_currentSizeMode == PictureInPictureSizeMode.Small &&
                        _pictureAnchoredPosition.x < _targetPosition.x - _currentPictureSizeDelta.x * peekabooDetectionMultiplier ||
                        _pictureAnchoredPosition.y < _targetPosition.y - _currentPictureSizeDelta.y * peekabooDetectionMultiplier)
                    {
                        _targetPosition.x -= _currentPictureSizeDelta.x * hiddenPercentage;
                    }

                    break;
                // Top Left
                case true when _pictureAnchoredPosition.y >= 0f:
                    if (_currentSizeMode == PictureInPictureSizeMode.Small &&
                        _pictureAnchoredPosition.x < _targetPosition.x - _currentPictureSizeDelta.x * peekabooDetectionMultiplier ||
                        _pictureAnchoredPosition.y > _targetPosition.y + _currentPictureSizeDelta.y * peekabooDetectionMultiplier)
                    {
                        _targetPosition.x -= _currentPictureSizeDelta.x * hiddenPercentage;
                    }

                    break;
                default:
                    switch (_pictureAnchoredPosition.x >= 0f)
                    {
                        // Bottom Right
                        case true when _pictureAnchoredPosition.y <= 0f:
                            if (_currentSizeMode == PictureInPictureSizeMode.Small &&
                                _pictureAnchoredPosition.x > _targetPosition.x + _currentPictureSizeDelta.x * peekabooDetectionMultiplier ||
                                _pictureAnchoredPosition.y < _targetPosition.y - _currentPictureSizeDelta.y * peekabooDetectionMultiplier)
                            {
                                _targetPosition.x += _currentPictureSizeDelta.x * hiddenPercentage;
                            }

                            break;
                        // Top Right
                        case true when _pictureAnchoredPosition.y > 0f:
                            if (_currentSizeMode == PictureInPictureSizeMode.Small &&
                                _pictureAnchoredPosition.x > _targetPosition.x + _currentPictureSizeDelta.x * peekabooDetectionMultiplier ||
                                _pictureAnchoredPosition.y > _targetPosition.y + _currentPictureSizeDelta.y * peekabooDetectionMultiplier)
                            {
                                _targetPosition.x += _currentPictureSizeDelta.x * hiddenPercentage;
                            }

                            break;
                    }

                    break;
            }
        }

        private void ChangeSize()
        {
            switch (_currentSizeMode)
            {
                case PictureInPictureSizeMode.Small:
                    SetSizeMode(PictureInPictureSizeMode.Big);
                    break;
                case PictureInPictureSizeMode.Big:
                    SetSizeMode(PictureInPictureSizeMode.Small);
                    break;
            }

            OnSizeModeChanged?.Invoke(_currentSizeMode);
        }
    }

    public enum PictureInPictureSizeMode
    {
        Small = 0,
        Big = 1,
    }
}
