using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;
using Genies.UI.Scroller;
using Genies.UIFramework;
using Genies.UI.Animations;

namespace Genies.UI.Components.Widgets
{
    public class ScrollableToggleBar : MonoBehaviour
    {
        [SerializeField]
        private List<GeniesButton> _buttons;

        [SerializeField]
        private RectTransform _barTransform;

        [SerializeField]
        private RectTransform _selector;

        [SerializeField]
        private ScrollRect _scrollRect;

        [SerializeField]
        private RectTransform _buttonsRoot;

        [SerializeField]
        private ContentSizeFitter _contentSizeFitter;

        public float SelectorAnimationDuration = 0.17f;
        public bool InitializeOnLaunch = true;

        public event Action<bool> Displaying;

        public int MaxButtonsToEnableScrolling = 6;
        public bool AutoResizeSelector;

        #region Logic

        private const int DefaultIndex = -1;

        private GeniesButton _prevClicked;
        private int _prevIndex = DefaultIndex;
        private int _lastSelectedIndex = DefaultIndex;
        public bool IsShown { get; private set; }

        private bool _initialized = false;
        private bool _blockInput = false;

        public List<GeniesButton> Buttons => _buttons;
        public int LastSelectedIndex => _lastSelectedIndex;
        protected IShowHideAnimation _showHideAnimation;

        public bool BlockInput
        {
            get => _blockInput;
            set
            {
                _blockInput = value;

                foreach (var button in _buttons)
                {
                    button.SetButtonEnabled(!_blockInput, true);
                }
            }
        }

        protected void Awake()
        {
            InitTransition();

            if (InitializeOnLaunch)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            InitTransition();
            CheckUI();
            BindButtons();
            CheckEnableScrolling();
            _initialized = true;
        }

        public void Dispose()
        {
            _selector.Terminate();
            _prevClicked = null;
            _prevIndex = -1;
            _selector.gameObject.SetActive(false);
            _initialized = false;
        }

        public async UniTask SetButtons(List<GeniesButton> buttons)
        {
            Dispose();

            _buttons = buttons;
            foreach (var button in buttons)
            {
                button.transform.SetParent(_buttonsRoot);
                button.SetButtonSelected(false);
            }

            // Ensures the newly added buttons are positioned as they should (i.e. within a HorizontalLayoutGroup).
            // Downstream processes may use the button positions to align certain UI elements before the Canvas hierarchy has a chance to automatically update.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonsRoot);

            // Wait a frame before initializing to avoid race conditions between switching buttons
            // and animating the selector from the last click
            await UniTask.Yield();

            Initialize();
        }


        private void InitTransition()
        {
            _showHideAnimation ??= GetComponent<IShowHideAnimation>();
        }

        private void BindButtons()
        {
            var buttonCount = 0;
            foreach (var button in _buttons)
            {
                var buttonIndex = buttonCount;
                button.onClick.AddListener(() => OnButtonClick(buttonIndex));
                buttonCount++;
            }
        }

        private void CheckEnableScrolling()
        {
            if (_buttons.Count >= MaxButtonsToEnableScrolling)
            {
                _scrollRect.enabled = true;
                _contentSizeFitter.enabled = true;
                _scrollRect.horizontalNormalizedPosition = 0;
            }
            else
            {
                _scrollRect.enabled = false;
                _contentSizeFitter.enabled = false;
                _buttonsRoot.sizeDelta = new Vector2(_barTransform.sizeDelta.x, _buttonsRoot.sizeDelta.y);
                _buttonsRoot.anchoredPosition = Vector2.zero;
            }
        }

        private void CheckUI()
        {
#if UNITY_EDITOR
            Debug.Assert(_buttons != null, "Buttons are not set");
            Debug.Assert(_selector != null, "Selector is not set");
            Debug.Assert(_buttons.Count > 0, "Buttons are not set");
            Debug.Assert(_scrollRect, "_scrollRect is not set");
            Debug.Assert(_contentSizeFitter, "_contentSizeFitter is not set");
#endif
        }

