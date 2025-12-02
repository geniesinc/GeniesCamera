using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.CameraSystem;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Customization.Framework.Navigation;
using Genies.ServiceManagement;
using Genies.UI;
using Genies.UI.Animations;
using Genies.UIFramework.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Looks.View
{
    public class CustomizerDrawerView : CustomizerViewBase
    {
        private const int ExitedState = 0;
        private const int HiddenState = 1;
        private const int DefaultState = 2;
        private const int GalleryExpandedState = 3;
        private const int GalleryFullScreenState = 4;

        [Header("Drawer Components")]

        [SerializeField]
        private float _panelExitedHeight = -100f;

        [SerializeField]
        private float _panelHiddenHeight = 60f; // sets a min hidden height to avoid conflicting with system's gesture control at the bottom

        [SerializeField]
        private float _panelMinHeight;

        [SerializeField]
        private float _panelMaxHeight;

        [SerializeField]
        private int _backButtonPadding;

        [SerializeField]
        private RectTransform _galleryRt;

        [SerializeField]
        private RectTransform _navBarRect;

        [SerializeField]
        private HorizontalLayoutGroup _navBarLayout;

        [SerializeField]
        private RectTransform _navBarRt;

        [SerializeField]
        private RectTransform _selectorRt;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private GalleryItemPicker _galleryItemPicker;

        [SerializeField]
        private CanvasGroup _actionBarCanvas;

        [SerializeField]
        private CanvasGroup _secondaryItemPickerCanvas;

        [SerializeField]
        private CanvasGroup _galleryItemPickerCanvas;

        [SerializeField]
        private CanvasGroup _primaryItemPickerCanvas;

        [SerializeField]
        private RectTransform _customizationEditorContent;

        [SerializeField]
        private RectTransform _previewArea;

        [SerializeField]
        private ExpandablePanel _panel;

        [SerializeField]
        private GameObject _panelHandle;

        [Header("Transition Components")]
        [SerializeField]
        private CustomizerBackgroundElement[] _backgrounds;

        [SerializeField]
        private SwappableCustomizerViewComponent navBarViewComponents;

        [SerializeField]
        private CustomizerViewComponents actionBarViewComponents;

        [SerializeField]
        private SwappableCustomizerViewComponent fullscreenCustomizationEditorViewComponents;

        [SerializeField]
        private SwappableCustomizerViewComponent customizationEditorViewComponents;

        [SerializeField]
        private SwappableCustomizerViewComponent secondaryItemPickerViewComponents;

        [Header("Animation Data")]
        [SerializeField]
        private Color _backgroundDefaultColor;

        [SerializeField]
        private Ease _animationEase = Ease.InOutSine;

        [SerializeField]
        private float _animationDuration = 0.25f;

        private CustomizerViewFlags _currentState = CustomizerViewFlags.None;
        private CancellationTokenSource _animationCancellationToken;
        private IItemPickerDataSource _primaryItemPickerSource;
        private int _defaultNavBarPadding;

        private PictureInPictureController _PictureInPictureController => this.GetService<PictureInPictureController>();
        private VirtualCameraController<GeniesVirtualCameraCatalog> _VirtualCameraController => this.GetService<VirtualCameraController<GeniesVirtualCameraCatalog>>();

        protected override void OnInitialized()
        {
            _panel.Initialize();

            _panel.TransitionUpdated += OnTransitioning;
            _panel.TransitionStarted += OnTransitionStarted;
            _panel.TransitionEnded += OnTransitionEnded;
            PrimaryItemPicker.SourceChanged += OnSourceChanged;

            // remove any states that could be registered on the expandable panel
            for (var i = _panel.States.Count - 1; i >= 0; --i)
            {
                _panel.RemoveState(i, doTransition: false);
            }

            //Exited
            _panel.AddState(_panelExitedHeight, ExitedState, doTransition: false);
            //Hidden
            _panel.AddState(_panelHiddenHeight, HiddenState, doTransition: false);
            //Default
            _panel.AddState(_panelMinHeight, DefaultState, doTransition: false);


            _defaultNavBarPadding = _navBarLayout.padding.right;
            _backButton.onClick.AddListener(() => _customizer.GoBack(1));

            if (_galleryItemPicker.CollapseButton != null)
            {
                _galleryItemPicker.CollapseButton.onClick.AddListener(OnCollapseButtonClick);
            }
        }

        private void OnCollapseButtonClick()
        {
            _panel.SetState(DefaultState);
        }

        protected override void OnDispose()
        {
            // remove any states that could be registered on the expandable panel
            for (var i = _panel.States.Count - 1; i >= 0; --i)
            {
                _panel.RemoveState(i, doTransition: false);
            }

            _panel.TransitionUpdated -= OnTransitioning;
            _panel.TransitionStarted -= OnTransitionStarted;
            _panel.TransitionEnded -= OnTransitionEnded;
            PrimaryItemPicker.SourceChanged -= OnSourceChanged;
            _backButton.onClick.RemoveAllListeners();
        }

        private void OnTransitionEnded(int state)
        {
            var isGallery = _primaryItemPickerSource != null && (state == GalleryExpandedState || state == GalleryFullScreenState);

            _galleryItemPickerCanvas.alpha = isGallery ? 1 : 0;
            _galleryItemPickerCanvas.interactable = isGallery;

            if (!isGallery)
            {
                _galleryItemPicker.SelectionChanged -= RefreshPrimaryItemPicker;
                _galleryItemPicker.Hide();
            }

            if (state == GalleryFullScreenState)
            {
                _PictureInPictureController.Enable();
                _VirtualCameraController.SetFullScreenModeInFocusCameras(true);
            }

            var collapseButton = _galleryItemPicker.CollapseButton;

            if (collapseButton != null)
            {
                collapseButton.gameObject.SetActive(state == GalleryFullScreenState && !_panel.IsTransitioning);
            }

            _primaryItemPickerCanvas.alpha = isGallery ? 0 : 1;
            _primaryItemPickerCanvas.interactable = !isGallery;

            if (state < GalleryFullScreenState)
            {
                OnGalleryTransition(1);
            }
        }

        private void OnTransitionStarted(int fromState, int toState)
        {
            var isGallery = _primaryItemPickerSource != null && (toState == GalleryExpandedState || toState == GalleryFullScreenState);

            if (!isGallery)
            {
                return;
            }

            if (fromState == GalleryExpandedState || fromState == GalleryFullScreenState && _PictureInPictureController.IsEnabled)
            {
                _PictureInPictureController.Disable();
                _VirtualCameraController.SetFullScreenModeInFocusCameras(false);
            }

            if (!_galleryItemPicker.IsShowing ||
                _galleryItemPicker.Source != _primaryItemPickerSource)
            {
                _galleryItemPicker.SelectionChanged += RefreshPrimaryItemPicker;
                _galleryItemPicker.Show(_primaryItemPickerSource);
            }
        }

        private void OnSourceChanged(IItemPickerDataSource source)
        {
            _primaryItemPickerSource = source;

            if (_galleryItemPicker.IsShowing &&
                _galleryItemPicker.Source != _primaryItemPickerSource &&
                _primaryItemPickerSource != null)
            {
                _galleryItemPicker.Show(_primaryItemPickerSource);
            }
        }

        private void OnTransitioning(int fromState, int toState, float lerp)
        {
            if (_PictureInPictureController.IsEnabled && _PictureInPictureController.canBeDisabled)
            {
                _PictureInPictureController.Disable();
                _VirtualCameraController.SetFullScreenModeInFocusCameras(false);
            }

            //Update preview area offset
            var offsetY = _panel.Size;
            offsetY = Mathf.Clamp(offsetY, 0, _panelMaxHeight / 2f);

            var previousOffset = _previewArea.offsetMin;
            previousOffset.y = offsetY;
            _previewArea.offsetMin = previousOffset;

            if (fromState == DefaultState && toState == GalleryExpandedState)
            {
                AnimateItemPickerSwap(true, lerp);
            }
            else if (fromState == GalleryExpandedState && toState == DefaultState)
            {
                AnimateItemPickerSwap(false, lerp);
            }

            if (fromState == GalleryExpandedState && toState == GalleryFullScreenState)
            {
                OnGalleryTransition(1 - lerp * 2f);
            }
            else if (fromState == GalleryFullScreenState && toState == GalleryExpandedState)
            {
                OnGalleryTransition(lerp * 2f);
            }
        }

        private void OnGalleryTransition(float lerp)
        {
            var isInteractable = Math.Abs(lerp - 1) < 0.01f;
            _secondaryItemPickerCanvas.alpha = lerp;
            _actionBarCanvas.alpha = lerp;
            _secondaryItemPickerCanvas.interactable = isInteractable;
            _actionBarCanvas.interactable = isInteractable;
            _secondaryItemPickerCanvas.blocksRaycasts = isInteractable;
            _actionBarCanvas.blocksRaycasts = isInteractable;
        }

        private void AnimateItemPickerSwap(bool toGallery, float lerp)
        {
            float primaryPickerAlpha = 0;
            float galleryPickerAlpha = 1;

            if (!toGallery)
            {
                primaryPickerAlpha = 1;
                galleryPickerAlpha = 0;
                lerp = 1 - lerp;
            }

            _galleryItemPickerCanvas.alpha = lerp;
            _primaryItemPickerCanvas.alpha = 1 - lerp;

            if (Math.Abs(lerp - 1) < 0.05f)
            {
                _galleryItemPickerCanvas.alpha = galleryPickerAlpha;
                _primaryItemPickerCanvas.alpha = primaryPickerAlpha;
            }
        }

        /// <summary>
        /// Forces the initialization of the Gallery View.
        /// There are cases where it doesn't update properly,
        /// causing the items to not be visible.
        /// </summary>
        public override void RefreshView()
        {
            _galleryItemPicker.SelectionChanged -= RefreshPrimaryItemPicker;
            _galleryItemPicker.SelectionChanged += RefreshPrimaryItemPicker;
            if (_galleryItemPicker.gameObject.activeSelf)
            {
                _galleryItemPicker.Show(_primaryItemPickerSource);
            }
        }

        /// <summary>
        /// Forces the hiding of the Gallery View.
        /// There are some cases where it hinders the normal
        /// behaviour of the Primary Item Picker, making it
        /// necessary to force it to hide.
        /// </summary>
        public override void HideView()
        {
            _galleryItemPicker.SelectionChanged -= RefreshPrimaryItemPicker;
            _galleryItemPicker.Hide();
        }

        private void RefreshPrimaryItemPicker()
        {
            PrimaryItemPicker.RefreshSelection().Forget();
        }

        protected override void ConfigureNavigationBar(INavigationNode targetNode)
        {
            base.ConfigureNavigationBar(targetNode);

            //check if any of the buttons have icons
            var hasIcons = false;
            foreach (INavigationNode childNode in targetNode.Children)
            {
                if (childNode == null)
                {
                    continue;
                }

                hasIcons = hasIcons || (childNode.Controller.CustomizerViewConfig.icon != null);
            }

            //custom buttons too
            List<NavBarNodeButtonData> navBarOptions = targetNode.Controller.GetCustomNavBarOptions();
            if (navBarOptions != null)
            {
                foreach (var button in navBarOptions)
                {
                    hasIcons = hasIcons || (button.icon != null);
                }
            }

            //these should only change target node has children
            if (targetNode.Children.Count > 0 || navBarOptions != null)
            {
                //set y width based on icons & add button
                CustomizerViewConfig customizerViewConfig = targetNode.Config.CustomizerViewConfig;
                if (hasIcons || customizerViewConfig.showNavBarCreateNewItemCta)
                {
                    _navBarRt.sizeDelta = new Vector2(0, 58);
                }
                else
                {
                    _navBarRt.sizeDelta = new Vector2(0, 36);
                }

                //set selector position based on icons as well
                var x = _selectorRt.anchoredPosition.x;
                if (hasIcons)
                {
                    _selectorRt.anchoredPosition = new Vector2(x, 2);
                }
                else
                {
                    _selectorRt.anchoredPosition = new Vector2(x, 13.5f);
                }
            }
        }

        protected override void OnConfigureView(INavigationNode resolvedNode, INavigationNode requestedNode)
        {
            var nodeHasChildren = !_customizer.IsLeafNode(resolvedNode) || resolvedNode.Config.GetCustomNavBarOptions()?.Count > 0;
            var backButtonGo = _backButton.gameObject;
            backButtonGo.SetActive(
                                _customizer.CanGoBack() &&
                (!resolvedNode.IsStackable || nodeHasChildren) &&
                                _currentState.HasFlagFast(CustomizerViewFlags.Breadcrumbs)
                                );


            //Nav bar offset
            var navBarOffset = _navBarRect.offsetMin;
            navBarOffset.x = backButtonGo.activeSelf ? _backButtonPadding : 0;
            _navBarRect.offsetMin = navBarOffset;
            _navBarLayout.padding.right = backButtonGo.activeSelf ? _backButtonPadding : _defaultNavBarPadding;

            //Gallery offset
            var galleryOffset = _galleryRt.offsetMax;
            var topPadding = Mathf.Abs(PrimaryItemPicker.Padding.top - _galleryItemPicker.Padding.top);
            galleryOffset.y = -topPadding;
            _galleryRt.offsetMax = galleryOffset;

            _panel.gameObject.SetActive(true);

            if (!_currentState.HasFlagFast(CustomizerViewFlags.CustomizationEditor) && !_currentState.HasFlagFast(CustomizerViewFlags.NavBar))
            {
                _panel.SetState(ExitedState);
            }
            else if (!PrimaryItemPicker.IsShowing || _panel.State < GalleryExpandedState)
            {
                _panel.SetState(DefaultState);
            }

            if (PrimaryItemPicker.Source != null)
            {
                _panelHandle.gameObject.SetActive(true);

                //Extra state for dynamic state dragging.
                _panel.AddState(_panelMaxHeight / 2, GalleryExpandedState, doTransition: false);

                //Max state for dragging.
                _panel.AddState(_panelMaxHeight, GalleryFullScreenState, doTransition: false);

                //Only allow dragging between min and max states
                _panel.Lock(HiddenState, GalleryFullScreenState);
            }
            else
            {
                _panelHandle.gameObject.SetActive(false);
                _panel.RemoveState(GalleryFullScreenState, doTransition: false);
                _panel.RemoveState(GalleryExpandedState, doTransition: false);
                _panel.Lock(DefaultState, DefaultState);
            }
        }

        private async UniTask AnimateDrawerToState(int state)
        {
            _panel.SetState(state);

            while (_panel.IsTransitioning)
            {
                await UniTask.Yield();
            }
        }

        public override async UniTask AnimateOut(bool immediate = false, Color? backgroundColor = null)
        {
            _animationCancellationToken?.Cancel();
            _animationCancellationToken?.Dispose();
            _animationCancellationToken = new CancellationTokenSource();

            _currentState = CustomizerViewFlags.None;
            navBarViewComponents.TerminateAnimations();
            actionBarViewComponents.TerminateAnimations();
            customizationEditorViewComponents.TerminateAnimations();
            fullscreenCustomizationEditorViewComponents.TerminateAnimations();
            secondaryItemPickerViewComponents.TerminateAnimations();

            _panelHandle.gameObject.SetActive(false);
            _panel.Unlock();

            var targetColor = backgroundColor ?? _backgroundDefaultColor;

            await UniTask.WhenAll(
                                AnimateDrawerToState(0),
                                AnimateActionBarTask(false, immediate, _animationCancellationToken.Token),
                                AnimateCustomizationEditorTask(fullscreenCustomizationEditorViewComponents, false, immediate, token: _animationCancellationToken.Token),
                                AnimateCustomizationEditorTask(secondaryItemPickerViewComponents, false, immediate, token: _animationCancellationToken.Token),
                                AnimateBackgroundColors(targetColor, immediate)
                                );

            AnimateNavBarTask(false, true).Forget();
            AnimateCustomizationEditorTask(customizationEditorViewComponents, false, true).Forget();
            _panel.gameObject.SetActive(false);
        }

        public override UniTask GetNodeTransitionAnimation(
            CustomizerViewFlags viewFlags,
            float customizationEditorHeight = 0,
            bool showGlobalNavigation = false,
            bool navOptionsChanged = false,
            bool isGoingBack = false,
            bool immediate = false,
            Color? backgroundColor = null)
        {
            if (customizationEditorHeight > 0 && viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor))
            {
                _panel.SetStateSize(DefaultState, customizationEditorHeight + _panelMinHeight, transitionToState: false);
            }
            else
            {
                _panel.SetStateSize(DefaultState, _panelMinHeight, transitionToState: false);
            }

            _animationCancellationToken?.Cancel();
            _animationCancellationToken?.Dispose();
            _animationCancellationToken = new CancellationTokenSource();

            navBarViewComponents.TerminateAnimations();
            actionBarViewComponents.TerminateAnimations();
            customizationEditorViewComponents.TerminateAnimations();
            fullscreenCustomizationEditorViewComponents.TerminateAnimations();
            secondaryItemPickerViewComponents.TerminateAnimations();

            if (viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor))
            {
                //Set customization editor height
                var editorRt = _customizationEditorContent;
                var editorSize = editorRt.sizeDelta;
                editorSize.y = customizationEditorHeight;
                editorRt.sizeDelta = editorSize;
            }

            var targetColor = backgroundColor ?? _backgroundDefaultColor;

            var animationTasks = UniTask.WhenAll
                (
                UniTask.WaitUntil(() => !_panel.IsTransitioning),
                AnimateNavBarTask(viewFlags.HasFlagFast(CustomizerViewFlags.NavBar), immediate, isGoingBack, navOptionsChanged, _animationCancellationToken.Token),
                AnimateActionBarTask(viewFlags.HasFlagFast(CustomizerViewFlags.ActionBar), immediate, _animationCancellationToken.Token),
                AnimateCustomizationEditorTask(customizationEditorViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                AnimateCustomizationEditorTask(secondaryItemPickerViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                AnimateCustomizationEditorTask(fullscreenCustomizationEditorViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, token: _animationCancellationToken.Token),
                AnimateBackgroundColors(targetColor, immediate)
                );

            //Cache previous state
            _currentState = viewFlags;

            return animationTasks;
        }

        private async UniTask TweenCanvasGroupAlphaTask(CanvasGroup group, float alphaTarget, bool immediate)
        {
            if (immediate)
            {
                group.alpha = alphaTarget;
                return;
            }

            var settings = new AnimationSettings
            {
                Easing = _animationEase,
                AutoStart = true
            };
            await UIAnimatation.To(() => group.alpha, alpha => group.alpha = alpha, alphaTarget, _animationDuration, settings);
        }

        private async UniTask TweenRectYPivotTask(RectTransform rt, float yPivot, bool immediate)
        {
            if (immediate)
            {
                var pivot = rt.pivot;
                pivot.y = yPivot;
                rt.pivot = pivot;
                return;
            }

            // Smooth spring for natural pivot animation
            await rt.SpringPivotY(yPivot, SpringPhysics.Presets.Smooth);
        }

        public async UniTask AnimateNavBarTask(
            bool animateIn,
            bool immediate,
            bool isGoingBack = false,
            bool navOptionsChanged = false,
            CancellationToken token = default)
        {
            var isAnimatedIn = _currentState.HasFlagFast(CustomizerViewFlags.NavBar);

            var alphaTarget = animateIn ? 1 : 0;
            var yPivotTarget = animateIn ? navBarViewComponents.inYPivot : navBarViewComponents.outTopYPivot;

            var rt = navBarViewComponents.rectTransform;
            var canvasGroup = navBarViewComponents.canvasGroup;
            var pivotAnim = TweenRectYPivotTask(rt, yPivotTarget, immediate);
            var alphaAnim = TweenCanvasGroupAlphaTask(canvasGroup, alphaTarget, immediate);

            if (animateIn)
            {
                rt.gameObject.SetActive(true);
            }

            //If nothing changed. Or we just want to animate it in or out.
            if (!navOptionsChanged || !isAnimatedIn || !animateIn)
            {
                await UniTask.WhenAll(pivotAnim, alphaAnim);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                rt.gameObject.SetActive(animateIn);
                return;
            }

            //Play swap animation
            var parent = isGoingBack ? navBarViewComponents.backSwapLayer : navBarViewComponents.frontSwapLayer;
            var clone = Instantiate(rt, parent, worldPositionStays: true);
            var cloneCanvasGroup = clone.GetComponentInChildren<CanvasGroup>();
            cloneCanvasGroup.blocksRaycasts = false;
            cloneCanvasGroup.interactable = false;

            if (isGoingBack)
            {
                //immediate clone in place.
                clone.pivot = new Vector2(clone.pivot.x, navBarViewComponents.inYPivot);
                cloneCanvasGroup.alpha = 1;

                //immediate nav bar out
                rt.pivot = new Vector2(rt.pivot.x, navBarViewComponents.outTopYPivot);
                canvasGroup.alpha = 0;

                await UniTask.WhenAll(
                                    TweenRectYPivotTask(rt, navBarViewComponents.inYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(canvasGroup, 1, immediate),
                                    TweenRectYPivotTask(clone, navBarViewComponents.outBottomYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(cloneCanvasGroup, 0, immediate)
                                    );
                Destroy(clone.gameObject);
            }
            else
            {
                //immediate clone in place.
                clone.pivot = new Vector2(clone.pivot.x, navBarViewComponents.inYPivot);
                cloneCanvasGroup.alpha = 1;

                //immediate nav bar out
                rt.pivot = new Vector2(rt.pivot.x, navBarViewComponents.outBottomYPivot);
                canvasGroup.alpha = 0;

                await UniTask.WhenAll(
                                    TweenRectYPivotTask(rt, navBarViewComponents.inYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(canvasGroup, 1, immediate),
                                    TweenRectYPivotTask(clone, navBarViewComponents.outTopYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(cloneCanvasGroup, 0, immediate)
                                    );

                Destroy(clone.gameObject);
            }
        }

        private async UniTask AnimateCustomizationEditorTask(SwappableCustomizerViewComponent editorComponents, bool animateIn, bool immediate, bool isGoingBack = false, CancellationToken token = default)
        {
            var rt = editorComponents.rectTransform;
            var canvasGroup = editorComponents.canvasGroup;

            if (animateIn)
            {
                rt.gameObject.SetActive(true);
            }

            //Swap animation
            //Play swap animation
            var parent = isGoingBack ? editorComponents.backSwapLayer : editorComponents.frontSwapLayer;
            var clone = Instantiate(rt, parent, worldPositionStays: true);
            var cloneCanvasGroup = clone.GetComponentInChildren<CanvasGroup>();
            cloneCanvasGroup.blocksRaycasts = false;
            cloneCanvasGroup.interactable = false;

            //immediate clone in place.
            clone.pivot = new Vector2(clone.pivot.x, editorComponents.inYPivot);
            cloneCanvasGroup.alpha = 1;


            if (!animateIn)
            {
                rt.pivot = new Vector2(rt.pivot.x, editorComponents.inYPivot);
                canvasGroup.alpha = 0;
                await UniTask.WhenAll(
                                    TweenRectYPivotTask(clone, editorComponents.outBottomYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(cloneCanvasGroup, 0, immediate)
                                    );
            }
            else
            {
                rt.pivot = new Vector2(rt.pivot.x, editorComponents.outTopYPivot);
                canvasGroup.alpha = 0;
                await UniTask.WhenAll(
                                    TweenRectYPivotTask(rt, editorComponents.inYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(canvasGroup, 1, immediate),
                                    TweenRectYPivotTask(clone, editorComponents.outBottomYPivot, immediate),
                                    TweenCanvasGroupAlphaTask(cloneCanvasGroup, 0, immediate)
                                    );
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            Destroy(clone.gameObject);

            if (!animateIn)
            {
                rt.gameObject.SetActive(false);
            }
        }


        private async UniTask AnimateActionBarTask(bool animateIn, bool immediate, CancellationToken token = default)
        {
            var isAnimatedIn = _currentState.HasFlagFast(CustomizerViewFlags.ActionBar);

            if (isAnimatedIn && animateIn)
            {
                return;
            }

            var alphaTarget = animateIn ? 1 : 0;
            var yPivotTarget = animateIn ? 1 : 0;

            var rt = actionBarViewComponents.rectTransform;
            var canvasGroup = actionBarViewComponents.canvasGroup;
            var pivotAnim = TweenRectYPivotTask(rt, yPivotTarget, immediate);
            var alphaAnim = TweenCanvasGroupAlphaTask(canvasGroup, alphaTarget, immediate);

            if (animateIn)
            {
                rt.gameObject.SetActive(true);
            }


            await UniTask.WhenAll(
                                pivotAnim,
                                alphaAnim
                                );

            if (token.IsCancellationRequested)
            {
                return;
            }

            rt.gameObject.SetActive(animateIn);
        }

        private async UniTask AnimateBackgroundColors(Color targetColor, bool immediate)
        {
            var tasks = new List<UniTask>();

            foreach (var bg in _backgrounds)
            {
                var colorTween = bg.AnimateColor(targetColor, _animationDuration, _animationEase, immediate);
                tasks.Add(colorTween);
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
