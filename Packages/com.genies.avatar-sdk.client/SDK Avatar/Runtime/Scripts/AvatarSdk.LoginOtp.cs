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

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Gets whether the email OTP login flow is awaiting code submission.
        /// </summary>
        public static bool IsAwaitingEmailOtpCode => Instance.CoreSdk.LoginOtpApi.IsAwaitingEmailOtpCode;

        public static partial class Events
        {
            /// <summary>
            /// Invoked when an email OTP code request succeeds. Provides the email address.
            /// </summary>
            public static event Action<string> LoginEmailOtpCodeRequestSucceeded
            {
                add => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeRequestSucceeded += value;
                remove => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeRequestSucceeded -= value;
            }

            /// <summary>
            /// Invoked when an email OTP code request fails. Provides the email address and failure reason.
            /// </summary>
            public static event Action<(string email, string failReason)> LoginEmailOtpCodeRequestFailed
            {
                add => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeRequestFailed += value;
                remove => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeRequestFailed -= value;
            }

            /// <summary>
            /// Invoked when an email OTP code submission succeeds. Provides the submitted code.
            /// </summary>
            public static event Action<string> LoginEmailOtpCodeSubmissionSucceeded
            {
                add => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeSubmissionSucceeded += value;
                remove => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeSubmissionSucceeded -= value;
            }

            /// <summary>
            /// Invoked when an email OTP code submission fails. Provides the submitted code and failure reason.
            /// </summary>
            public static event Action<(string code, string failReason)> LoginEmailOtpCodeSubmissionFailed
            {
                add => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeSubmissionFailed += value;
                remove => Instance.CoreSdk.LoginOtpApi.LoginEmailOtpCodeSubmissionFailed -= value;
            }
        }

        /// <summary>
        /// Starts the email login flow by submitting an email address to receive a verification code.
        /// </summary>
        /// <param name="email">The email address to send the verification code to.</param>
        /// <returns>A tuple indicating success and an optional failure reason.</returns>
        public static async UniTask<(bool succeeded, string failReason)> StartLoginEmailOtpAsync(string email)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginOtpApi.StartLoginEmailOtpAsync(email);
        }

        /// <summary>
        /// Resends the email verification code to the email address from the current login flow.
        /// </summary>
        /// <returns>A tuple indicating success and an optional failure reason.</returns>
        public static async UniTask<(bool succeeded, string failReason)> ResendEmailCodeAsync()
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginOtpApi.ResendEmailCodeAsync();
        }

        /// <summary>
        /// Submits the email OTP code to complete the email OTP login flow.
        /// </summary>
        /// <param name="code">The OTP code received via email.</param>
        /// <returns>A tuple indicating success and an optional failure reason.</returns>
        public static async UniTask<(bool succeeded, string failReason)> SubmitEmailOtpCodeAsync(string code)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginOtpApi.SubmitEmailOtpCodeAsync(code);
        }
    }
}
