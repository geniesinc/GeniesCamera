using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Genies.Login.Native.Editor
{
    public class GeniesLoginEditorWindow : EditorWindow
    {
        private GeniesEditorLoginController _Controller;
        private LoginStateInfo _LoginState;
        private string _EmailKey;

        private string _PlaceHolderEmail = "someuser@gmail.com";
        private string _EmailInput;
        private string _VerificationCode;
        private TextField _EmailField;
        private TextField _VerificationCodeField;
        private VisualElement _RootContainer;
        private Button _SendButton;
        private Button _VerifyButton;

        /// <summary>
        /// Shows the Genies Login window with an external controller.
        /// The window will use the controller's existing state.
        /// </summary>
        public static GeniesLoginEditorWindow ShowWindow(GeniesEditorLoginController controller, string emailKey)
        {
            var window = GetWindow<GeniesLoginEditorWindow>("Genies Login");
            window.minSize = new Vector2(400, 300);
            window._Controller = controller;
            window._LoginState = controller.LoginStateInfo;

            window._EmailKey = emailKey;
            window.Initialize();
            return window;
        }

        /// <summary>
        /// Shows the Genies Login window in standalone mode.
        /// The window will create its own controller internally.
        /// </summary>
        public static GeniesLoginEditorWindow ShowWindow(string apiPath, string clientName = "GeniesEditorLogin", string emailKey = "GeniesLogin-Email")
        {
            var controller = new GeniesEditorLoginController(apiPath, clientName, emailKey);
            var window = GetWindow<GeniesLoginEditorWindow>("Genies Login");
            window.minSize = new Vector2(400, 300);
            window._Controller = controller;
            window._LoginState = controller.LoginStateInfo;
            window._EmailKey = emailKey;
            window.Initialize();
            return window;
        }

        private void Initialize()
        {
            _LoginState = _Controller.LoginStateInfo;

            var savedEmail = EditorPrefs.GetString(_EmailKey);
            _EmailInput = string.IsNullOrEmpty(savedEmail) ? _PlaceHolderEmail : savedEmail;

            _LoginState.Updated += OnStateUpdated;
            _LoginState.Changed += OnStateChanged;

            // Try instant login
            _ = _Controller.InitializeAndTryInstantLogin();

            CreateGUI();
        }

        private void OnStateChanged()
        {
            // Update UI on any state change
            UpdateUI();
        }

        private void OnStateUpdated(LoginState previous, LoginState newState)
        {
            if (previous != LoginState.EnterCode)
            {
                _VerificationCode = "";
            }

            // Close window on successful login
            if (newState == LoginState.LoggedIn)
            {
                EditorApplication.delayCall += () => { Close(); };
            }
            else
            {
                // Refresh UI for other state changes
                UpdateUI();
            }
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            _RootContainer = new VisualElement();
            _RootContainer.style.paddingTop = 10;
            _RootContainer.style.paddingBottom = 10;
            _RootContainer.style.paddingLeft = 10;
            _RootContainer.style.paddingRight = 10;
            _RootContainer.style.flexGrow = 1;

            rootVisualElement.Add(_RootContainer);

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_RootContainer == null || _LoginState == null)
            {
                return;
            }

            _RootContainer.Clear();

            // Handle different UI based on login state
            switch (_LoginState.State)
            {
                case LoginState.EnterEmail:
                    CreateEmailInput(_RootContainer);
                    break;
                case LoginState.EnterCode:
                    CreateVerificationCodeInput(_RootContainer);
                    break;
                case LoginState.LoggedIn:
                    // Window will close
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CreateEmailInput(VisualElement parent)
        {
            var label = new Label("Login with your Genies account");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 10;
            parent.Add(label);

            var linkButton = new Button(() => Application.OpenURL(GeniesLoginSdk.UrlGeniesHubSignUp));
            linkButton.text = "Don't have an account? Sign up here";
            linkButton.style.marginBottom = 20;
            linkButton.style.color = new Color(0.4f, 0.6f, 1f);
            parent.Add(linkButton);

            // Email input field with placeholder handling


            _SendButton = new Button(() =>
                {
                    _LoginState.SetError(null); // refresh for any new errors that may appear
                    if (_EmailField.value != _PlaceHolderEmail)
                    {
                        _EmailInput = _EmailField.value;
                        SaveEmail();
                        _Controller.SubmitEmailAsync(_EmailInput);
                    }
                }
            );
            _SendButton.text = "Send Verification Code";
            _SendButton.style.marginTop = 10;
            _EmailField = new TextField("Email:");
            _EmailField.RegisterCallback<FocusInEvent>(evt =>
                {
                    if (_EmailField.value == _PlaceHolderEmail)
                    {
                        _EmailField.value = "";
                        _EmailField.style.color = Color.white;
                    }
                }
            );

            _EmailField.RegisterCallback<FocusOutEvent>(evt =>
                {
                    if (string.IsNullOrEmpty(_EmailField.value))
                    {
                        _EmailField.value = _PlaceHolderEmail;
                        _EmailField.style.color = new Color(0.5f, 0.5f, 0.5f);
                    }
                }
            );

            _EmailField.RegisterValueChangedCallback(evt =>
                {
                    _EmailInput = evt.newValue;
                    OnEmailUpdated();
                }
            );

            OnEmailUpdated();
            parent.Add(_EmailField);
            parent.Add(_SendButton);

            if (_LoginState.AwaitingLoginResponse)
            {
                var helpBox = new HelpBox("Please wait, checking login status...", HelpBoxMessageType.Info);
                helpBox.style.marginTop = 10;
                parent.Add(helpBox);
            }

            if (!string.IsNullOrEmpty(_LoginState.OptionalError) && !_LoginState.AwaitingLoginResponse)
            {
                var errorBox = new HelpBox(_LoginState.OptionalError, HelpBoxMessageType.Error);
                errorBox.style.marginTop = 10;
                parent.Add(errorBox);
            }
        }

        private void OnEmailUpdated()
        {
            var isPlaceholder = string.IsNullOrEmpty(_EmailInput) || _EmailInput == _PlaceHolderEmail;
            if (isPlaceholder)
            {
                _EmailField.style.color = new Color(0.5f, 0.5f, 0.5f);
            }

            _EmailField.SetValueWithoutNotify(isPlaceholder ? _PlaceHolderEmail : _EmailInput);
            var canSend = !isPlaceholder &&
                          !string.IsNullOrWhiteSpace(_EmailInput) &&
                          !_LoginState.AwaitingLoginResponse &&
                          _EmailInput != _PlaceHolderEmail;

            _SendButton.SetEnabled(canSend);
        }

        private void CreateVerificationCodeInput(VisualElement parent)
        {
            var label = new Label("Enter the verification code you received to your email");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 10;
            parent.Add(label);

            _VerificationCodeField = new TextField("Verification Code");
            _VerificationCodeField.value = _VerificationCode;

            _VerifyButton = new Button(() =>
                {
                    _LoginState.ResetError();
                    _Controller.SubmitOtpCodeAsync(_EmailInput, _VerificationCode);
                }
            );

            _VerifyButton.text = "Verify Code";
            _VerifyButton.style.marginTop = 10;
            _VerifyButton.SetEnabled(!string.IsNullOrEmpty(_VerificationCode) && !_LoginState.AwaitingVerification && !_LoginState.AwaitingResendOtp);

            _VerificationCodeField.RegisterValueChangedCallback(evt =>
                {
                    _VerificationCode = evt.newValue;

                    // Update verify button enabled state when verification code changes
                    _VerifyButton.SetEnabled(!string.IsNullOrEmpty(_VerificationCode) && !_LoginState.AwaitingVerification && !_LoginState.AwaitingResendOtp);
                }
            );

            // Auto-focus the verification code field
            parent.schedule.Execute(() => { _VerificationCodeField?.Focus(); }).StartingIn(100);

            parent.Add(_VerificationCodeField);
            parent.Add(_VerifyButton);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginTop = 10;

            var resendButton = new Button(() =>
                {
                    _LoginState.ResetError();
                    _Controller.ResendCode(_EmailInput);
                }
            );
            resendButton.text = "Resend Code";
            resendButton.style.flexGrow = 1;
            resendButton.style.marginRight = 5;
            resendButton.SetEnabled(!_LoginState.AwaitingResendOtp && !_LoginState.AwaitingVerification);
            buttonRow.Add(resendButton);

            var returnButton = new Button(() => { _LoginState.SetState(LoginState.EnterEmail); });
            returnButton.text = "Back";
            returnButton.style.flexGrow = 1;
            returnButton.SetEnabled(!_LoginState.AwaitingResendOtp && !_LoginState.AwaitingVerification);
            buttonRow.Add(returnButton);

            parent.Add(buttonRow);

            if (_LoginState.AwaitingVerification)
            {
                var helpBox = new HelpBox("Please wait, verifying...", HelpBoxMessageType.Info);
                helpBox.style.marginTop = 10;
                parent.Add(helpBox);
            }

            if (!string.IsNullOrEmpty(_LoginState.OptionalError) && !_LoginState.AwaitingVerification)
            {
                var errorBox = new HelpBox(_LoginState.OptionalError, HelpBoxMessageType.Error);
                errorBox.style.marginTop = 10;
                parent.Add(errorBox);
            }
        }

        private void SaveEmail()
        {
            EditorPrefs.SetString(_EmailKey, _EmailInput);
        }

        private void OnDestroy()
        {
            if (_LoginState != null)
            {
                _LoginState.Updated -= OnStateUpdated;
                _LoginState.Changed -= OnStateChanged;
            }

            // Notify controller that window is destroyed
            _Controller?.OnWindowDestroyed();
        }
    }
}
