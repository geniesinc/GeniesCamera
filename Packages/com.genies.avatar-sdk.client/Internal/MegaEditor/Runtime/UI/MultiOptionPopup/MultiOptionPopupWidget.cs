using System;
using System.Collections.Generic;
using Genies.Customization.Framework.Actions;
using Genies.Looks.MultiOptionPopup.Scripts;
using Genies.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Build a Multi Option Drawer widget. Requires initialization
/// </summary>
///
namespace Genies.MegaEditor
{
    public class MultiOptionPopupWidget : MonoBehaviour
    {
        [SerializeField] protected RectTransform popupRectTransform;
        [SerializeField] protected TextMeshProUGUI headerText, descriptionText;
        [SerializeField] protected GameObject buttonPrefab;
        [SerializeField] protected GameObject dividerPrefab;

        [SerializeField] protected Color disabledColor = new Color(.3f, .3f, .3f, 1f);
        [SerializeField] protected Color enabledColor = new Color(10.0f / 255.0f, 132.0f / 255.0f, 1, 1);

        [SerializeField]
        protected Color riskOptionEnabledColor = new Color(182.0f / 255.0f, 65.0f / 255.0f, 54.0f / 255.0f, 1);

        protected RectTransform _popupParentRectTransform;
        protected List<MultiOptionButton> _multiOptionButtons;

        protected List<GameObject>
            _optionGameObjects; // to destroy every time we set new options, includes the dividers

        [SerializeField] private SlidingPopupWidget _slidingPopupWidget;
        protected bool _isInitialized;

        public event Action CloseButtonClicked
        {
            add { _slidingPopupWidget.CloseClicked += value; }
            remove { _slidingPopupWidget.CloseClicked -= value; }
        }

        private void Awake()
        {
            Initialize();
        }

        protected void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _multiOptionButtons = new List<MultiOptionButton>();
            _optionGameObjects = new List<GameObject>();
            _popupParentRectTransform = popupRectTransform.parent.GetComponent<RectTransform>();
            _isInitialized = true;
        }

        public void SetHeader(string headerText, string descriptionText = "")
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            this.headerText.text = headerText;
            if (this.descriptionText != null)
            {
                this.descriptionText.text = descriptionText;
            }
        }

        /// <summary>
        /// Creates a list of buttons based off the 'options' input
        /// </summary>
        /// <param name="options"> each option will create its own respective button. </param>
        public virtual void SetOptions(List<ActionDrawerOption> options)
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

                // Any button click updates all buttons
                multiOptionButton.button.onClick.AddListener(_slidingPopupWidget.Hide);


                // set colors for button
                multiOptionButton.SetColors(option.riskOption ? riskOptionEnabledColor : enabledColor, disabledColor);

                _multiOptionButtons.Add(multiOptionButton);
                _optionGameObjects.Add(multiOptionButton.gameObject);
            }

            UpdateButtons();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_popupParentRectTransform);
        }

        /// <summary>
        /// Update whether or not the buttons are interactable
        /// This also changes the text color.
        /// </summary>
        public virtual void UpdateButtons()
        {
            foreach (var button in _multiOptionButtons)
            {
                button.UpdateButton();
            }
        }
    }
}