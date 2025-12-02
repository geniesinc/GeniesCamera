using System.Collections.Generic;
using Genies.CrashReporting;
using Genies.UI.Animations;
using Genies.UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    public class NavBar : Widget
    {
        private const string _iconName = "Icon";

        [SerializeField]
        private Image _slider;
        [SerializeField]
        private float _animationDuration = 0.17f;
        [SerializeField]
        private List<GeniesButton> _navBarButtons;
        [SerializeField]
        private Color _inactiveIconColor = new Color32(255, 255, 255, 75);
        [SerializeField]
        private Color _activeIconColor = new Color32(255, 255, 255, 255);
        [SerializeField]
        private Color _inactiveSliderColor = new Color32(255, 255, 255, 75);
        [SerializeField]
        private Color _activeSliderColor = new Color32(255, 255, 255, 255);
        public List<GeniesButton> NavBarButtons => _navBarButtons;
        private List<Image> _icons = new();
        private bool _isSliderActive;

        public override void OnWidgetInitialized()
        {
            CheckUI();
            GetIcons();
            BindButtons();
            ResetHighlightedIcons();
            HighlightIcon(_navBarButtons.Count - 1);
        }

        private void BindButtons()
        {
            var buttonCount = 0;
            foreach (var button in _navBarButtons)
            {
                var buttonIndex = buttonCount;
                button.onClick.AddListener(() => OnButtonClick(buttonIndex));
                buttonCount++;
            }
        }

        private void GetIcons()
        {
            if (_icons.Count == 0)
            {
                foreach (var button in _navBarButtons)
                {
                    var icon = button.transform.Find(_iconName);
                    if (icon != null)
                    {
                        _icons.Add(icon.GetComponent<Image>());
                    }
                    else
                    {
                        CrashReporter.LogError($"Icon for button {button.name} not found");
                    }
                }
            }
        }

        protected void CheckUI()
        {
#if UNITY_EDITOR
            Debug.Assert(_slider != null,   "_slider is not set");
            Debug.Assert(_navBarButtons != null,   "_navBarButtons is not set");
#endif
        }

        private void ResetHighlightedIcons()
        {
            foreach (var icon in _icons)
            {
                icon.color = _inactiveIconColor;
            }
        }

        private void HighlightIcon(int index)
        {
            _icons[index].color = _activeIconColor;
        }

        public void SetSliderActive(bool isActive)
        {
            if(_isSliderActive == isActive)
            {
                return;
            }

            _isSliderActive = isActive;
            _slider.AnimateColor(_isSliderActive ? _activeSliderColor : _inactiveSliderColor, _animationDuration);
        }

        protected virtual void OnButtonClick(int index)
        {
            SelectButton(index);
        }

        public void SelectButton(int index)
        {
            _slider.AnimateColor(_activeSliderColor, _animationDuration);
            var button = _navBarButtons[index];

            if (_navBarButtons != null &&
                _navBarButtons.Contains(button))
            {
                ResetHighlightedIcons();
                HighlightIcon(index);
                var buttonRT = (button.transform as RectTransform);
                var sliderRT = (_slider.transform as RectTransform);
                if (buttonRT != null && sliderRT != null)
                {
                    sliderRT.AnimateAnchorPosX(buttonRT.anchoredPosition.x, _animationDuration);
                }
            }
        }
    }
}
