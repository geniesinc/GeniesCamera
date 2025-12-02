using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    public class MobileButton : Button
    {
        public enum ButtonTheme
        {
            Primary = 0,
            Secondary = 1,
            Tertiary = 2
        }

        private enum ButtonState
        {
            Default = 0,
            Hover = 1,
            Selected = 2,
            Disabled = 3
        }

        public enum ButtonSize
        {
            XL = 0,
            Large = 1,
            Medium = 2
        }

        [SerializeField] private ButtonTheme buttonTheme;
        private bool _isEnabled = true;

        [SerializeField] private ButtonSize buttonSize;
        [SerializeField] private MobileButtonSizes sizes;
        public string ButtonText;

        [SerializeField] private RectTransform outerFrameTransform;
        [SerializeField] private RectTransform innerFrameTransform;
        [SerializeField] private Image outerFrameImage;
        [SerializeField] private Image innerFrameImage;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private List<MobileButtonTheme> themes;

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (_isEnabled)
            {
                ApplyTheme(ButtonState.Hover);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (_isEnabled)
            {
                ApplyTheme(ButtonState.Default);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (_isEnabled)
            {
                ApplyTheme(ButtonState.Selected);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (_isEnabled)
            {
                ApplyTheme(ButtonState.Default);
            }
        }

        public override bool IsInteractable()
        {
            _isEnabled = base.IsInteractable();
            ApplyTheme(!_isEnabled ? ButtonState.Disabled : ButtonState.Default);
            return _isEnabled;
        }

        /// <summary>
        /// This changes the size of the 'innerFrame'
        /// Making this smaller, increases the size of the outline
        /// </summary>
        private void UpdateOutline(float pixelWidth)
        {
            innerFrameTransform.sizeDelta = outerFrameTransform.sizeDelta - new Vector2(pixelWidth, pixelWidth);
        }

        private void ApplyTheme(ButtonState state)
        {
            var _currentTheme = themes[(int)buttonTheme];

            switch (state)
            {
                case ButtonState.Default:
                    innerFrameImage.color = _currentTheme.defaultInnerFrame;
                    outerFrameImage.color = _currentTheme.defaultOuterFrame;
                    text.color = _currentTheme.defaultTextColor;
                    UpdateOutline(_currentTheme.defaultOutlineWidth);
                    break;
                case ButtonState.Hover:
                    innerFrameImage.color = _currentTheme.hoverInnerFrame;
                    outerFrameImage.color = _currentTheme.hoverOuterFrame;
                    text.color = _currentTheme.hoverTextColor;
                    UpdateOutline(_currentTheme.hoverOutlineWidth);
                    break;
                case ButtonState.Selected:
                    innerFrameImage.color = _currentTheme.selectedInnerFrame;
                    outerFrameImage.color = _currentTheme.selectedOuterFrame;
                    text.color = _currentTheme.selectedTextColor;
                    UpdateOutline(_currentTheme.selectedOutlineWidth);
                    break;
                case ButtonState.Disabled:
                    innerFrameImage.color = _currentTheme.disabledInnerFrame;
                    outerFrameImage.color = _currentTheme.disabledOuterFrame;
                    text.color = _currentTheme.disabledTextColor;
                    UpdateOutline(_currentTheme.disabledOutlineWidth);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ApplySize(ButtonSize size)
        {
            switch (size)
            {
                case ButtonSize.XL:
                    outerFrameTransform.sizeDelta = sizes.XL;
                    innerFrameTransform.sizeDelta = sizes.XL;
                    break;
                case ButtonSize.Large:
                    outerFrameTransform.sizeDelta = sizes.Large;
                    innerFrameTransform.sizeDelta = sizes.Large;
                    break;
                case ButtonSize.Medium:
                    outerFrameTransform.sizeDelta = sizes.Medium;
                    innerFrameTransform.sizeDelta = sizes.Medium;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

        /// <summary>
        /// This method is called by the inspector only
        /// We can switch between the Primary, Secondary, and Tertiary themes.
        /// </summary>
        public void UpdateButton()
        {
            ApplySize(buttonSize);
            ApplyTheme(_isEnabled ? ButtonState.Default : ButtonState.Disabled);

            text.text = ButtonText;
        }
    }
}