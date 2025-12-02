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

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Gets whether the password login flow is awaiting email verification code submission.
        /// </summary>
        public static bool IsAwaitingPasswordVerification => Instance.CoreSdk.LoginPasswordApi.IsAwaitingPasswordVerification;

        public static partial class Events
        {
            /// <summary>
            /// Invoked when a password sign-in succeeds and requires email verification. Provides the email address.
            /// </summary>
            public static event Action<string> LoginPasswordSignInPendingVerification
            {
                add => Instance.CoreSdk.LoginPasswordApi.LoginPasswordSignInPendingVerification += value;
                remove => Instance.CoreSdk.LoginPasswordApi.LoginPasswordSignInPendingVerification -= value;
            }

            /// <summary>
            /// Invoked when a password sign-in succeeds and login is complete. Provides the email address.
            /// </summary>
            public static event Action<string> LoginPasswordSignInComplete
            {
                add => Instance.CoreSdk.LoginPasswordApi.LoginPasswordSignInComplete += value;
                remove => Instance.CoreSdk.LoginPasswordApi.LoginPasswordSignInComplete -= value;
            }

            /// <summary>
            /// Invoked when a password sign-in fails. Provides the email address and failure reason.
            /// </summary>
            public static event Action<(string email, string failReason)> LoginPasswordSignInFailed
            {
                add => Instance.CoreSdk.LoginPasswordApi.LoginPasswordSignInFailed += value;
                remove => Instance.CoreSdk.LoginPasswordApi.LoginPasswordSignInFailed -= value;
            }

            /// <summary>
            /// Invoked when a password verification code submission succeeds. Provides the submitted code.
            /// </summary>
            public static event Action<string> LoginPasswordVerificationCodeSucceeded
            {
                add => Instance.CoreSdk.LoginPasswordApi.LoginPasswordVerificationCodeSucceeded += value;
                remove => Instance.CoreSdk.LoginPasswordApi.LoginPasswordVerificationCodeSucceeded -= value;
            }

            /// <summary>
            /// Invoked when a password verification code submission fails. Provides the submitted code and failure reason.
            /// </summary>
            public static event Action<(string code, string failReason)> LoginPasswordVerificationCodeFailed
            {
                add => Instance.CoreSdk.LoginPasswordApi.LoginPasswordVerificationCodeFailed += value;
                remove => Instance.CoreSdk.LoginPasswordApi.LoginPasswordVerificationCodeFailed -= value;
            }
        }

        /// <summary>
        /// Starts the password login flow by signing in with email and password.
        /// </summary>
        /// <param name="email">The email address to sign in with.</param>
        /// <param name="password">The password for the account.</param>
        /// <returns>A tuple indicating success, whether email verification is required, and an optional failure reason.</returns>
        public static async UniTask<(bool succeeded, bool requiresVerification, string failReason)> StartLoginPasswordAsync(string email, string password)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginPasswordApi.StartLoginPasswordAsync(email, password);
        }

        /// <summary>
        /// Submits the password verification code if email verification is required after sign-in.
        /// </summary>
        /// <param name="code">The verification code received via email.</param>
        /// <returns>A tuple indicating success and an optional failure reason.</returns>
        public static async UniTask<(bool succeeded, string failReason)> SubmitPasswordVerificationCodeAsync(string code)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginPasswordApi.SubmitPasswordVerificationCodeAsync(code);
        }
    }
}
