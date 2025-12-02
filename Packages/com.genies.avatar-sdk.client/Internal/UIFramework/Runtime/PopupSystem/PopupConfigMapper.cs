using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UMI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    public class PopupConfigMapper : MonoBehaviour
    {
        [SerializeField] private PopupLayout _popupLayout;

        [SerializeField] private TextMeshProUGUI _header;

        [SerializeField] private TMP_Text _content;

        [SerializeField] private TMP_InputField _inputFieldTextContent;

        [SerializeField] private TMP_Text _inputFieldTextPlaceholder;

        [SerializeField] private Image _topImage;

        [SerializeField] private PopupBlocker _popupBlocker;

        [SerializeField] private Image _backgroundBlocker;

        [SerializeField] private GeniesButton _closeButton;

        [SerializeField] private List<OutlineButton> _actionButtons;

        [Header("Mobile Input Settings")]
        [SerializeField] private InputContentType _inputContentType;
        private MobileInputField _mobileInputField;

        private UnityAction _closeAction;

        private string _debugMessage;

        public TMP_InputField InputFieldText => _inputFieldTextContent;

        private bool _isKeyboardShown = false;
        private bool _pendingHideKeyboard = false;

        private int _blockerDelayMiliseconds = 500;
        private bool _blockerDelayInProgress = false;

        private void Awake()
        {
            CheckUI();
            MobileInput.OnKeyboardAction += OnKeyboardAction;
        }

        private void OnDestroy()
        {
            MobileInput.OnKeyboardAction -= OnKeyboardAction;
        }

        /// <summary>
        /// Directly sets all UI values of the popup, as well as the actions for the buttons.
        /// </summary>
        /// <param name="config">A popup config for configuring the UI</param>
        /// <param name="closeAction">The action to take for closing this popup</param>
        /// <param name="actions">Nullable ordered list of actions for each button</param>
        public void Setup(PopupConfig config, UnityAction closeAction, List<UnityAction> actions = null)
        {
            if (config == null)
            {
                return;
            }

            _header.text = config.Header;

            if (_header != null && !string.IsNullOrWhiteSpace(config.Header))
            {
                if(config.FallBackFont != null)
                {
                    _header.font = config.FallBackFont;
                }
                _header.text = config.Header;
                _header.gameObject.SetActive(true);
            }
            else
            {
                _header.gameObject.SetActive(false);
            }

            if (_content != null && !string.IsNullOrWhiteSpace(config.Content))
            {
                if(config.FallBackFont != null)
                {
                    _content.font = config.FallBackFont;
                }
                _content.text = config.Content;
                _content.gameObject.SetActive(true);
            }
            else
            {
                _content.gameObject.SetActive(false);
            }

            if (_inputFieldTextPlaceholder != null)
            {
                if(config.FallBackFont != null)
                {
                    _inputFieldTextPlaceholder.font = config.FallBackFont;
                }
                _inputFieldTextPlaceholder.text = config.PlaceholderInputFieldText;
                _inputFieldTextContent.shouldHideMobileInput = true;
                _mobileInputField = _inputFieldTextContent.gameObject.GetComponent<MobileInputField>();
                SetupMobileInputField();
            }

            if (config.TopImage != null)
            {
                _topImage.gameObject.SetActive(true);
                _topImage.sprite = config.TopImage;
            }
            else
            {
                _topImage.gameObject.SetActive(false);
            }

            // Clear old listeners
            ClearListeners();

            // Apply button labels
            ApplyButtonLabels(config, config.FallBackFont);

            _closeAction = closeAction;

            // Apply button actions
            ApplyButtonActions(actions, _closeAction);

            // Setup close button
            SetupCloseButton(config.HasCloseButton, _closeAction);

            // Setup Popup Blocker
            if (_popupBlocker != null)
            {
                _popupBlocker.OnBlockerClicked += HandleBlockerTapped;
            }
        }

        /// <summary>
        /// Public method for checking if this popup's layout matches the given one.
        /// </summary>
        /// <param name="layout">The layout to compare against</param>
        /// <returns>Whether they match or not</returns>
        public bool IsOfLayout(PopupLayout layout)
        {
            return _popupLayout == layout;
        }

        /// <summary>
        /// Activates or deactivates the background blocker for this specific popup
        /// </summary>
        /// <param name="activate"></param>
        public void ActivateBackgroundBlocker(bool activate)
        {
            _backgroundBlocker.GetComponent<Image>().enabled = activate;
            _backgroundBlocker.gameObject.SetActive(activate);
        }

        /// <summary>
        /// Clears all this popup's listeners
        /// </summary>
        public void ClearListeners()
        {
            for (int i = 0; i < _actionButtons.Count; i++)
            {
                _actionButtons[i].OnClick.RemoveAllListeners();
            }

            _closeButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Handles Blocker tapped input
        /// </summary>
        private void HandleBlockerTapped()
        {
            if (_blockerDelayInProgress)
            {
                //delay is in progress, ignore input and return
                return;
            }

            if (CanHideKeyboard())
            {
                //hide native keyboard and activate blocker delay
                HideKeyboard();
                DelayBlocker().Forget();
            }
            else
            {
                //hide popup
                HideMobileInputField();
                _closeAction?.Invoke();
            }
        }

        /// <summary>
        /// Activates a flag and disables it after a delay
        /// </summary>
        private async UniTask DelayBlocker()
        {
            _blockerDelayInProgress = true;
            await UniTask.Delay(_blockerDelayMiliseconds);
            _blockerDelayInProgress = false;
        }

        /// <summary>
        /// Checks if native keyboard is currently active and can be hidden
        /// </summary>
        /// <returns></returns>
        private bool CanHideKeyboard()
        {
            if (_mobileInputField == null)
            {
                return false;
            }
            return _pendingHideKeyboard;
        }

        /// <summary>
        /// Hides virtual mobile input keyboard
        /// </summary>
        private void HideKeyboard()
        {
            if (_mobileInputField != null)
            {
                //hide virtual keyboard
                _mobileInputField.SetFocus(false);
                _pendingHideKeyboard = false;
            }
        }

        /// <summary>
        /// Hides virtual mobile input keyboard
        /// </summary>
        private void HideMobileInputField()
        {
            if (_mobileInputField != null)
            {
                //hide virtual keyboard
                _mobileInputField.SetFocus(false);
                _mobileInputField.SetVisible(false);
                _pendingHideKeyboard = false;
            }
        }

        /// <summary>
        /// Sets Mobile Input Field from TMP_InputField in this PopoupConfigMapper
        /// </summary>
        private void SetupMobileInputField()
        {
            if (_mobileInputField == null || _inputFieldTextContent == null)
            {
                return;
            }

            var color = _inputFieldTextContent.GetComponent<Image>().color;
            _mobileInputField.SetBackgroundColor(color);
            _mobileInputField.SetContentType(_inputContentType);
            _mobileInputField.InputField.pointSize = _inputFieldTextContent.pointSize;
            _mobileInputField.InputField.fontAsset = _inputFieldTextContent.fontAsset;

        }

        private void OnKeyboardAction(bool isShown, int height)
        {
            _isKeyboardShown = isShown;
            if (_isKeyboardShown)
            {
                _pendingHideKeyboard = true;
            }
        }

        private void ApplyButtonLabels(PopupConfig config, TMP_FontAsset fallbackFont = null)
        {
            List<string> buttonLabels = config.ButtonLabels;
            var popupStyle = config.Style;
            for (int i = 0; i < _actionButtons.Count; i++)
            {
                if (buttonLabels.Count >= i + 1)
                {
                    PopupButtonStyle buttonStyle = null;

                    if (popupStyle != null && popupStyle.ButtonStyles != null)
                    {
                        //check if we have a style available for this button
                        var styles = popupStyle.ButtonStyles;
                        buttonStyle = styles.Count > i ? styles[i] : null;
                    }

                    //If we have a matching label, display it
                    ApplyButtonLabel(_actionButtons[i], buttonLabels[i], buttonStyle, fallbackFont);
                }
                else
                {
                    //If no label, deactivate it
                    _actionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void ApplyButtonActions(List<UnityAction> actions, UnityAction closeAction)
        {
            for (int i = 0; i < _actionButtons.Count; i++)
            {
                // Apply new actions
                if (actions != null && actions.Count >= i + 1)
                {
                    _actionButtons[i].OnClick.AddListener(actions[i]);
                }

                // Any button click also force-closes the popup
                if (closeAction != null)
                {
                    _actionButtons[i].OnClick.AddListener(closeAction);
                }

                _actionButtons[i].OnClick.AddListener(HideMobileInputField);
            }
        }

        private void ApplyButtonLabel(OutlineButton button, string label, PopupButtonStyle style = null, TMP_FontAsset fallBackFont = null)
        {
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (fallBackFont != null)
            {
                buttonText.font = fallBackFont;
            }
            buttonText.text = label;

            style ??= PopupButtonStyle.Default;

            //set default state so button defaults to custom style
            button.DefaultState.TextColor = style.TextColor;
            button.DefaultState.ButtonColor = style.BackgroundColor;

            //set button text and background colour to custom style
            buttonText.color = style.TextColor;
            button.ButtonImage.color = style.BackgroundColor;
        }

        private void SetupCloseButton(bool active, UnityAction closeAction)
        {
            _closeButton.gameObject.SetActive(active);
            if (active)
            {
                _closeButton.onClick.AddListener(closeAction);
            }
        }

        private void CheckUI()
        {

#if UNITY_EDITOR
            Debug.Assert(_header != null, "_header is not set!");
            Debug.Assert(_content != null, "_content is not set!");
            Debug.Assert(_topImage != null, "_topImage is not set!");
            Debug.Assert(_closeButton != null, "_closeButton is not set!");
            Debug.Assert(_actionButtons != null, "_actionButtons is not set!");
#endif
        }

    }
}
