using System;
using System.Threading.Tasks;
using Genies.Login.EmailOtp;
using Genies.NativeAPI;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Login.Native.Editor
{
    public class GeniesEditorLoginController
    {
        private readonly LoginStateInfo _StateInfo;
        private readonly string _ApiPath;
        private readonly string _ClientName;
        private readonly string _EmailKey;
        private IEmailOtpLoginFlowController _OtpLoginController;
        private GeniesLoginEditorWindow _Window;

        public bool Initializing { get; private set; }
        public LoginStateInfo LoginStateInfo => _StateInfo;

        public GeniesEditorLoginController(string apiPath, string clientName = "GeniesEditorLogin", string emailKey = null)
        {
            _ApiPath = apiPath;
            _ClientName = clientName;
            this._EmailKey = emailKey ?? $"{clientName}-email";
            _StateInfo = new LoginStateInfo(LoginState.EnterEmail, null);

            if (!this.HasService<IGeniesLogin>())
            {
                var service = new GeniesNativeAPIAuth();
                ServiceManager.RegisterService<IGeniesLogin>(service);
            }
        }

        /// <summary>
        /// Shows the login window using this controller's state.
        /// If window is already open, brings it to focus.
        /// </summary>
        public void ShowLoginWindow()
        {
            if (_Window != null)
            {
                _Window.Focus();
                return;
            }

            _Window = GeniesLoginEditorWindow.ShowWindow(this, _EmailKey);
        }

        /// <summary>
        /// Called by the window when it's destroyed.
        /// </summary>
        internal void OnWindowDestroyed()
        {
            _Window = null;
        }

        // initializes login and attempts to do an instant sign in
        public async Task<bool> InitializeAndTryInstantLogin()
        {
            try
            {
                Initializing = true;
                await InitializeLoginAsync();

                // attempt an instant login
                var didLoginInstantly = await GeniesLoginSdk.TryInstantLoginAsync();
                if (didLoginInstantly)
                {
                    _StateInfo.SetState(LoginState.LoggedIn);
                    Initializing = false;
                    return true;
                }

                // if not signed in and no instant login, stay on the 'enter email' screen
                _StateInfo.SetState(LoginState.EnterEmail);
                Initializing = false;
                return true;
            }
            catch (Exception ex)
            {
                Initializing = false;
                Debug.LogError($"Unexpected error in login initialization: {ex}");
                return false;
            }
        }

        private async Task InitializeLoginAsync()
        {
            await GeniesLoginSdk.InitializeAsync(_ApiPath, _ClientName, false);
        }

        // begins login process with email number
        public async void SubmitEmailAsync(string email)
        {
            try
            {
                var didInitialize = await InitializeAndTryInstantLogin();

                if (!didInitialize)
                {
                    _StateInfo.SetAwaitingLoginResponse(false);
                    _StateInfo.SetState(newState: LoginState.EnterEmail, "Failed to initialize");
                    return;
                }

                if (GeniesLoginSdk.IsUserSignedIn())
                {
                    _StateInfo.SetState(LoginState.LoggedIn);
                    return;
                }

                _StateInfo.SetAwaitingLoginResponse(true);

                // begin login
                _OtpLoginController?.Dispose();
                _OtpLoginController = GeniesLoginSdk.StartMagicLinkLogin();
                var result = await _OtpLoginController.SubmitEmailAsync(email);
                _StateInfo.SetAwaitingLoginResponse(false);

                // if email number is submitted successfully, continue
                if (result.IsSuccessful)
                {
                    if (GeniesLoginSdk.IsUserSignedIn())
                    {
                        _StateInfo.SetState(LoginState.LoggedIn);
                        return;
                    }

                    _StateInfo.SetState(LoginState.EnterCode);
                    return;
                }

                // reset UI with new error
                _StateInfo.SetState(LoginState.EnterEmail, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during login: {ex.Message}");
                _StateInfo.SetAwaitingLoginResponse(false);
            }
        }

        // submit otp code from user
        public async void SubmitOtpCodeAsync(string email, string code)
        {
            _StateInfo.SetAwaitingVerification(true);
            var result = await _OtpLoginController.SubmitCodeAsync(email, code);
            _StateInfo.SetAwaitingVerification(false);

            if (result.IsSuccessful)
            {
                _StateInfo.SetState(LoginState.LoggedIn);
                return;
            }

            // if otp submission is not successful, have them try again
            _StateInfo.SetError(result.ErrorMessage ?? result.Message);
        }

        public async void ResendCode(string email)
        {
            _StateInfo.SetAwaitCodeResend(true);
            var result = await _OtpLoginController.ResendCodeAsync(email);
            _StateInfo.SetAwaitCodeResend(false);

            if (!result.IsSuccessful)
            {
                // if otp submission is not successful, have them try again
                _StateInfo.SetError(result.ErrorMessage);
            }
        }

        // logs user out and communicates information to experience settings UI
        public async void LogOutAsync()
        {
            try
            {
                _StateInfo.SetAwaitingLogOut(true);

                var logOutResult = await GeniesLoginSdk.LogOutAsync();

                _StateInfo.SetAwaitingLogOut(false);

                var logOutSuccess = logOutResult.Status == "success";
                if (logOutSuccess || !GeniesLoginSdk.IsUserSignedIn())
                {
                    // Successfully logged out or user is no longer signed in
                    _StateInfo.SetState(LoginState.EnterEmail);
                }
                else
                {
                    _StateInfo.SetError(logOutResult.ErrorMessage ?? "Failed to log out");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during logout: {ex.Message}");
                _StateInfo.SetAwaitingLogOut(false);

                // Check if user is actually still logged in
                if (!GeniesLoginSdk.IsUserSignedIn())
                {
                    _StateInfo.SetState(LoginState.EnterEmail);
                }
                else
                {
                    _StateInfo.SetError("An error occurred during logout");
                }
            }
        }
    }

    public enum LoginState
    {
        EnterEmail,
        EnterCode,
        LoggedIn
    }

    public class LoginStateInfo
    {
        public delegate void StateUpdatedHandler(LoginState previous, LoginState newState);
        public delegate void StateChangedHandler();

        public LoginState State { get; private set; }
        public string OptionalError { get; private set; }

        public bool AwaitingLoginResponse { get; private set; }
        public bool AwaitingVerification { get; private set; }
        public bool AwaitingResendOtp { get; private set; }
        public bool AwaitingLogOut { get; private set; }

        public event StateUpdatedHandler Updated;
        public event StateChangedHandler Changed;

        public LoginStateInfo(LoginState state, string optionalError)
        {
            State = state;
            OptionalError = optionalError;
        }

        public void SetAwaitingLoginResponse(bool value)
        {
            AwaitingLoginResponse = value;
            NotifyChanged();
        }

        public void SetAwaitingVerification(bool value)
        {
            AwaitingVerification = value;
            NotifyChanged();
        }

        public void SetAwaitingLogOut(bool value)
        {
            AwaitingLogOut = value;
            NotifyChanged();
        }

        public void SetAwaitCodeResend(bool value)
        {
            AwaitingResendOtp = value;
            NotifyChanged();
        }

        public void SetState(LoginState newState, string optionalError = null)
        {
            var stateChanged = newState != State;
            var errorChanged = optionalError != OptionalError;

            if (stateChanged)
            {
                OnUpdated(State, newState);
            }

            State = newState;
            OptionalError = optionalError;

            if (stateChanged || errorChanged)
            {
                NotifyChanged();
            }
        }

        public void SetError(string errorMessage)
        {
            OptionalError = errorMessage;
            NotifyChanged();
        }

        public void ResetError()
        {
            OptionalError = null;
            NotifyChanged();
        }

        private void OnUpdated(LoginState previous, LoginState newstate)
        {
            Updated?.Invoke(previous, newstate);
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
