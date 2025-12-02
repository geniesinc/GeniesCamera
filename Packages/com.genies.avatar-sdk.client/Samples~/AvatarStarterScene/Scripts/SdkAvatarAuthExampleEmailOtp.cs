using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Sdk.Samples.AvatarStarter
{
    /// <summary>
    /// Email OTP login sample (button/UI based) using the event-driven flow from LoginEmailOtp.
    /// </summary>
    public class SdkAvatarAuthExampleEmailOtp : MonoBehaviour
    {
        [Header("Scene References")]
        public LoadMyAvatar loadMyAvatar;

        [Header("Status")]
        public TextMeshProUGUI statusText;

        [Header("Email Entry")]
        public TMP_InputField emailInputField;
        public Button submitEmailButton;

        [Header("Sign Up")]
        public Button signUpButton;

        [Header("Code Entry")]
        public TMP_InputField otpInputField;
        public Button submitOtpButton;
        public Button requestNewCodeButton;

        [Header("Logged-in UI")]
        public Button openAvatarEditor;
        public Button logOutButton;

        private string _emailTarget;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private async void Awake()
        {
            // Initial UI state
            SetBusy(false);
            UpdateStatus("Initializing...");

            // Init SDK (idempotent)
            await AvatarSdk.InitializeAsync();

            var instantLoginResult = await AvatarSdk.TryInstantLoginAsync();

            // Subscribe to the same events used by the Debug/LoginEmailOtp pattern
            SubscribeEvents();
            RestartLoggedInStates();
        }

        private void OnEnable()
        {
            // Safety: ensure listeners are added if domain reloaded
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void OnDestroy()
        {
            // Clean up events when this sample is removed
            UnsubscribeEvents();
        }

        // ------------------------------------------------------------------
        // Event subscriptions (LoginEmailOtp pattern)
        // ------------------------------------------------------------------

        private void SubscribeEvents()
        {
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded += OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed += OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded += OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed += OnEmailCodeSubmissionFailed;

            AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;
        }

        private void UnsubscribeEvents()
        {
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded -= OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed -= OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded -= OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed -= OnEmailCodeSubmissionFailed;

            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
        }

        // ------------------------------------------------------------------
        // Flow state / wiring
        // ------------------------------------------------------------------

        private void RestartLoggedInStates()
        {
            _emailTarget = string.Empty;

            if (AvatarSdk.IsLoggedIn)
            {
                OnUserLoggedIn();
                return;
            }

            OnUserLoggedOut();
        }

        private void OnAvatarEditorClosed()
        {
            RestartLoggedInStates();
        }

        private void OnAvatarEditorOpened()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.gameObject.SetActive(false);
            }

            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }
        }

        private void WireButtons()
        {
            if (submitEmailButton != null)
            {
                submitEmailButton.onClick.RemoveAllListeners();
                submitEmailButton.onClick.AddListener(SubmitEmailForMagicAuth);
            }

            if (signUpButton != null)
            {
                signUpButton.onClick.RemoveAllListeners();
                signUpButton.onClick.AddListener(GoToSignUpPage);
            }

            if (submitOtpButton != null)
            {
                submitOtpButton.onClick.RemoveAllListeners();
                submitOtpButton.onClick.AddListener(SubmitOtpCode);
            }

            if (requestNewCodeButton != null)
            {
                requestNewCodeButton.onClick.RemoveAllListeners();
                requestNewCodeButton.onClick.AddListener(RequestNewCode);
            }

            if (openAvatarEditor != null)
            {
                openAvatarEditor.onClick.RemoveAllListeners();
                openAvatarEditor.onClick.AddListener(OpenAvatarEditor);
            }

            if (logOutButton != null)
            {
                logOutButton.onClick.RemoveAllListeners();
                logOutButton.onClick.AddListener(LogOutAndExit);
            }
        }

        private async void OpenAvatarEditor()
        {
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                Debug.LogWarning("The Avatar Editor is already open.");
                return;
            }

            if (loadMyAvatar.LoadedAvatar is null)
            {
                Debug.LogWarning("An avatar must be loaded to open the Avatar Editor.");
            }

            await AvatarSdk.OpenAvatarEditorAsync(loadMyAvatar.LoadedAvatar);
        }

        private async void LogOutAndExit()
        {
            await AvatarSdk.LogOutAsync();
#if UNITY_EDITOR
            // Exiting play mode immediately after the log out call will crash the Unity Editor.
            // Use delayCall to prevent crash.
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void GoToSignUpPage()
        {
            Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
        }

        private void UnwireButtons()
        {
            submitEmailButton?.onClick.RemoveAllListeners();
            signUpButton?.onClick.RemoveAllListeners();
            submitOtpButton?.onClick.RemoveAllListeners();
            requestNewCodeButton?.onClick.RemoveAllListeners();
            logOutButton?.onClick.RemoveAllListeners();
        }

        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------

        private void SetBusy(bool busy)
        {
            if (submitEmailButton)
            {
                submitEmailButton.interactable = !busy;
            }

            if (signUpButton)
            {
                signUpButton.interactable = !busy;
            }

            if (submitOtpButton)
            {
                submitOtpButton.interactable = !busy;
            }

            if (requestNewCodeButton)
            {
                requestNewCodeButton.interactable = !busy;
            }

            if (emailInputField)
            {
                emailInputField.interactable = !busy;
            }

            if (otpInputField)
            {
                otpInputField.interactable = !busy;
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.interactable = !busy;
            }

            if (logOutButton)
            {
                logOutButton.interactable = !busy;
            }
        }

        private void ShowEmailEntryUI()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            if (emailInputField)
            {
                emailInputField.gameObject.SetActive(true);
            }

            if (submitEmailButton)
            {
                submitEmailButton.gameObject.SetActive(true);
            }

            if (signUpButton)
            {
                signUpButton.gameObject.SetActive(true);
            }

            if (otpInputField)
            {
                otpInputField.gameObject.SetActive(false);
            }

            if (submitOtpButton)
            {
                submitOtpButton.gameObject.SetActive(false);
            }

            if (requestNewCodeButton)
            {
                requestNewCodeButton.gameObject.SetActive(false);
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.gameObject.SetActive(false);
            }

            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }

            SetBusy(false);
        }

        private void ShowCodeEntryUI()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            if (emailInputField)
            {
                emailInputField.gameObject.SetActive(false);
            }

            if (submitEmailButton)
            {
                submitEmailButton.gameObject.SetActive(false);
            }

            if (signUpButton)
            {
                signUpButton.gameObject.SetActive(false);
            }

            if (otpInputField)
            {
                otpInputField.gameObject.SetActive(true);
            }

            if (submitOtpButton)
            {
                submitOtpButton.gameObject.SetActive(true);
            }

            if (requestNewCodeButton)
            {
                requestNewCodeButton.gameObject.SetActive(true);
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.gameObject.SetActive(false);
            }

            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }

            SetBusy(false);
            UpdateStatus("Enter the verification code sent to your email.");
        }

        private void UpdateStatus(string text)
        {
            if (statusText)
            {
                statusText.text = text;
            }
        }

        private void ShowProcessing(string message)
        {
            SetBusy(true);
            UpdateStatus(message);
        }

        // ------------------------------------------------------------------
        // Button handlers (call simple SDK methods; events drive the UI)
        // ------------------------------------------------------------------

        private async void SubmitEmailForMagicAuth()
        {
            if (emailInputField == null)
            {
                UpdateStatus("Email field not set.");
                return;
            }

            var email = emailInputField.text?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                UpdateStatus("Please enter a valid email.");
                return;
            }

            try
            {
                ShowProcessing($"Sending verification code to {email}...");
                await AvatarSdk.StartLoginEmailOtpAsync(email);
                // Success/failure UI continues via events.
            }
            catch (Exception ex)
            {
                SetBusy(false);
                UpdateStatus($"Failed to request code: {ex.Message}");
                ShowEmailEntryUI();
            }
        }

        private async void SubmitOtpCode()
        {
            if (otpInputField == null)
            {
                UpdateStatus("OTP field not set.");
                return;
            }

            var code = otpInputField.text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                UpdateStatus("Please enter the verification code.");
                return;
            }

            try
            {
                ShowProcessing("Verifying code...");
                await AvatarSdk.SubmitEmailOtpCodeAsync(code);
                // Events will handle success/failure transitions.
            }
            catch (Exception ex)
            {
                SetBusy(false);
                UpdateStatus($"Failed to submit code: {ex.Message}");
                ShowCodeEntryUI();
            }
        }

        private async void RequestNewCode()
        {
            try
            {
                ShowProcessing("Resending email code...");
                await AvatarSdk.ResendEmailCodeAsync();
                // Events will handle follow-up UI.
            }
            catch (Exception ex)
            {
                SetBusy(false);
                UpdateStatus($"Failed to resend code: {ex.Message}");
                ShowCodeEntryUI();
            }
        }

        // ------------------------------------------------------------------
        // Event handlers (mirror LoginEmailOtp pattern)
        // ------------------------------------------------------------------

        private void OnUserLoggedIn()
        {
            // Clear busy and finish flow
            SetBusy(false);
            UpdateStatus("Logged in successfully.");

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            if (emailInputField)
            {
                emailInputField.gameObject.SetActive(false);
            }

            if (submitEmailButton)
            {
                submitEmailButton.gameObject.SetActive(false);
            }

            if (signUpButton)
            {
                signUpButton.gameObject.SetActive(false);
            }

            if (otpInputField)
            {
                otpInputField.gameObject.SetActive(false);
            }

            if (submitOtpButton)
            {
                submitOtpButton.gameObject.SetActive(false);
            }

            if (requestNewCodeButton)
            {
                requestNewCodeButton.gameObject.SetActive(false);
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.gameObject.SetActive(true);
            }

            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(true);
            }
        }

        private void OnUserLoggedOut()
        {
            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }

            if (AvatarSdk.IsAwaitingEmailOtpCode)
            {
                // We’ve already requested a code; go straight to code entry
                ShowCodeEntryUI();
                UpdateStatus("Enter the verification code sent to your email.");
            }
            else
            {
                // Fresh flow
                ShowEmailEntryUI();
                UpdateStatus("Enter your email");
            }
        }

        private void OnEmailCodeRequestSucceeded(string email)
        {
            _emailTarget = email;
            SetBusy(false);
            ShowCodeEntryUI();

            // Optional: reflect where the code was sent
            if (statusText != null)
            {
                UpdateStatus($"A verification code was sent to {email}.");
            }
        }

        private void OnEmailCodeRequestFailed((string email, string failReason) fail)
        {
            SetBusy(false);

            // If we were already waiting for a code, keep the code UI; otherwise, return to email
            if (AvatarSdk.IsAwaitingEmailOtpCode)
            {
                ShowCodeEntryUI();
                UpdateStatus($"Could not resend code: {fail.failReason}");
            }
            else
            {
                ShowEmailEntryUI();
                UpdateStatus(string.IsNullOrWhiteSpace(fail.failReason)
                    ? "Could not request code. Please try again."
                    : fail.failReason);
            }
        }

        private void OnEmailCodeSubmissionSucceeded(string code)
        {
            // Logged-in event will handle final teardown; just clear busy here
            SetBusy(false);
            UpdateStatus("Code accepted. Finalizing login...");
        }

        private void OnEmailCodeSubmissionFailed((string code, string failReason) fail)
        {
            SetBusy(false);
            ShowCodeEntryUI();
            UpdateStatus(string.IsNullOrWhiteSpace(fail.failReason)
                ? "Verification failed. Please try again."
                : fail.failReason);
        }
    }
}
