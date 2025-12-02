using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.CameraSystem;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Customization.Framework.Navigation;
using Genies.Customization.MegaEditor;
using Genies.ServiceManagement;
using Genies.UI;
using Genies.UI.Animations;
using Genies.UIFramework.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Looks.View
{
    // Refactored version of CustomizerDrawerView to match Genies Party's drawer design layout
    public class GPCustomizerDrawerView : CustomizerViewBase
    {
        private const int ExitedState = 0;
        private const int HiddenState = 1;
        private const int DefaultState = 2;
        private const int GalleryFullScreenState = 3;

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

        [SerializeField][Tooltip("Drawer bottom margin to use when there's no global navigation visible")]
        private float _drawerMinBottomMargin = 8f;

        [SerializeField]
        private RectTransform _navBarRect;

        [SerializeField]
        private HorizontalLayoutGroup _navBarLayout;

        [SerializeField]
        private RectTransform _navBarRt;

        [SerializeField]
        private ScrollRect _navBarScrollRt;

        [SerializeField]
        private RectTransform _selectorRt;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private RectTransform _backButtonRect;

        [SerializeField]
        private TextMeshProUGUI _navBarHeaderText;

        [SerializeField]
        private RectTransform _navBarHeaderRect;

        [SerializeField]
        private CanvasGroup _actionBarCanvas;

        [SerializeField]
        private CanvasGroup _secondaryItemPickerCanvas;

        [SerializeField]
        private RectTransform _customizationEditorContent;

        [SerializeField]
        private RectTransform _previewArea;

        [SerializeField]
        private ExpandablePanel _panel;

        [SerializeField]
        private RectTransform _panelRect;

        [SerializeField]
        private GameObject _panelHandle;

        [SerializeField]
        private RectTransform _globalNavBar;

        [SerializeField]
        private RectTransform _topPanelEdge;

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

        // TODO: test this out
        private GalleryItemPicker _primaryGalleryItemPicker => PrimaryItemPicker as GalleryItemPicker;
        private CustomizerViewFlags _currentState = CustomizerViewFlags.None;
        private CancellationTokenSource _animationCancellationToken;
        private int _defaultNavBarPadding;
        private float _defaultBackButtonY;
        private bool _isGlobalNavBarVisible;
        private const float _navHeaderHeightPadding = 30f;
        private const float _navSpacing = 14f;
        private const float _navSpacingNoIcon = 0f;

        private PictureInPictureController _PictureInPictureController => this.GetService<PictureInPictureController>();
        private VirtualCameraController<GeniesVirtualCameraCatalog> _VirtualCameraController => this.GetService<VirtualCameraController<GeniesVirtualCameraCatalog>>();

        protected override void OnInitialized()
        {
            _panel.Initialize();

            _panel.TransitionUpdated += OnTransitioning;
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
            _defaultBackButtonY = _backButtonRect.anchoredPosition.y;
            _backButton.onClick.AddListener(() => _customizer.GoBack(1));
        }
        protected override void OnDispose()
        {
            // remove any states that could be registered on the expandable panel
            for (var i = _panel.States.Count - 1; i >= 0; --i)
            {
                _panel.RemoveState(i, doTransition: false);
            }

            _panel.TransitionUpdated -= OnTransitioning;
            _panel.TransitionEnded -= OnTransitionEnded;
            PrimaryItemPicker.SourceChanged -= OnSourceChanged;
            _backButton.onClick.RemoveAllListeners();
        }

        private void OnTransitionEnded(int state)
        {
            if (state == GalleryFullScreenState)
            {
                if (_PictureInPictureController != null)
                {
                    _PictureInPictureController.Enable();
                }

                _VirtualCameraController.SetFullScreenModeInFocusCameras(true);
            }
            else
            {
                if (_PictureInPictureController != null)
                {
                    _PictureInPictureController.Disable();
                }

                _VirtualCameraController.SetFullScreenModeInFocusCameras(false);
            }

            if (state < GalleryFullScreenState)
            {
                OnGalleryTransition(1);
            }
        }

        private void OnSourceChanged(IItemPickerDataSource dataSource)
        {
            if (dataSource != null)
            {
                _primaryGalleryItemPicker.SetGridLayoutCellSize(dataSource);
            }

            if (PrimaryItemPicker.Source != null || _customizer.CurrentNode.Config.CustomizationController is
                    FaceVectorCustomizationController or FaceCustomizationController or BodyShapeCustomizationController or BodyVectorCustomizationController)
            {
                _panelHandle.gameObject.SetActive(true);

                //Max state for dragging.
                _panel.AddState(_panelMaxHeight, GalleryFullScreenState, doTransition: false);

                //Only allow dragging between min and max states
                _panel.Lock(HiddenState, GalleryFullScreenState);
            }
            else
            {
                _panelHandle.gameObject.SetActive(false);
                _panel.RemoveState(GalleryFullScreenState, doTransition: false);
                _panel.Lock(DefaultState, DefaultState);
            }

            if (!_currentState.HasFlagFast(CustomizerViewFlags.CustomizationEditor) && !_currentState.HasFlagFast(CustomizerViewFlags.NavBar))
            {
                _panel.SetState(ExitedState);
            }
            else if (!PrimaryItemPicker.IsShowing || _panel.State < GalleryFullScreenState)
            {
                _panel.SetState(DefaultState);
            }
        }

        private void OnTransitioning(int fromState, int toState, float lerp)
        {
            //Update preview area offset
            var halfHeightTopPanelEdge = _topPanelEdge.rect.height * 0.5f;
            var offsetY = _panel.Size + halfHeightTopPanelEdge;

            if (_isGlobalNavBarVisible)
            {
                offsetY += _globalNavBar.rect.height;
            }

            offsetY = Mathf.Clamp(offsetY, 0, _panelMaxHeight * 0.5f + halfHeightTopPanelEdge);

            var previousOffset = _previewArea.offsetMin;
            previousOffset.y = offsetY;
            _previewArea.offsetMin = previousOffset;

            if (fromState == DefaultState && toState == GalleryFullScreenState)
            {
                OnGalleryTransition(1 - lerp * 2f);
            }
            else if (fromState == GalleryFullScreenState && toState == DefaultState)
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

                hasIcons = hasIcons || (childNode.Config.CustomizerViewConfig.icon != null);
            }

            if (!_customizer.IsLeafNode(targetNode))
            {
                // if navigation components are only texts, minimize spacing and align to left
                _navBarLayout.spacing = hasIcons ? _navSpacing : _navSpacingNoIcon;
                _navBarScrollRt.content.pivot = hasIcons ? new Vector2(0.5f, 0.5f) : new Vector2(0, 0.5f);
            }

            //custom buttons too
            List<NavBarNodeButtonData> navBarOptions = targetNode.Config.GetCustomNavBarOptions();
            if (navBarOptions != null)
            {
                foreach (var button in navBarOptions)
                {
                    hasIcons = hasIcons || (button.icon != null);
                }
            }

            CustomizerViewConfig customizerViewConfig = targetNode.Config.CustomizerViewConfig;
            //these should only change target node has children
            if (targetNode.Children.Count > 0 || navBarOptions != null)
            {
                //set y width based on icons & add button
                if (hasIcons || customizerViewConfig.showNavBarCreateNewItemCta)
                {
                    _navBarRt.sizeDelta = new Vector2(0, 58 + (customizerViewConfig.showHeaderText? _navHeaderHeightPadding : 0));
                }
                else
                {
                    _navBarRt.sizeDelta = new Vector2(0, 36 + (customizerViewConfig.showHeaderText? _navHeaderHeightPadding : 0));
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
            else
            {
                if (customizerViewConfig.showHeaderText && !_navBarHeaderText.gameObject.activeSelf)
                {
                    _navBarRt.sizeDelta += new Vector2(0, _navHeaderHeightPadding);
                }
                else if (!customizerViewConfig.showHeaderText && _navBarHeaderText.gameObject.activeSelf)
                {
                    _navBarRt.sizeDelta -= new Vector2(0, _navHeaderHeightPadding);
                }
            }

            _navBarHeaderText.gameObject.SetActive(customizerViewConfig.showHeaderText);
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

            //Apply adjusted layout based on header and Back button usage
            if (backButtonGo.activeSelf)
            {
                var useHeaderLayout = requestedNode.Config.CustomizerViewConfig.showHeaderText;
                navBarOffset.x = !useHeaderLayout ? _backButtonPadding : 0;
                _navBarRect.offsetMin = navBarOffset;
                _navBarLayout.padding.right = !useHeaderLayout ? _backButtonPadding : _defaultNavBarPadding;
                //Align back button to the header
                LayoutRebuilder.ForceRebuildLayoutImmediate(_navBarRt);
                _backButtonRect.anchoredPosition = new Vector2(_backButtonRect.anchoredPosition.x, !useHeaderLayout ? _defaultBackButtonY : _navBarHeaderRect.anchoredPosition.y);
            }
            else
            {
                navBarOffset.x = 0;
                _navBarRect.offsetMin = navBarOffset;
                _navBarLayout.padding.right = _defaultNavBarPadding;
                _panel.gameObject.SetActive(true);
            }

            if (requestedNode.Config.CustomizerViewConfig.showHeaderText)
            {
                _navBarHeaderText.text = requestedNode.Config.CustomizerViewConfig.headerText;
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
                                AnimateBackgroundColors(targetColor, immediate),
                                AnimateDrawerYOffset(0, immediate),
                                AnimateGlobalNavBarYOffset(-_globalNavBar.rect.height, immediate)
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
                _panel.SetStateSize(DefaultState,  Mathf.Max(customizationEditorHeight, _panelMinHeight), transitionToState: false);
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

            var targetColor = backgroundColor ?? _backgroundDefaultColor;
            var targetCustomizerYOffset = showGlobalNavigation ? _globalNavBar.rect.height : _drawerMinBottomMargin;
            _isGlobalNavBarVisible = showGlobalNavigation;

            var animationTasks = UniTask.WhenAll
                (
                UniTask.WaitUntil(() => !_panel.IsTransitioning),
                AnimateNavBarTask(viewFlags.HasFlagFast(CustomizerViewFlags.NavBar), immediate, isGoingBack, navOptionsChanged, _animationCancellationToken.Token),
                AnimateActionBarTask(viewFlags.HasFlagFast(CustomizerViewFlags.ActionBar), immediate, _animationCancellationToken.Token),
                AnimateCustomizationEditorTask(customizationEditorViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                AnimateCustomizationEditorTask(secondaryItemPickerViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                AnimateCustomizationEditorTask(fullscreenCustomizationEditorViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, token: _animationCancellationToken.Token),
                AnimateBackgroundColors(targetColor, immediate),
                AnimateDrawerYOffset(targetCustomizerYOffset, immediate),
                AnimateGlobalNavBarYOffset(showGlobalNavigation? 0 : -_globalNavBar.rect.height, immediate)
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

            var settings = new AnimationSettings
            {
                Easing = _animationEase,
                AutoStart = true
            };
            await rt.AnimatePivotY(yPivot, _animationDuration, settings);
        }

        private async UniTask TweenRectYAnchoredPositionTask(RectTransform rt, float yPosition, bool immediate)
        {
            if (immediate)
            {
                var anchoredPosition = rt.anchoredPosition;
                anchoredPosition.y = yPosition;
                rt.anchoredPosition = anchoredPosition;
                return;
            }

            var settings = new AnimationSettings
            {
                Easing = _animationEase,
                AutoStart = true
            };
            await rt.AnimateAnchorPosY(yPosition, _animationDuration, settings);
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

            var rt = actionBarViewComponents.rectTransform;
            var canvasGroup = actionBarViewComponents.canvasGroup;
            if (isAnimatedIn && animateIn && rt.pivot.y >= 1 && canvasGroup.alpha >= 1)
            {
                return;
            }

            var alphaTarget = animateIn ? 1 : 0;
            var yPivotTarget = animateIn ? 1 : 0;

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

        private async UniTask AnimateDrawerYOffset(float offset, bool immediate)
        {
            await TweenRectYAnchoredPositionTask(_panelRect, offset, immediate);
        }

        private async UniTask AnimateGlobalNavBarYOffset(float offset, bool immediate)
        {
            await TweenRectYAnchoredPositionTask(_globalNavBar, offset, immediate);
        }
    }
}
