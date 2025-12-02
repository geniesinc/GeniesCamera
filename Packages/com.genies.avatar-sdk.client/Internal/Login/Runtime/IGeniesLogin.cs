using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Genies.Login.Anonymous;
using Genies.Login.AuthMessages;
using Genies.Login.EmailOtp;
using Genies.Login.Otp;
using Genies.Login.Password;

namespace Genies.Login
{
    /// <summary>
    /// Defines the contract for Genies authentication and login functionality.
    /// This interface provides methods for user authentication, token management, and profile operations.
    /// </summary>
    public interface IGeniesLogin : IDisposable
    {
        public bool IsDisposed { get; }

        /// <summary>
        /// Delegate for retrieving OTP (One-Time Password) codes from user input.
        /// </summary>
        /// <returns>A task that completes with the user-provided OTP code.</returns>
        public delegate Task<string> GetOtpCodeDelegate();

        /// <summary>
        /// Event triggered when a user successfully logs in.
        /// </summary>
        public event Action UserLoggedIn;

        /// <summary>
        /// Event triggered when a user logs out.
        /// </summary>
        public event Action UserLoggedOut;

        /// <summary>
        /// Event triggered when authentication tokens are refreshed.
        /// </summary>
        /// <param name="newToken">The new access token.</param>
        public event Action<string> OnTokenRefresh;

        /// <summary>
        /// Gets a value indicating whether the login system has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets or sets the refresh token for maintaining authentication sessions.
        /// </summary>
        string RefreshToken { get; set; }

        /// <summary>
        /// Gets the current access token for authenticated requests.
        /// </summary>
        string AuthAccessToken { get; }

        /// <summary>
        /// Gets the current ID token containing user identity information.
        /// </summary>
        string AuthIdToken { get; }

        /// <summary>
        /// Retrieves the username of the currently authenticated user.
        /// </summary>
        /// <returns>A task that completes with the user's username.</returns>
        Task<string> GetUsernameAsync();

        /// <summary>
        /// Saves the user's birthdate to their profile.
        /// </summary>
        /// <param name="birthdate">The birthdate to save in string format.</param>
        /// <returns>A task that completes with the attribute response containing operation results.</returns>
        Task<GeniesAuthAttributeResponse> SaveBirthdate(string birthdate);

        /// <summary>
        /// Retrieves all user attributes for the currently authenticated user.
        /// </summary>
        /// <returns>A task that completes with a dictionary of user attributes.</returns>
        Task<Dictionary<string, string>> GetUserAttributesAsync();

        /// <summary>
        /// Retrieves user attributes for a specific user by username and user ID.
        /// </summary>
        /// <param name="username">The username of the target user.</param>
        /// <param name="userID">The unique identifier of the target user.</param>
        /// <returns>A task that completes with a dictionary of the specified user's attributes.</returns>
        Task<Dictionary<string, string>> GetUserAttributesAsync(string username, string userID);

        /// <summary>
        /// Refreshes the current authentication tokens.
        /// </summary>
        /// <param name="ct">Cancellation token to cancel the operation (optional).</param>
        /// <returns>A task that completes with the new access token.</returns>
        Task<string> RefreshTokens(CancellationToken ct = default);

        /// <summary>
        /// Retrieves the unique identifier of the currently authenticated user.
        /// </summary>
        /// <returns>A task that completes with the user's unique identifier.</returns>
        Task<string> GetUserId();

        /// <summary>
        /// Checks if a user is currently signed in.
        /// </summary>
        /// <returns>True if a user is signed in; otherwise, false.</returns>
        bool IsUserSignedIn();

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow for phone number authentication.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public IOtpLoginFlowController StartOtpLogin();

        public Task<bool> InitializeAPI(string targetEnv, string appName, bool allowSignup, string clientId = null, string clientSecret = null, string scope = null);

        /// <summary>
        /// Performs a global logout, terminating all user sessions across all devices.
        /// </summary>
        /// <returns>A task that completes with the logout response.</returns>
        public Task<GeniesAuthLogoutResponse> GlobalLogOutAsync();

        /// <summary>
        /// Shuts down the login system and cleans up resources.
        /// </summary>
        /// <returns>A task that completes with the shutdown response.</returns>
        public Task<GeniesAuthShutdownResponse> ShutDownAsync();

        /// <summary>
        /// Logs out the current user from this device only.
        /// </summary>
        /// <returns>A task that completes with the logout response.</returns>
        public Task<GeniesAuthLogoutResponse> LogOutAsync();

        /// <summary>
        /// Attempts to automatically log in the user using cached credentials.
        /// </summary>
        /// <returns>A task that completes with the instant login response.</returns>
        public Task<GeniesAuthInstantLoginResponse> TryInstantLoginAsync();

        /// <summary>
        /// Clears all cached authentication credentials from local storage. Requires
        /// API initialization
        /// </summary>
        /// <returns>A task that completes with the clear credentials response.</returns>
        public Task<GeniesAuthClearCachedCredentialsResponse> ClearCachedCredentials();

        /// <summary>
        /// Clears all cached authentication credentials from local storage. Does
        /// not require API initialization
        /// </summary>
        /// <returns>A task that completes with the clear credentials response.</returns>
        public Task<bool> ClearCachedCredentials(string baseUrl, string appName);

        /// <summary>
        /// Updates the current user's profile attributes with the provided parameters.
        /// </summary>
        /// <param name="updateParameters">The profile parameters to update.</param>
        /// <returns>A task that completes with the attribute update response.</returns>
        Task<GeniesAuthAttributeResponse> UpdateUserAttributesAsync(ProfileUpdateParameters updateParameters);

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow for email authentication.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public IEmailOtpLoginFlowController StartMagicLinkLogin();

        /// <summary>
        /// Starts an email/password login flow for email authentication.
        /// </summary>
        /// <returns>A Password Login Flow Controller for managing the authentication process.</returns>
        public IPasswordLoginFlowController StartPasswordLogin();

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow for sms authentication.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public IHybridOtpLoginFlowController StartSmsOtpLogin();

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow that will accept email or phone.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public IHybridOtpLoginFlowController StartHybridOtpLogin();
        
        /// <summary>
        /// Starts an anonymous login flow
        /// </summary>
        /// <returns>An anonymous login flow controller.</returns>
        public IAnonymousLoginFlowController StartAnonymousLogin();

    }
}
