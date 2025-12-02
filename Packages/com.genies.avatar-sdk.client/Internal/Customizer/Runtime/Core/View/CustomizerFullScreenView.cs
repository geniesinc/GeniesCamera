using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework.Navigation;
using Genies.UI.Animations;
using Genies.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework
{
    /// <summary>
    /// The main customizer view, handles animations and setting up different view components
    /// based on the node being navigated to.
    /// </summary>
    public class CustomizerFullScreenView : CustomizerViewBase
    {
        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Breadcrumbs _breadCrumbs;

        [Header("Preview View")]
        [SerializeField]
        private RectTransform _previewAreaTransform;

        [SerializeField]
        private RectTransform _previewAreaFullScreen;

        [Header("Backgrounds")]
        [SerializeField]
        private CustomizerBackgroundElement[] _backgrounds;

        [Header("Customizer Views")]
        [SerializeField]
        private SwappableCustomizerViewComponent navBarViewComponents;

        [SerializeField]
        private CustomizerViewComponents actionBarViewComponents;

        [SerializeField]
        private CustomizerViewComponents breadCrumbsViewComponents;

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
        private Ease _animationEase;

        [SerializeField]
        private float _animationDuration;

        private CustomizerViewFlags _previousState = CustomizerViewFlags.None;
        private CancellationTokenSource _animationCancellationToken;

        protected override void OnInitialized()
        {
            _backButton.onClick.AddListener(() => _customizer.GoBack(1));
        }

        protected override void OnDispose()
        {
            _backButton.onClick.RemoveAllListeners();
        }

        protected override void OnConfigureView(INavigationNode resolvedNode, INavigationNode requestedNode)
        {
            _backButton.interactable = _customizer.CanGoBack();
            ConfigureBreadcrumbs(requestedNode);
        }

        /// <summary>
        /// Configure and animate the breadcrumbs
        /// </summary>
        /// <param name="targetNode"> Node we're targeting </param>
        private void ConfigureBreadcrumbs(INavigationNode targetNode)
        {
            CustomizerViewConfig config = targetNode.Controller.CustomizerViewConfig;

            if (!config.customizerViewFlags.HasFlagFast(CustomizerViewFlags.Breadcrumbs))
            {
                return;
            }

            if (!targetNode.IsStackable)
            {
                return;
            }

            _breadCrumbs.ClearBreadcrumbs(false);

            var                         counter     = 0;
            IReadOnlyCollection<string> breadcrumbs = _customizer.GetBreadCrumbs();
            foreach (var breadCrumb in breadcrumbs)
            {
                var breadCrumbIndex = counter;
                _breadCrumbs.SetBreadcrumb(
                                           new SimpleBreadcrumb(
                                                                breadCrumb, breadCrumb, () =>
                                                                {
                                                                    var distance = _customizer.GetBreadCrumbCount() - 1 - breadCrumbIndex;
                                                                    _customizer.GoBack(distance);
                                                                }
                                                               )
                                          );

                counter++;
            }

            _breadCrumbs.RebuildUI();
        }

        public override async UniTask AnimateOut(bool immediate = false, Color? backgroundColor = null)
        {
            _animationCancellationToken?.Cancel();
            _animationCancellationToken?.Dispose();
            _animationCancellationToken = new CancellationTokenSource();

            _previousState = CustomizerViewFlags.None;
            _previewAreaTransform.Terminate();
            navBarViewComponents.TerminateAnimations();
            actionBarViewComponents.TerminateAnimations();
            breadCrumbsViewComponents.TerminateAnimations();
            customizationEditorViewComponents.TerminateAnimations();

            var targetColor = backgroundColor ?? _backgroundDefaultColor;

            await UniTask.WhenAll(
                                  AnimateNavBarTask(false, immediate, token: _animationCancellationToken.Token),
                                  AnimateActionBarTask(false, immediate, _animationCancellationToken.Token),
                                  AnimateBreadCrumbsTask(false, immediate, _animationCancellationToken.Token),
                                  AnimateCustomizationEditorTask(customizationEditorViewComponents,           false, immediate, token: _animationCancellationToken.Token),
                                  AnimateCustomizationEditorTask(fullscreenCustomizationEditorViewComponents, false, immediate, token: _animationCancellationToken.Token),
                                  AnimateCustomizationEditorTask(secondaryItemPickerViewComponents,           false, immediate, token: _animationCancellationToken.Token),
                                  AnimatePreviewAreaTask(0, 0, immediate),
                                  AnimateBackgroundColors(targetColor, immediate)
                                 );
        }

        /// <summary>
        /// Animate a customization editor in.
        /// </summary>
        /// <param name="viewFlags"> Which views are enabled for this customization </param>
        /// <param name="customizationEditorHeight"> The height of the customization editor view </param>
        /// <param name="showGlobalNavigation"> Pass true if global navigation should be enabled for view </param>
        /// <param name="navOptionsChanged"> Pass true if the nav bar has new options to show </param>
        /// <param name="isGoingBack"> If the animation should target going back to previous editors </param>
        /// <param name="immediate"> If the animation should finish immediately </param>
        /// <param name="backgroundColor"> The background color of the editor </param>
        public override UniTask GetNodeTransitionAnimation(
            CustomizerViewFlags viewFlags,
            float customizationEditorHeight = 0,
            bool showGlobalNavigation = false,
            bool navOptionsChanged = false,
            bool isGoingBack = false,
            bool immediate = false,
            Color? backgroundColor = null)
        {
            _animationCancellationToken?.Cancel();
            _animationCancellationToken?.Dispose();
            _animationCancellationToken = new CancellationTokenSource();

            _previewAreaTransform.Terminate();
            navBarViewComponents.TerminateAnimations();
            actionBarViewComponents.TerminateAnimations();
            breadCrumbsViewComponents.TerminateAnimations();
            customizationEditorViewComponents.TerminateAnimations();

            var targetColor = backgroundColor ?? _backgroundDefaultColor;
            var previewMinY = 0f;
            var previewMaxY = 0f;


            if (viewFlags.HasFlagFast(CustomizerViewFlags.Breadcrumbs))
            {
                previewMinY += breadCrumbsViewComponents.Height;
            }

            if (viewFlags.HasFlagFast(CustomizerViewFlags.ActionBar))
            {
                previewMaxY -= actionBarViewComponents.Height;
            }

            if (viewFlags.HasFlagFast(CustomizerViewFlags.NavBar))
            {
                previewMinY += navBarViewComponents.Height;
            }

            if (viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor))
            {
                //Set customization editor height
                var editorRt   = customizationEditorViewComponents.rectTransform;
                var editorSize = editorRt.sizeDelta;
                editorSize.y = customizationEditorHeight;
                editorRt.sizeDelta = editorSize;

                previewMinY += customizationEditorHeight;
            }

            var animationTasks = UniTask.WhenAll
                (
                 AnimateNavBarTask(viewFlags.HasFlagFast(CustomizerViewFlags.NavBar), immediate, isGoingBack, navOptionsChanged, _animationCancellationToken.Token),
                 AnimateActionBarTask(viewFlags.HasFlagFast(CustomizerViewFlags.ActionBar), immediate, _animationCancellationToken.Token),
                 AnimateBreadCrumbsTask(viewFlags.HasFlagFast(CustomizerViewFlags.Breadcrumbs), immediate, _animationCancellationToken.Token),
                 AnimateCustomizationEditorTask(customizationEditorViewComponents,           viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                 AnimateCustomizationEditorTask(fullscreenCustomizationEditorViewComponents, viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                 AnimateCustomizationEditorTask(secondaryItemPickerViewComponents,           viewFlags.HasFlagFast(CustomizerViewFlags.CustomizationEditor), immediate, isGoingBack, _animationCancellationToken.Token),
                 AnimatePreviewAreaTask(previewMinY, previewMaxY, immediate),
                 AnimateBackgroundColors(targetColor, immediate)
                );


            //Cache previous state
            _previousState = viewFlags;

            return animationTasks;
        }

        private async UniTask TweenCanvasGroupAlphaTask(CanvasGroup group, float alphaTarget, bool immediate)
        {
            if (immediate)
            {
                group.alpha = alphaTarget;
                return;
            }

            await UIAnimatation.To(() => group.alpha, alpha => group.alpha = alpha, alphaTarget, _animationDuration).SetEase(_animationEase);
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
            await rt.SpringPivotY(yPivot, SpringPhysics.Presets.Smooth).Play();
        }

        private async UniTask AnimateNavBarTask(
            bool animateIn,
            bool immediate,
            bool isGoingBack = false,
            bool navOptionsChanged = false,
            CancellationToken token = default)
        {
            var isAnimatedIn = _previousState.HasFlagFast(CustomizerViewFlags.NavBar);

            var alphaTarget  = animateIn ? 1 : 0;
            var yPivotTarget = animateIn ? navBarViewComponents.inYPivot : navBarViewComponents.outTopYPivot;

            var rt          = navBarViewComponents.rectTransform;
            var canvasGroup = navBarViewComponents.canvasGroup;
            var pivotAnim   = TweenRectYPivotTask(rt, yPivotTarget, immediate);
            var alphaAnim   = TweenCanvasGroupAlphaTask(canvasGroup, alphaTarget, immediate);

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
            var parent           = isGoingBack ? navBarViewComponents.backSwapLayer : navBarViewComponents.frontSwapLayer;
            var clone            = Instantiate(rt, parent, worldPositionStays: true);
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
                                      TweenCanvasGroupAlphaTask(canvasGroup, 1, immediate)
                                     );
                Destroy(clone.gameObject);
            }
            else
            {
                //immediate clone in place.
                clone.pivot = new Vector2(clone.pivot.x, navBarViewComponents.inYPivot);
                cloneCanvasGroup.alpha = 1;

                //immediate nav bar in
                rt.pivot = new Vector2(rt.pivot.x, navBarViewComponents.outTopYPivot);
                canvasGroup.alpha = 1;

                await UniTask.WhenAll(
                                      TweenRectYPivotTask(clone, 1, immediate),
                                      TweenCanvasGroupAlphaTask(cloneCanvasGroup, navBarViewComponents.outTopYPivot, immediate)
                                     );

                Destroy(clone.gameObject);
            }
        }

        private async UniTask AnimateCustomizationEditorTask(SwappableCustomizerViewComponent editorComponents, bool animateIn, bool immediate, bool isGoingBack = false, CancellationToken token = default)
        {
            var isAnimatedIn = _previousState.HasFlagFast(CustomizerViewFlags.CustomizationEditor);
            var isSwap       = isAnimatedIn && animateIn;

            var alphaTarget  = animateIn ? 1 : 0;
            var yPivotTarget = animateIn ? editorComponents.inYPivot : editorComponents.outTopYPivot;

            var rt          = editorComponents.rectTransform;
            var canvasGroup = editorComponents.canvasGroup;
            var pivotAnim   = TweenRectYPivotTask(rt, yPivotTarget, immediate);
            var alphaAnim   = TweenCanvasGroupAlphaTask(canvasGroup, alphaTarget, immediate);

            if (animateIn)
            {
                rt.gameObject.SetActive(true);
            }

            //Regular animation
            if (!isSwap)
            {
                await UniTask.WhenAll(pivotAnim, alphaAnim);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                rt.gameObject.SetActive(animateIn);
                return;
            }


            //Swap animation
            //Play swap animation
            var parent           = isGoingBack ? editorComponents.backSwapLayer : editorComponents.frontSwapLayer;
            var clone            = Instantiate(rt, parent, worldPositionStays: true);
            var cloneCanvasGroup = clone.GetComponentInChildren<CanvasGroup>();
            cloneCanvasGroup.blocksRaycasts = false;
            cloneCanvasGroup.interactable = false;

            //immediate clone in place.
            clone.pivot = new Vector2(clone.pivot.x, editorComponents.inYPivot);
            cloneCanvasGroup.alpha = 1;

            //immediate editor out
            rt.pivot = new Vector2(rt.pivot.x, editorComponents.outTopYPivot);
            canvasGroup.alpha = 0;

            await UniTask.WhenAll(
                                  TweenRectYPivotTask(rt, editorComponents.inYPivot, immediate),
                                  TweenCanvasGroupAlphaTask(canvasGroup, 1, immediate),
                                  TweenRectYPivotTask(clone, editorComponents.outTopYPivot, immediate),
                                  TweenCanvasGroupAlphaTask(cloneCanvasGroup, 0, immediate)
                                 );

            Destroy(clone.gameObject);
        }

        private async UniTask AnimateBreadCrumbsTask(bool animateIn, bool immediate, CancellationToken token = default)
        {
            var isAnimatedIn = _previousState.HasFlagFast(CustomizerViewFlags.Breadcrumbs);

            if (isAnimatedIn && animateIn)
            {
                return;
            }

            var alphaTarget = animateIn ? 1 : 0;

            var rt          = breadCrumbsViewComponents.rectTransform;
            var canvasGroup = breadCrumbsViewComponents.canvasGroup;
            var alphaAnim   = TweenCanvasGroupAlphaTask(canvasGroup, alphaTarget, immediate);

            if (animateIn)
            {
                rt.gameObject.SetActive(true);
            }

            await UniTask.WhenAll(
                                  alphaAnim
                                 );

            if (token.IsCancellationRequested)
            {
                return;
            }

            rt.gameObject.SetActive(animateIn);
        }

        private async UniTask AnimateActionBarTask(bool animateIn, bool immediate, CancellationToken token = default)
        {
            var isAnimatedIn = _previousState.HasFlagFast(CustomizerViewFlags.ActionBar);

            if (isAnimatedIn && animateIn)
            {
                return;
            }

            var alphaTarget  = animateIn ? 1 : 0;
            var yPivotTarget = animateIn ? 1 : 0;

            var rt          = actionBarViewComponents.rectTransform;
            var canvasGroup = actionBarViewComponents.canvasGroup;
            var pivotAnim   = TweenRectYPivotTask(rt, yPivotTarget, immediate);
            var alphaAnim   = TweenCanvasGroupAlphaTask(canvasGroup, alphaTarget, immediate);

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

        private async UniTask AnimatePreviewAreaTask(float previewMinY, float previewMaxY, bool immediate)
        {
            var targetMinOffset = _previewAreaFullScreen.offsetMin;
            var targetMaxOffset = _previewAreaFullScreen.offsetMax;
            targetMinOffset.y += previewMinY;
            targetMaxOffset.y += previewMaxY;

            if (immediate)
            {
                _previewAreaTransform.offsetMin = targetMinOffset;
                _previewAreaTransform.offsetMax = targetMaxOffset;
                return;
            }

            var minOffsetTween = UIAnimatation.To(() => _previewAreaTransform.offsetMin, newOffset => _previewAreaTransform.offsetMin = newOffset, targetMinOffset, _animationDuration);
            var maxOffsetTween = UIAnimatation.To(() => _previewAreaTransform.offsetMax, newOffset => _previewAreaTransform.offsetMax = newOffset, targetMaxOffset, _animationDuration);

            await UniTask.WhenAll(
                                  minOffsetTween.Play().ToUniTask(),
                                  maxOffsetTween.Play().ToUniTask()
                                 );
        }
    }
}
