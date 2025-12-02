// Email OTP Login Flow:
//
// The email OTP (One-Time Password) flow is a passwordless authentication method where users receive
// a verification code via email to complete login.
//
// Flow Steps:
// 1. Call StartLoginEmailOtpAsync(email) to initiate the flow
//    - Sends a verification code to the provided email address
//    - Returns success/failure and optional error message
//    - On success, IsAwaitingEmailOtpCode becomes true
//    - Triggers LoginEmailOtpCodeRequestSucceeded or LoginEmailOtpCodeRequestFailed event
//
// 2. (Optional) Call ResendEmailCodeAsync() to resend the verification code
//    - Resends the code to the same email address from step 1
//    - Useful if user didn't receive the initial email
//    - Triggers the same events as StartLoginEmailOtpAsync
//
// 3. Call SubmitEmailOtpCodeAsync(code) with the code received via email
//    - Validates the code and completes authentication
//    - On success, user is logged in and IsAwaitingEmailOtpCode becomes false
//    - Triggers LoginEmailOtpCodeSubmissionSucceeded or LoginEmailOtpCodeSubmissionFailed event
//
// State Management:
// - IsAwaitingEmailOtpCode: Indicates whether the flow is waiting for code submission
// - EmailLoginFlowController: Internal controller managing the flow lifecycle
// - EmailAddress: Stores the email address for the current flow

using System;
using Cysharp.Threading.Tasks;
using Genies.Login.EmailOtp;
using Genies.Login.Native;
using UnityEngine;

namespace Genies.Sdk
{
    internal partial class CoreSdk
    {
        public class LoginOtp
        {
            private CoreSdk Parent { get; }

            private LoginOtp() { }

            internal LoginOtp(CoreSdk parent)
            {
                Parent = parent;
            }
            /// <summary>
            /// Invoked when an email OTP code request succeeds. Provides the email address.
            /// </summary>
            public event Action<string> LoginEmailOtpCodeRequestSucceeded;

            /// <summary>
            /// Invoked when an email OTP code request fails. Provides the email address and failure reason.
            /// </summary>
            public event Action<(string email, string failReason)> LoginEmailOtpCodeRequestFailed;

            /// <summary>
            /// Invoked when an email OTP code submission succeeds. Provides the submitted code.
            /// </summary>
            public event Action<string> LoginEmailOtpCodeSubmissionSucceeded;

            /// <summary>
            /// Invoked when an email OTP code submission fails. Provides the submitted code and failure reason.
            /// </summary>
            public event Action<(string code, string failReason)> LoginEmailOtpCodeSubmissionFailed;

            /// <summary>
            /// Gets whether the email OTP login flow is awaiting code submission.
            /// </summary>
            public bool IsAwaitingEmailOtpCode { get; set; }

            private IEmailOtpLoginFlowController EmailLoginFlowController { get; set; }
            private string EmailCurrent { get; set; }

            /// <summary>
            /// Starts the email login flow by submitting an email address to receive a verification code.
            /// </summary>
            /// <param name="email">The email address to send the verification code to.</param>
            /// <returns>A tuple indicating success and an optional failure reason.</returns>
            public async UniTask<(bool succeeded, string failReason)> StartLoginEmailOtpAsync(string email)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    var failReason = "Genies SDK initialization failed.";
                    InvokeLoginEmailOtpCodeRequestFailed(email, failReason);
                    return (false, failReason);
                }

                if (Parent.LoginApi.IsLoggedIn)
                {
                    var failReason = "User is already logged in.";
                    InvokeLoginEmailOtpCodeRequestFailed(email, failReason);
                    return (false, failReason);
                }

                EmailLoginFlowController?.Dispose();
                EmailLoginFlowController = null;
                IsAwaitingEmailOtpCode = false;

                EmailLoginFlowController = GeniesLoginSdk.StartMagicLinkLogin();
                EmailCurrent = email;
                var result = await EmailLoginFlowController.SubmitEmailAsync(email);

                if (result.IsSuccessful)
                {
                    IsAwaitingEmailOtpCode = true;
                    InvokeLoginEmailOtpCodeRequestSucceeded(email);
                    return (true, null);
                }

                IsAwaitingEmailOtpCode = false;
                InvokeLoginEmailOtpCodeRequestFailed(email, result.ErrorMessage);
                return (false, result.ErrorMessage);
            }

            /// <summary>
            /// Resends the email verification code to the email address from the current login flow.
            /// </summary>
            /// <returns>A tuple indicating success and an optional failure reason.</returns>
            public async UniTask<(bool succeeded, string failReason)> ResendEmailCodeAsync()
            {
                if (EmailLoginFlowController is null)
                {
                    var failReason = $"Start login flow first using {nameof(StartLoginEmailOtpAsync)}";
                    Debug.LogWarning(failReason);
                    IsAwaitingEmailOtpCode = false;
                    InvokeLoginEmailOtpCodeRequestFailed(EmailCurrent ?? string.Empty, failReason);
                    return (false, failReason);
                }

                var result = await EmailLoginFlowController.ResendCodeAsync(EmailCurrent);

                if (result.IsSuccessful)
                {
                    IsAwaitingEmailOtpCode = true;
                    InvokeLoginEmailOtpCodeRequestSucceeded(EmailCurrent);
                    return (true, null);
                }
                else
                {
                    IsAwaitingEmailOtpCode = false;
                    InvokeLoginEmailOtpCodeRequestFailed(EmailCurrent ?? string.Empty, result.ErrorMessage);
                    return (false, result.ErrorMessage);
                }
            }

            /// <summary>
            /// Submits the email OTP code to complete the email OTP login flow.
            /// </summary>
            /// <param name="code">The OTP code received via email.</param>
            /// <returns>A tuple indicating success and an optional failure reason.</returns>
            public async UniTask<(bool succeeded, string failReason)> SubmitEmailOtpCodeAsync(string code)
            {
                if (EmailLoginFlowController is null)
                {
                    var failReason = $"Start login flow first using {nameof(StartLoginEmailOtpAsync)}";
                    Debug.LogWarning(failReason);
                    IsAwaitingEmailOtpCode = false;
                    InvokeLoginEmailOtpCodeSubmissionFailed(code, failReason);
                    return (false, failReason);
                }

                var result = await EmailLoginFlowController.SubmitCodeAsync(EmailCurrent, code);

                if (result.IsSuccessful)
                {
                    IsAwaitingEmailOtpCode = false;
                    InvokeLoginEmailOtpCodeSubmissionSucceeded(code);
                    return (true, null);
                }
                else
                {
                    InvokeLoginEmailOtpCodeSubmissionFailed(code, result.ErrorMessage);
                    return (false, result.ErrorMessage);
                }
            }

            private void InvokeLoginEmailOtpCodeRequestSucceeded(string email) => LoginEmailOtpCodeRequestSucceeded?.Invoke(email);
            private void InvokeLoginEmailOtpCodeRequestFailed(string email, string reason) => LoginEmailOtpCodeRequestFailed?.Invoke((email, reason));
            private void InvokeLoginEmailOtpCodeSubmissionSucceeded(string code) => LoginEmailOtpCodeSubmissionSucceeded?.Invoke(code);
            private void InvokeLoginEmailOtpCodeSubmissionFailed(string code, string reason) => LoginEmailOtpCodeSubmissionFailed?.Invoke((code, reason));
        }
    }
}
