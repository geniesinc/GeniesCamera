using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Sdk.Samples.MultipleAvatars
{
    /// <summary>
    /// Simplified login script for the MultipleAvatars sample.
    /// Uses scene UI buttons for authentication functionality.
    /// </summary>
    internal class MultipleAvatarsLoginExample : MonoBehaviour
    {
        [Header("Login Configuration")]
        [Tooltip("Automatically attempt instant login on start?")]
        [SerializeField] private bool _autoInstantLogin = true;

        [Header("UI References")]
        [Tooltip("Status text to display login information")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [Tooltip("Input field for email address")]
        [SerializeField] private TMP_InputField _emailInputField;
        [Tooltip("Input field for OTP code")]
        [SerializeField] private TMP_InputField _otpInputField;

        [Header("Login Buttons")]
        [SerializeField] private Button _submitEmailButton;
        [SerializeField] private Button _submitOtpButton;
        [SerializeField] private Button _resendCodeButton;
        [SerializeField] private Button _signUpButton;

        private string _pendingEmail = "";
        private bool _isInitialized = false;

        private void Awake()
        {
            SubscribeToLoginEvents();
            WireButtons();
        }

        private async void Start()
        {
            UpdateStatus("Initializing...");

            if (!_isInitialized)
            {
                Debug.Log("Initializing AvatarSdk...");
                await AvatarSdk.InitializeAsync();
                _isInitialized = true;
                Debug.Log("AvatarSdk initialized successfully.");
                UpdateStatus("Ready for login.");
            }

            if (_autoInstantLogin)
            {
                await TryInstantLogin();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromLoginEvents();
            UnwireButtons();
        }

        #region Event Subscription

        private void SubscribeToLoginEvents()
        {
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded += OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed += OnEmailCodeRequestFailed;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded += OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed += OnEmailCodeSubmissionFailed;
        }

        private void UnsubscribeFromLoginEvents()
        {
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded -= OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed -= OnEmailCodeRequestFailed;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded -= OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed -= OnEmailCodeSubmissionFailed;
        }

        #endregion

        #region Button Wiring

        private void WireButtons()
        {
            if (_submitEmailButton != null)
            {
                _submitEmailButton.onClick.RemoveAllListeners();
                _submitEmailButton.onClick.AddListener(() => _ = StartEmailOtpLogin());
            }

            if (_submitOtpButton != null)
            {
                _submitOtpButton.onClick.RemoveAllListeners();
                _submitOtpButton.onClick.AddListener(() => _ = SubmitOtpCodeFromInput());
            }

            if (_resendCodeButton != null)
            {
                _resendCodeButton.onClick.RemoveAllListeners();
                _resendCodeButton.onClick.AddListener(() => _ = ResendEmailCode());
            }

            if (_signUpButton != null)
            {
                _signUpButton.onClick.RemoveAllListeners();
                _signUpButton.onClick.AddListener(GoToSignUpPage);
            }
        }

        private void UnwireButtons()
        {
            _submitEmailButton?.onClick.RemoveAllListeners();
            _submitOtpButton?.onClick.RemoveAllListeners();
            _resendCodeButton?.onClick.RemoveAllListeners();
            _signUpButton?.onClick.RemoveAllListeners();
        }

        #endregion

        #region UI Helper Methods

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
            Debug.Log($"Status: {message}");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_signUpButton != null)
            {
                _signUpButton.interactable = interactable;
            }

            if (_submitEmailButton != null)
            {
                _submitOtpButton.interactable = interactable;
            }

            if (_submitOtpButton != null)
            {
                _submitOtpButton.interactable = interactable;
            }

            if (_resendCodeButton != null)
            {
                _resendCodeButton.interactable = interactable;
            }
        }

        private void SetInputFieldsInteractable(bool interactable)
        {
            if (_emailInputField != null)
            {
                _emailInputField.interactable = interactable;
            }

            if (_otpInputField != null)
            {
                _otpInputField.interactable = interactable;
            }
        }

        private void SetProcessing(bool processing, string message = "Processing...")
        {
            SetButtonsInteractable(!processing);
            SetInputFieldsInteractable(!processing);
            if (processing)
            {
                UpdateStatus(message);
            }
        }

        #endregion

        #region Public Button Methods

        private async System.Threading.Tasks.Task TryInstantLogin()
        {
            try
            {
                SetProcessing(true, "Attempting instant login...");

                var result = await AvatarSdk.TryInstantLoginAsync();

                if (result.isLoggedIn)
                {
                    Debug.Log($"Instant login successful!");
                    UpdateStatus($"Logged in");
                }
                else
                {
                    Debug.Log("Instant login not available - no cached credentials available.");
                    UpdateStatus($"Instant login not available, please login");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Instant login error: {ex.Message}");
                UpdateStatus($"Instant login error: {ex.Message}");
            }
            finally
            {
                SetProcessing(false);
            }
        }

        private async System.Threading.Tasks.Task StartEmailOtpLogin()
        {
            var email = _emailInputField?.text?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                UpdateStatus("Please enter an email address first!");
                return;
            }

            try
            {
                SetProcessing(true, $"Sending code to {email}...");
                Debug.Log($"Starting email OTP login for: {email}");

                _pendingEmail = email;
                await AvatarSdk.StartLoginEmailOtpAsync(email);
                Debug.Log("Email OTP request sent. Check your email for the verification code.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start email OTP login: {ex.Message}");
                UpdateStatus($"Email OTP error: {ex.Message}");
                SetProcessing(false);
            }
        }

        private async System.Threading.Tasks.Task SubmitOtpCodeFromInput()
        {
            var otpCode = _otpInputField?.text?.Trim();
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                UpdateStatus("Please enter the OTP code first!");
                return;
            }

            await SubmitOtpCode(otpCode);
        }

        private async System.Threading.Tasks.Task ResendEmailCode()
        {
            if (!AvatarSdk.IsAwaitingEmailOtpCode)
            {
                UpdateStatus("No pending OTP request. Start email login first.");
                return;
            }

            try
            {
                SetProcessing(true, "Resending verification code...");
                Debug.Log("Resending email verification code...");

                await AvatarSdk.ResendEmailCodeAsync();
                Debug.Log("Email code resent successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resend email code: {ex.Message}");
                UpdateStatus($"Resend error: {ex.Message}");
                SetProcessing(false);
            }
        }

        private void GoToSignUpPage()
        {
            Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Submit an OTP code programmatically.
        /// </summary>
        /// <param name="otpCode">The OTP code received via email</param>
        public async System.Threading.Tasks.Task SubmitOtpCode(string otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                UpdateStatus("OTP code cannot be empty!");
                return;
            }

            if (!AvatarSdk.IsAwaitingEmailOtpCode)
            {
                UpdateStatus("No pending OTP request. Start email login first.");
                return;
            }

            try
            {
                SetProcessing(true, "Verifying OTP code...");
                Debug.Log($"Submitting OTP code: {otpCode}");

                await AvatarSdk.SubmitEmailOtpCodeAsync(otpCode);
                Debug.Log("OTP code submitted successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to submit OTP code: {ex.Message}");
                UpdateStatus($"OTP submission error: {ex.Message}");
                SetProcessing(false);
            }
        }

        #endregion

        #region Event Handlers

        private void OnUserLoggedIn()
        {
            Debug.Log("User logged in successfully!");
            UpdateStatus("Login successful!");
            _pendingEmail = "";
            SetProcessing(false);

            // Clear the OTP input field on successful login
            if (_otpInputField != null)
            {
                _otpInputField.text = "";
            }

            _statusText.gameObject.SetActive(false);
            _emailInputField.gameObject.SetActive(false);
            _otpInputField.gameObject.SetActive(false);
            _submitEmailButton.gameObject.SetActive(false);
            _submitOtpButton.gameObject.SetActive(false);
            _resendCodeButton.gameObject.SetActive(false);
            _signUpButton.gameObject.SetActive(false);
        }

        private void OnEmailCodeRequestSucceeded(string email)
        {
            Debug.Log($"Email verification code sent successfully to: {email}");
            UpdateStatus($"Code sent to: {email}\nEnter the code below and click Send OTP.");
            SetProcessing(false);
            _emailInputField.gameObject.SetActive(false);
            _submitEmailButton.gameObject.SetActive(false);
            _signUpButton.gameObject.SetActive(false);

            _otpInputField.gameObject.SetActive(true);
            _submitOtpButton.gameObject.SetActive(true);
            _resendCodeButton.gameObject.SetActive(true);
        }

        private void OnEmailCodeRequestFailed((string email, string failReason) fail)
        {
            Debug.LogError($"Failed to send email code to {fail.email}: {fail.failReason}");
            UpdateStatus($"Failed to send code: {fail.failReason}");
            _pendingEmail = "";
            SetProcessing(false);
        }

        private void OnEmailCodeSubmissionSucceeded(string code)
        {
            Debug.Log($"OTP code '{code}' accepted successfully!");
            UpdateStatus($"Code accepted! Completing login...");
        }

        private void OnEmailCodeSubmissionFailed((string code, string failReason) fail)
        {
            Debug.LogError($"OTP code '{fail.code}' failed: {fail.failReason}");
            UpdateStatus($"Code rejected: {fail.failReason}\nPlease try again.");
            SetProcessing(false);
        }

        #endregion
    }
}
