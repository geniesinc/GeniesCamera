#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login.Anonymous;
using Genies.Login.AuthMessages;
using Genies.Login.EmailOtp;
using Genies.Login.Otp;
using Genies.Login.Password;
using Genies.ServiceManagement;
using UnityEngine;
using UnityEngine.Analytics;

namespace Genies.Login.Native
{
    /// <summary>
    /// Static entry point for the Genies Native Login SDK providing authentication functionality.
    /// This class manages the underlying login implementation and provides a simplified API for Unity applications.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class GeniesLoginSdk
#else
    public static class GeniesLoginSdk
#endif
    {
        private static IGeniesLogin _instance;

        private static readonly HashSet<Action> _pendingLogin = new();
        private static readonly HashSet<Action> _pendingLogout = new();
        private static readonly HashSet<Action<string>> _pendingRefresh = new();

        /// <summary>
        /// Gets a value indicating whether the login SDK has been initialized and is ready for use.
        /// </summary>
        public static bool IsInitialized => _instance != null && _instance.IsInitialized;

        /// <summary>
        /// The URL for the Genies Hub authentication and sign-up page for new users to create an account.
        /// </summary>
        public static string UrlGeniesHubSignUp { get; }
#if GENIES_INTERNAL && GENIES_DEV
            = "https://hub.dev.genies.com/auth";
#else
            = "https://hub.genies.com/auth";
#endif

        /// <summary>
        /// Gets the current refresh token for maintaining authentication sessions.
        /// </summary>
        public static string RefreshToken => _instance?.RefreshToken;

        /// <summary>
        /// Gets the current ID token containing user identity information.
        /// </summary>
        public static string AuthIdToken => _instance?.AuthIdToken;

        /// <summary>
        /// Gets the current access token for authenticated requests.
        /// </summary>
        public static string AuthAccessToken => _instance?.AuthAccessToken;

        /// <summary>
        /// Gets the instance of IGeniesLogin registered to the <see cref="ServiceManager"/>.
        /// Expects the <see cref="IGeniesInstaller"/> pattern where consumers are responsible for initializing runtime with <see cref="NativeGeniesLoginInstaller"/>.
        /// </summary>
        private static IGeniesLogin Instance
        {
            get
            {
                if (_instance is null || _instance.IsDisposed)
                {
                    _instance = null;

                    Application.focusChanged -= OnFocusChanged;
                    Application.focusChanged += OnFocusChanged;

#if UNITY_EDITOR
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                    AssemblyReloadEvents.beforeAssemblyReload -= OnAssemblyReload;
                    AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
                    UserLoggedIn -= FireAnaylticsOnUserLoggedIn;
                    UserLoggedIn += FireAnaylticsOnUserLoggedIn;
#endif

                    _instance = ServiceManager.GetService<IGeniesLogin>(null);
                    if (_instance is not null)
                    {
                        ApplyPendingCallbacks();
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Event triggered when authentication tokens are refreshed.
        /// Subscribers receive the new access token as a parameter.
        /// </summary>
        public static event Action<string> OnTokenRefresh
        {
            add
            {
                if (_instance == null)
                {
                    _pendingRefresh.Add(value);
                }
                else
                {
                    _instance.OnTokenRefresh += value;
                }
            }
            remove
            {
                if (_instance == null)
                {
                    _pendingRefresh.Remove(value);
                }
                else
                {
                    _instance.OnTokenRefresh -= value;
                }
            }
        }

        /// <summary>
        /// Event triggered when a user successfully logs in to the system.
        /// </summary>
        public static event Action UserLoggedIn
        {
            add
            {
                if (_instance == null)
                {
                    _pendingLogin.Add(value);
                }
                else
                {
                    _instance.UserLoggedIn += value;
                }
            }
            remove
            {
                if (_instance == null)
                {
                    _pendingLogin.Remove(value);
                }
                else
                {
                    _instance.UserLoggedIn -= value;
                }
            }
        }

        /// <summary>
        /// Event triggered when a user logs out of the system.
        /// </summary>
        public static event Action UserLoggedOut
        {
            add
            {
                if (_instance == null)
                {
                    _pendingLogout.Add(value);
                }
                else
                {
                    _instance.UserLoggedOut += value;
                }
            }
            remove
            {
                if (_instance == null)
                {
                    _pendingLogout.Remove(value);
                }
                else
                {
                    _instance.UserLoggedOut -= value;
                }
            }
        }

        public static Task<bool> InitializeAsync(string targetEnv, string appName, bool allowSignup,
            string clientId = "",
            string clientSecret = "",
            string scope = "")
        {
            if (Instance is null)
            {
                Debug.LogWarning(
                    $"{nameof(IGeniesLogin)} must be registered to {nameof(ServiceManager)} prior initializing.");
                return Task.FromResult(false);
            }

            return Instance.InitializeAPI(targetEnv, appName, allowSignup, clientId, clientSecret, scope);
        }

        /// <summary>
        /// Retrieves the unique identifier of the currently authenticated user.
        /// </summary>
        /// <returns>A task that completes with the user's unique identifier, or an empty string if not authenticated.</returns>
        public static Task<string> GetUserIdAsync()
        {
            return _instance != null ? Instance.GetUserId() : Task.FromResult("");
        }

        /// <summary>
        /// Checks if a user is currently signed in to the system.
        /// </summary>
        /// <returns>True if a user is signed in with valid tokens; otherwise, false.</returns>
        public static bool IsUserSignedIn()
        {
            return _instance != null && _instance.IsUserSignedIn();
        }

        /// <summary>
        /// Performs a global logout, terminating all user sessions across all devices.
        /// This is a more comprehensive logout that invalidates sessions everywhere.
        /// </summary>
        /// <returns>A task that completes with the logout response indicating success or failure.</returns>
        public static async Task<GeniesAuthMessage> GlobalLogOutAsync()
        {
            if (_instance != null)
            {
                return await Instance.GlobalLogOutAsync();

            }

            return new GeniesAuthMessage { ErrorMessage = "Auth instance was null", Status = "error" };
        }

        /// <summary>
        /// Logs out the current user from this device only, leaving other sessions intact.
        /// </summary>
        /// <returns>A task that completes with the logout response indicating success or failure.</returns>
        public static async Task<GeniesAuthMessage> LogOutAsync()
        {
            if (_instance != null)
            {
                return await Instance.LogOutAsync();
            }

            return  new GeniesAuthMessage { ErrorMessage = "Auth instance was null", Status = "error" };
        }

        /// <summary>
        /// Shuts down the login SDK and cleans up all resources.
        /// This should be called when the application is closing or when login functionality is no longer needed.
        /// </summary>
        /// <returns>A task that completes with the shutdown response indicating success or failure.</returns>
        public static Task<GeniesAuthShutdownResponse> ShutDownAsync()
        {
            if (_instance != null)
            {
                return Instance.ShutDownAsync();
            }

            var result = new GeniesAuthShutdownResponse();
            result.ResponseStatusCode = GeniesAuthShutdownResponse.StatusCode.ShutdownFailure;
            result.ErrorMessage = "Auth instance was null";

            return Task.FromResult(result);
        }

        /// <summary>
        /// Retrieves the username of the currently authenticated user.
        /// </summary>
        /// <returns>A task that completes with the user's username, or an empty string if not authenticated.</returns>
        public static Task<string> GetUsernameAsync()
        {
            return _instance != null ? Instance.GetUsernameAsync() : Task.FromResult("");
        }

        /// <summary>
        /// Saves or updates the user's birthdate in their profile.
        /// </summary>
        /// <param name="birthdate">The birthdate to save in a supported date format.</param>
        /// <returns>A task that completes with an attribute response indicating success or failure.</returns>
        public static Task<GeniesAuthAttributeResponse> SaveBirthdateAsync(string birthdate)
        {
            if (_instance != null)
            {
                return Instance.SaveBirthdate(birthdate);
            }

            var result = new GeniesAuthAttributeResponse();
            result.ResponseStatusCode = GeniesAuthAttributeResponse.StatusCode.UserAttributesUpdateFailed;
            result.ErrorMessage = "Auth instance was null";

            return Task.FromResult(result);
        }

        /// <summary>
        /// Updates the user's profile with the provided parameters.
        /// </summary>
        /// <param name="updateParameters">The profile update parameters containing the fields to update.</param>
        /// <returns>A task that completes with an attribute response indicating success or failure.</returns>
        public static Task<GeniesAuthAttributeResponse> UpdateProfileAsync(ProfileUpdateParameters updateParameters)
        {
            if (_instance != null)
            {
                return Instance.UpdateUserAttributesAsync(updateParameters);
            }

            var result = new GeniesAuthAttributeResponse
            {
                ResponseStatusCode = GeniesAuthAttributeResponse.StatusCode.UserAttributesUpdateFailed,
                ErrorMessage = "Auth instance was null",
            };

            return Task.FromResult(result);
        }

        /// <summary>
        /// Forces a refresh of the authentication tokens to extend the session.
        /// </summary>
        /// <param name="ct">Cancellation token to cancel the refresh operation.</param>
        /// <returns>A task that completes with an error message if refresh failed, or null if successful.</returns>
        public static Task<string> RefreshTokensAsync(CancellationToken ct = default)
        {
            return _instance != null ? Instance.RefreshTokens(ct) : Task.FromResult<string>(null);
        }

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow for phone number-based authentication.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public static IOtpLoginFlowController StartOtpLogin()
        {
            return Instance.StartOtpLogin();
        }

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow for email number-based authentication.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public static IEmailOtpLoginFlowController StartMagicLinkLogin()
        {
            return Instance.StartMagicLinkLogin();
        }

        /// <summary>
        /// Starts an OTP (One-Time Password) login flow for email number-based authentication.
        /// </summary>
        /// <returns>An OTP login flow controller for managing the authentication process.</returns>
        public static IHybridOtpLoginFlowController StartSmsOtpLogin()
        {
            return Instance.StartSmsOtpLogin();
        }
        
        /// <summary>
        /// Starts anonymous login
        /// </summary>
        /// <returns>An anonymous login flow controller.</returns>
        public static IAnonymousLoginFlowController StartAnonymousLogin()
        {
            return Instance.StartAnonymousLogin();
        }

        /// <summary>
        /// Starts an email/password login flow.
        /// </summary>
        /// <returns>A Password Login Flow Controller for managing the authentication process.</returns>
        public static IPasswordLoginFlowController StartPasswordLogin()
        {
            return Instance.StartPasswordLogin();
        }


        /// <summary>
        /// Attempts to automatically log in the user using cached credentials from a previous session.
        /// This is useful for providing seamless re-authentication when the app starts.
        /// </summary>
        /// <returns>A task that completes with true if instant login was successful; otherwise, false.</returns>
        public static async Task<bool> TryInstantLoginAsync()
        {
            var result = await Instance.TryInstantLoginAsync();
            return result.IsSuccessful;
        }

        /// <summary>
        /// Waits asynchronously until a user is logged in to the system.
        /// This method will block until <see cref="IsUserSignedIn"/> returns true.
        /// </summary>
        /// <returns>A task that completes when the user is logged in.</returns>
        public static async Task WaitUntilLoggedInAsync()
        {
            await UniTask.WaitUntil(IsUserSignedIn);
        }

        /// <summary>
        /// Retrieves all user attributes for the currently authenticated user.
        /// </summary>
        /// <returns>A task that completes with a dictionary of user attributes, or an empty dictionary if not authenticated.</returns>
        public static async Task<Dictionary<string, string>> GetUserAttributesAsync()
        {
            return _instance != null ? await Instance.GetUserAttributesAsync() : new();
        }

        /// <summary>
        /// Retrieves user attributes for a specific user by ID and username.
        /// </summary>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="username">The username of the user.</param>
        /// <returns>A task that completes with a dictionary of user attributes, or an empty dictionary if not authenticated.</returns>
        public static async Task<Dictionary<string, string>> GetUserAttributesAsync(string userID, string username)
        {
            return _instance != null ? await Instance.GetUserAttributesAsync(userID,username) : new();
        }

        /// <summary>
        /// Clears all cached credentials and authentication data from local storage.
        /// This is useful for debugging or when switching between different user accounts.
        /// </summary>
        /// <returns>A task that completes with a response indicating whether the cache was cleared successfully.</returns>
        public static async Task<GeniesAuthClearCachedCredentialsResponse> ResetCache()
        {
            return _instance != null ? await Instance.ClearCachedCredentials() : null;
        }

        /// <summary>
        /// Clears all cached credentials and authentication data from local storage.
        /// This is useful for debugging or when switching between different user accounts.
        /// Does not require the API to be initialized
        /// </summary>
        /// <returns>A task that completes with a response indicating whether the cache was cleared successfully.</returns>
        public static Task<bool> ResetCache(string baseUrl, string appName)
        {
            return _instance.ClearCachedCredentials(baseUrl, appName);
        }

        private static async void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus && _instance != null)
            {
                await RefreshTokensAsync();
            }
        }

        private static void ApplyPendingCallbacks()
        {
            if (_instance == null)
            {
                CrashReporter.LogError("Attempted to apply callbacks to a null login instance");
                return;
            }

            foreach (var callback in _pendingLogin)
            {
                _instance.UserLoggedIn += callback;
            }

            _pendingLogin.Clear();

            foreach (var callback in _pendingLogout)
            {
                _instance.UserLoggedOut += callback;
            }
            _pendingLogout.Clear();

            foreach (var callback in _pendingRefresh)
            {
                _instance.OnTokenRefresh += callback;
            }
            _pendingRefresh.Clear();
        }

#if UNITY_EDITOR
        private static async void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            if (_instance != null && _instance.IsInitialized)
            {
                Application.focusChanged -= OnFocusChanged;
                CrashReporter.LogInternal("Shutting down Genies Auth SDK");
                var result = await _instance.ShutDownAsync();

                if (!result.IsSuccessful)
                {
                    CrashReporter.LogError($"Failed to shutdown login SDK: {result.ErrorMessage}");
                }
            }

            _instance = null;
        }

        private static async void OnAssemblyReload()
        {
            if (_instance != null && _instance.IsInitialized)
            {
                Application.focusChanged -= OnFocusChanged;
                CrashReporter.LogInternal("Shutting down Genies Auth SDK");
                var result = await _instance.ShutDownAsync();

                if (!result.IsSuccessful)
                {
                    CrashReporter.LogError($"Failed to shutdown login SDK: {result.ErrorMessage}");
                }
            }
        }

        private const string _actionName = "Login";
        private const string _partnerIdentifier = "Genies.com";
        private const string _prefsKet = "Genies.VSAttributionSent";
        private static async void FireAnaylticsOnUserLoggedIn()
        {
            // Event should be sent only once
            if (PlayerPrefs.HasKey(_prefsKet))
            {
                return;
            }

            PlayerPrefs.SetInt(_prefsKet,1);
            PlayerPrefs.Save();

            var userId = await GetUserIdAsync();
            AnalyticsResult result = VS.VSAttribution.SendAttributionEvent(_actionName, _partnerIdentifier, userId);
            Debug.Log($"VSAttribution result {result}");
        }

#endif
    }
}
