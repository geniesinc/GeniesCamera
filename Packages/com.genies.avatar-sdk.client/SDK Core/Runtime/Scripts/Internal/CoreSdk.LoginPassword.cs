// Password Login Flow:
//
// The password login flow is a traditional email and password authentication method. Depending on
// security settings, it may require an additional email verification step after initial sign-in.
//
// Flow Steps:
// 1. Call StartLoginPasswordAsync(email, password) to initiate sign-in
//    - Authenticates with the provided email and password
//    - Returns success/failure, verification requirement flag, and optional error message
//    - Triggers one of three events based on the result:
//      a) LoginPasswordSignInComplete: Login succeeded without additional verification required
//      b) LoginPasswordSignInPendingVerification: Login succeeded but email verification required
//         - IsAwaitingPasswordVerification becomes true
//         - User receives a verification code via email
//      c) LoginPasswordSignInFailed: Login failed (invalid credentials or other error)
//
// 2. (Conditional) If verification is required, call SubmitPasswordVerificationCodeAsync(code)
//    - Only necessary when StartLoginPasswordAsync returns requiresVerification = true
//    - Validates the verification code received via email
//    - On success, user is fully logged in and IsAwaitingPasswordVerification becomes false
//    - Triggers LoginPasswordVerificationCodeSucceeded or LoginPasswordVerificationCodeFailed event
//    - On success, also triggers LoginPasswordSignInComplete event
//
// State Management:
// - IsAwaitingPasswordVerification: Indicates whether the flow is waiting for verification code
// - PasswordLoginFlowController: Internal controller managing the flow lifecycle
// - PasswordEmail: Stores the email address for the current flow

using System;
using Cysharp.Threading.Tasks;
using Genies.Login.AuthMessages;
using Genies.Login.Native;
using Genies.Login.Password;
using UnityEngine;

namespace Genies.Sdk
{
    internal partial class CoreSdk
    {
        public class LoginPassword
        {
            private CoreSdk Parent { get; }

            private LoginPassword() { }

            internal LoginPassword(CoreSdk parent)
            {
                Parent = parent;
            }
            /// <summary>
            /// Invoked when a password sign-in succeeds and requires email verification. Provides the email address.
            /// </summary>
            public event Action<string> LoginPasswordSignInPendingVerification;

            /// <summary>
            /// Invoked when a password sign-in succeeds and login is complete. Provides the email address.
            /// </summary>
            public event Action<string> LoginPasswordSignInComplete;

            /// <summary>
            /// Invoked when a password sign-in fails. Provides the email address and failure reason.
            /// </summary>
            public event Action<(string email, string failReason)> LoginPasswordSignInFailed;

            /// <summary>
            /// Invoked when a password verification code submission succeeds. Provides the submitted code.
            /// </summary>
            public event Action<string> LoginPasswordVerificationCodeSucceeded;

            /// <summary>
            /// Invoked when a password verification code submission fails. Provides the submitted code and failure reason.
            /// </summary>
            public event Action<(string code, string failReason)> LoginPasswordVerificationCodeFailed;

            /// <summary>
            /// Gets whether the password login flow is awaiting email verification code submission.
            /// </summary>
            public bool IsAwaitingPasswordVerification { get; set; }

            private IPasswordLoginFlowController PasswordLoginFlowController { get; set; }
            private string EmailCurrent { get; set; }

            /// <summary>
            /// Starts the password login flow by signing in with email and password.
            /// </summary>
            /// <param name="email">The email address to sign in with.</param>
            /// <param name="password">The password for the account.</param>
            /// <returns>A tuple indicating success, whether email verification is required, and an optional failure reason.</returns>
            public async UniTask<(bool succeeded, bool requiresVerification, string failReason)> StartLoginPasswordAsync(string email, string password)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    var failReason = "Genies SDK initialization failed.";
                    InvokeLoginPasswordSignInFailed(email, failReason);
                    return (false, false, failReason);
                }

                if (Parent.LoginApi.IsLoggedIn)
                {
                    var failReason = "User is already logged in.";
                    InvokeLoginPasswordSignInFailed(email, failReason);
                    return (false, false, failReason);
                }

                PasswordLoginFlowController?.Dispose();
                PasswordLoginFlowController = null;
                IsAwaitingPasswordVerification = false;

                PasswordLoginFlowController = GeniesLoginSdk.StartPasswordLogin();
                EmailCurrent = email;
                var result = await PasswordLoginFlowController.SignInAsync(email, password);

                if (result.IsSuccessful)
                {
                    if (result.ResponseStatusCode is GeniesAuthSignInV2Response.StatusCode.SignInPending)
                    {
                        IsAwaitingPasswordVerification = true;
                        InvokeLoginPasswordSignInPendingVerification(email);
                        return (true, true, null);
                    }

                    if (result.ResponseStatusCode is GeniesAuthSignInV2Response.StatusCode.SignInSuccess)
                    {
                        IsAwaitingPasswordVerification = false;
                        InvokeLoginPasswordSignInComplete(email);
                        return (true, false, null);
                    }
                }

                IsAwaitingPasswordVerification = false;
                InvokeLoginPasswordSignInFailed(email, result.ErrorMessage);
                return (false, false, result.ErrorMessage);
            }

            /// <summary>
            /// Submits the password verification code if email verification is required after sign-in.
            /// </summary>
            /// <param name="code">The verification code received via email.</param>
            /// <returns>A tuple indicating success and an optional failure reason.</returns>
            public async UniTask<(bool succeeded, string failReason)> SubmitPasswordVerificationCodeAsync(string code)
            {
                if (PasswordLoginFlowController is null)
                {
                    var failReason = $"Start login flow first using {nameof(StartLoginPasswordAsync)}";
                    Debug.LogWarning(failReason);
                    IsAwaitingPasswordVerification = false;
                    InvokeLoginPasswordVerificationCodeFailed(code, failReason);
                    return (false, failReason);
                }

                var result = await PasswordLoginFlowController.VerifyEmailAsync(code);

                if (result.IsSuccessful)
                {
                    IsAwaitingPasswordVerification = false;
                    InvokeLoginPasswordVerificationCodeSucceeded(code);
                    InvokeLoginPasswordSignInComplete(EmailCurrent);
                    return (true, null);
                }

                InvokeLoginPasswordVerificationCodeFailed(code, result.ErrorMessage);
                return (false, result.ErrorMessage);
            }

            private void InvokeLoginPasswordSignInPendingVerification(string email) => LoginPasswordSignInPendingVerification?.Invoke(email);
            private void InvokeLoginPasswordSignInComplete(string email) => LoginPasswordSignInComplete?.Invoke(email);
            private void InvokeLoginPasswordSignInFailed(string email, string reason) => LoginPasswordSignInFailed?.Invoke((email, reason));
            private void InvokeLoginPasswordVerificationCodeSucceeded(string code) => LoginPasswordVerificationCodeSucceeded?.Invoke(code);
            private void InvokeLoginPasswordVerificationCodeFailed(string code, string reason) => LoginPasswordVerificationCodeFailed?.Invoke((code, reason));
        }
    }
}