        private void OnButtonClick(int index)
        {
            SetSelected(index);
        }

        public async void SetSelected(int index)
        {
            //Don't animate if not initialized
            if (!_initialized)
            {
                return;
            }

            if (index < 0 || index > _buttons.Count - 1)
            {
                return;
            }

            if (_prevIndex == index)
            {
                _prevClicked.SetButtonSelected(true);
            }

            if (_prevClicked != null)
            {
                _prevClicked.SetButtonSelected(false);
            }

            _lastSelectedIndex = index;
            _prevIndex = index;
            _prevClicked = _buttons[index];
            _prevClicked.SetButtonSelected(true);

            var isImmediateAnimation = !_selector.gameObject.activeSelf; // Cache _selector active state since `PlaySelectorAnimation` changes it.

            // Wait for customizer to update content rect transform
            await UniTask.Yield(PlayerLoopTiming.Update);
            PlaySelectorAnimation(index, isImmediateAnimation);
            AnimateScrollRect(index, isImmediateAnimation);
        }
        private void PlaySelectorAnimation(int selectedIndex, bool immediate = false)
        {
            _selector.Terminate();

            var button = _buttons[selectedIndex];
            var buttonRect = button.transform as RectTransform;
            var position = _contentSizeFitter.gameObject.transform.InverseTransformPoint(buttonRect.position);

            _selector.gameObject.SetActive(true);
            if (!immediate)
            {
                _selector.AnimateLocalMoveX(position.x, SelectorAnimationDuration);
            }
            else
            {
                var prevPos = _selector.transform.localPosition;
                _selector.localPosition = new Vector3(position.x, prevPos.y, prevPos.z);
            }

            if (AutoResizeSelector)
            {
                var scaleX = buttonRect.sizeDelta.x / _selector.sizeDelta.x;
                if (!immediate)
                {
                    _selector.AnimateScaleX(scaleX, SelectorAnimationDuration);
                }
                else
                {
                    var prevScale = _selector.localScale;
                    _selector.localScale = new Vector3(scaleX, prevScale.y, prevScale.z);
                }
            }
        }

        private void AnimateScrollRect(int selectedIndex, bool immediate = false)
        {
            if (!_scrollRect.isActiveAndEnabled)
            {
                return;
            }

            var button = _buttons[selectedIndex];
            var buttonRect = button.transform as RectTransform;
            var normalizedPos = _scrollRect.normalizedPosition;

            if (_scrollRect.vertical)
            {
                var targetNormalizedYPos = _scrollRect.GetScrollToCenterNormalizedPosition(buttonRect);
                normalizedPos.y = targetNormalizedYPos;
            }
            else
            {
                var targetNormalizedXPos =
                    _scrollRect.GetScrollToCenterNormalizedPosition(buttonRect, RectTransform.Axis.Horizontal);
                normalizedPos.x = targetNormalizedXPos;
            }

            if (!immediate)
            {
                _scrollRect.AnimateNormalizedPos(normalizedPos, SelectorAnimationDuration).SetEase(Ease.InOutSine).Play();
            }
            else
            {
                _scrollRect.normalizedPosition = normalizedPos;
            }
        }

        public void Hide()
        {
            if (!IsShown)
            {
                return;
            }

            IsShown = false;
            _showHideAnimation?.Hide();
            InvokeDisplaying();
        }

        public void Show()
        {
            Initialize();
            if (IsShown)
            {
                return;
            }

            IsShown = true;
            gameObject.SetActive(true);
            _showHideAnimation?.Show();
            InvokeDisplaying();
            _selector.gameObject.SetActive(false);
        }

        protected void InvokeDisplaying()
        {
            Displaying?.Invoke(IsShown);
        }

        #endregion
    }
}
