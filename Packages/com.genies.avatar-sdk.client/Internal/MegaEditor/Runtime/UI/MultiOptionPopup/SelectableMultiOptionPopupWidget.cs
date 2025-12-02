using System;
using System.Collections.Generic;
using Genies.Customization.Framework.Actions;
using Genies.MegaEditor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.Looks.MultiOptionPopup.Scripts
{
    public class SelectableMultiOptionPopupWidget : MultiOptionPopupWidget
    {
        [Header("UI Items")]
        [SerializeField] private Color _selectedTextColor = new Color(0.33f, 0.35f, 0.92f);
        [SerializeField] private Color _unselectedTextColor = new Color(0.24f, 0.24f, 0.24f);

        [Header("DropShadowPrefab")]
        [SerializeField] protected GameObject _dropShadow;
        [FormerlySerializedAs("dropShadowAdditionalHeight")] [SerializeField] protected float _dropShadowAdditionalHeight = 40f;

        [Header("Optional Bottom Link")]
        [SerializeField] protected GameObject _linkButtonPrefab;
        [SerializeField] private TextMeshProUGUI _bottomText;
        [SerializeField] private Button _bottomLink;

        private SelectableMultiOptionButton _previousButton;
        private SelectableMultiOptionButton _selectedButton;

        public void SetOptions(List<ActionDrawerOption> options, LinkButton linkButton)
        {
            SetOptions(options);

            if (linkButton != null)
            {
                _linkButtonPrefab.transform.SetParent(popupRectTransform);
                _bottomText.text = linkButton.DisplayName;
                _bottomLink.onClick.AddListener(linkButton.OnClick.Invoke);
                _linkButtonPrefab.SetActive(true);
            }
        }



        /// <summary>
        /// Creates a list of buttons based off the 'options' input
        /// </summary>
        /// <param name="options"> each option will create its own respective button. </param>
        public override void SetOptions(List<ActionDrawerOption> options)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // TODO: pooling
            foreach (GameObject gameObject in _optionGameObjects)
            {
                Destroy(gameObject);
            }

            _multiOptionButtons.Clear();
            _optionGameObjects.Clear();

            for (var i = 0; i < options.Count; i++)
            {
                ActionDrawerOption option = options[i];

                if (i != 0)
                {
                    // add a divider for each button
                    var divider = Instantiate(dividerPrefab, popupRectTransform, false);
                    divider.transform.localScale = new Vector3(1, 1, 1);
                    _optionGameObjects.Add(divider);
                }

                // Button
                var prefab = Instantiate(buttonPrefab, popupRectTransform, false);
                var multiOptionButton = prefab.GetComponent<MultiOptionButton>();
                multiOptionButton.Initialize(option);

                // Any click triggers selection
                multiOptionButton.button.onClick.AddListener(SetSelection(multiOptionButton).Invoke);

                // set colors for button
                multiOptionButton.SetColors(option.riskOption ? riskOptionEnabledColor : enabledColor, disabledColor);

                _multiOptionButtons.Add(multiOptionButton);
                _optionGameObjects.Add(multiOptionButton.gameObject);
            }

            UpdateButtons();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_popupParentRectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRectTransform);

            CopyRectTransformSizeDelta(popupRectTransform, _dropShadow.GetComponent<RectTransform>(), _dropShadowAdditionalHeight);
        }

        private UnityAction SetSelection(MultiOptionButton multiOptionButton)
        {
            var selectableMultiOptionButton = multiOptionButton as SelectableMultiOptionButton;
            if (selectableMultiOptionButton == null)
            {
                Debug.LogError("MultiOptionButton is not of type SelectableMultiOptionButton.");
                return null;
            }

            return () =>
            {
                if (_selectedButton != null)
                {
                    _selectedButton.SetSelected(false);
                    _selectedButton.SetColorButton(_unselectedTextColor);
                }

                // Update the references
                _selectedButton = selectableMultiOptionButton;

                // Select the current button
                _selectedButton.SetSelected(true);
                _selectedButton.SetColorButton(_selectedTextColor);
            };
        }

        private void CopyRectTransformSizeDelta(RectTransform sourceRectTransform, RectTransform targetRectTransform, float additionalHeight = 0f)
        {
            if (sourceRectTransform != null && targetRectTransform != null)
            {
                // Copy only the height of the source RectTransform
                targetRectTransform.sizeDelta = new Vector2(targetRectTransform.sizeDelta.x, sourceRectTransform.sizeDelta.y + additionalHeight);
            }
            else
            {
                Debug.LogError("Source or target RectTransform is null.");
            }
        }
    }

    public class LinkButton
    {
        public readonly string DisplayName;
        public readonly Action OnClick;

        public LinkButton(string displayName, Action action)
        {
            this.DisplayName = displayName;
            OnClick = action;
        }
    }
}
