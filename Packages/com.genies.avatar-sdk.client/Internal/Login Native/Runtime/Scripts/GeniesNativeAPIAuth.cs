using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login;
using Genies.Login.Anonymous;
using Genies.Login.AuthMessages;
using Genies.Login.EmailOtp;
using Genies.Login.Native.Data;
using Genies.Login.Otp;
using Genies.Login.Password;
using UnityEngine;

namespace Genies.NativeAPI
{
    /// <summary>
    /// Unity-side wrapper around the native GeniesAuthAPI.
    /// Handles interop, request de-duplication, and token refresh scheduling.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesNativeAPIAuth : IGeniesLogin
#else
    public class GeniesNativeAPIAuth : IGeniesLogin
#endif
    {
        // =========================================================
        // Constants / Fields
        // =========================================================

        private const string SUCCESS = "success";
        private const int REFRESH_OFFSET = 5;

        private readonly ConcurrentDictionary<string, Task<object>> _ongoingRequests = new();

        private Task _tokenExpiryTask;
        private CancellationTokenSource _tokenExpiryCts;

#if UNITY_IOS && !UNITY_EDITOR
        private const string DllName = "__Internal";
#else
        private const string DllName = "GeniesAuthAPI";
#endif

        // =========================================================
        // Events / State / Properties
        // =========================================================

        public event Action UserLoggedIn;
        public event Action UserLoggedOut;
        public event Action<string> OnTokenRefresh;

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public string RefreshToken
        {
            get => IsInitialized ? Utf8FromNativeAndFree(GetRefreshToken_NativeAPI()) : string.Empty;
            set
            {
                /* intentionally no-op: maintained for interface compatibility */
            }
        }

        public string AuthAccessToken =>
            IsInitialized ? Utf8FromNativeAndFree(GetAccessToken_NativeAPI()) : string.Empty;

        public string AuthIdToken =>
            IsInitialized ? Utf8FromNativeAndFree(GetIdToken_NativeAPI()) : string.Empty;

        // =========================================================
        // Native Interop
        // =========================================================

        // ---------- lifecycle ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Initialize_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string baseUrl,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string appName,
            [MarshalAs(UnmanagedType.I1)] bool allowSignup,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string clientId = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string clientSecret= "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string scope= "");

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ShutDown_NativeAPI();

        // ---------- interop helpers ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeBuffer(IntPtr p);

        // ---------- getters ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetIdToken_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetAccessToken_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetRefreshToken_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetUserID_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetUserName_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool CheckAccessTokenValidity_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetTimeUntilTokensExpire_NativeAPI();

        // ---------- auth / session ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr InitiateAuth_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phoneNumber,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string challengeType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr RespondToChallenge_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string challengeResponse);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ResendChallenge_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phoneNumber);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr TryRestoreSession_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ForceRefreshTokens_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ClearStoredCredentials_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]

        private static extern bool ClearStoredCredentialsWithoutInitializing_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string baseUrl,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string appName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LocalLogOut_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GlobalLogOut_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr DeleteAccount_NativeAPI();

        // ---------- profile ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetUserProfile_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetOtherUserProfile_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string userId = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string userName = "");

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr UpdateUserProfile_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phoneNumber = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string bio = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string birthday = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string username = "",
            int createdAt = 0,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string geniesId = "",
            int updatedAt = 0,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string profileImageUrl = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string firstName = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string lastName = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dapperId = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string flowAddress = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string legalFirstName = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string legalLastName = "",
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dollName = ""
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ApisAreReady_NativeAPI();

        // ---------- v2: magic link / password ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr StartMagicAuth_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phone);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr VerifyMagicAuth_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phone,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string code);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ResendMagicAuth_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phone);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SignUpV2_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string phone,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string birthday,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string firstName,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string lastName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SignInV2_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr VerifyEmailV2_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string code);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LogoutV2_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr DeleteAccountV2_NativeAPI();

        // ---------- Anonymous ----------
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AnonymousSignUp_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string applicationId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AnonymousRefresh_NativeAPI();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AnonymousUpgrade_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string birthday,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string firstName,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string lastName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr UpgradeUserV1_NativeAPI(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string email);

        // =========================================================
        // Construction / Initialization / Disposal
        // =========================================================

        public IOtpLoginFlowController StartOtpLogin() => new GeniesNativeAPIOTPFlowController(this);
        public IEmailOtpLoginFlowController StartMagicLinkLogin() => new GeniesNativeApiEmailOtpLoginFlowController(this);
        
        public IHybridOtpLoginFlowController StartSmsOtpLogin() => new GeniesNativeAPIPhoneOtpLoginFlowController(this);
        
        public IHybridOtpLoginFlowController StartHybridOtpLogin() => new GeniesNativeAPIHybirdOtpLoginFlowController(this);
        public IPasswordLoginFlowController StartPasswordLogin() => new GeniesNativeAPIPasswordFlowController(this);
        public IAnonymousLoginFlowController StartAnonymousLogin() => new GeniesNativeAPIAnonymousFlowController(this);

        public Task<bool> InitializeAPI(string environment, string appName, bool allowSignup = false,
                                        string clientId = "", string clientSecret = "", string scope = "")
        {
#if (UNITY_ANDROID && !UNITY_EDITOR)
            InitializeAndroidHelper();
#endif
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                var settings = GeniesAuthSettings.LoadFromResources();

                if (settings != null)
                {
                    if (string.IsNullOrWhiteSpace(clientId))
                    {
                        clientId = settings.ClientId;
                    }

                    if (string.IsNullOrWhiteSpace(clientSecret))
                    {
                        clientSecret = settings.ClientSecret;
                    }
                }
            }

            Initialize_NativeAPI(environment, appName, allowSignup, clientId, clientSecret, scope);
            IsInitialized = true;
            return Task.FromResult(true);
        }

        public async Task<GeniesAuthShutdownResponse> ShutDownAsync()
        {
            UserLoggedOut?.Invoke();
            IsInitialized = false;
            _tokenExpiryCts?.Cancel();
            _tokenExpiryTask = null;

            OnTokenRefresh = null;
            UserLoggedIn = null;
            UserLoggedOut = null;

            ShutDown_NativeAPI();
            return await Task.FromResult(new GeniesAuthShutdownResponse { Status = "success" });
        }

        public void Dispose()
        {
            _ = ShutDownAsync();
            IsDisposed = true;
        }

#if (UNITY_ANDROID && !UNITY_EDITOR)
        private void InitializeAndroidHelper()
        {
            try
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                var androidHelperClass = new AndroidJavaClass("com.genies.androidhelper.AndroidHelper");
                androidHelperClass.CallStatic("initialize", currentActivity);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error initializing AndroidHelper: " + ex.Message);
            }
        }
#endif

        // =========================================================
        // Public Queries / Helpers
        // =========================================================

        public async Task<string> GetUsernameAsync()
        {
            if (!IsInitialized)
            {
                return await Task.FromResult(string.Empty);
            }

            return await Task.FromResult(Utf8FromNativeAndFree(GetUserName_NativeAPI()));
        }

        public Task<string> GetUserId()
        {
            if (!IsInitialized)
            {
                return Task.FromResult(string.Empty);
            }

            return Task.FromResult(Utf8FromNativeAndFree(GetUserID_NativeAPI()));
        }

        public bool IsUserSignedIn()
        {
            var userId = IsInitialized ? Utf8FromNativeAndFree(GetUserID_NativeAPI()) : string.Empty;
            return !string.IsNullOrWhiteSpace(AuthAccessToken)
                   && CheckAccessTokenValidity_NativeAPI()
                   && !string.IsNullOrWhiteSpace(userId)
                   && ApisAreReady_NativeAPI();
        }

        public async Task<Dictionary<string, string>> GetUserAttributesAsync()
        {
            if (!IsInitialized)
            {
                return null;
            }

            var attributesResponse =
                await RunFunctionExclusiveAsync<GeniesAuthDictionaryMessage>(
                    nameof(GetUserAttributesAsync),
                    GetUserProfile_NativeAPI);

            return attributesResponse.ToDictionary();
        }

        public async Task<Dictionary<string, string>> GetUserAttributesAsync(string userId = "", string userName = "")
        {
            if (!IsInitialized)
            {
                return null;
            }

            var attributesResponse =
                await RunFunctionExclusiveAsync<GeniesAuthDictionaryMessage>(
                    nameof(GetUserAttributesAsync),
                    () => GetOtherUserProfile_NativeAPI(userId, userName));

            return attributesResponse.ToDictionary();
        }

        public async Task<GeniesAuthAttributeResponse> SaveBirthdate(string birthdate)
        {
            return await RunFunctionExclusiveAsync<GeniesAuthAttributeResponse>(
                nameof(SaveBirthdate),
                () => UpdateUserProfile_NativeAPI(birthday: birthdate));
        }

        public async Task<GeniesAuthAttributeResponse> UpdateUserAttributesAsync(
            ProfileUpdateParameters updateParameters)
        {
            return await RunFunctionExclusiveAsync<GeniesAuthAttributeResponse>(
                nameof(UpdateUserAttributesAsync),
                () => UpdateUserProfile_NativeAPI(
                    updateParameters.Email,
                    updateParameters.PhoneNumber,
                    updateParameters.Bio,
                    updateParameters.Birthday,
                    updateParameters.Username,
                    updateParameters.CreatedAt,
                    updateParameters.GeniesId,
                    updateParameters.UpdatedAt,
                    updateParameters.ProfileImageUrl,
                    updateParameters.FirstName,
                    updateParameters.LastName,
                    updateParameters.DapperId,
                    updateParameters.FlowAddress,
                    updateParameters.LegalFirstName,
                    updateParameters.LegalLastName,
                    updateParameters.DollName));
        }

        public async Task<GeniesAuthMessage> DeleteUserAccount()
        {
            if (!IsInitialized)
            {
                return new GeniesAuthMessage
                {
                    Message = "Not initialized",
                    ErrorMessage = "Not initialized",
                    Status = "error",
                    StatusCodeString = "NOT_INITIALIZED"
                };
            }

            return await RunFunctionExclusiveAsync<GeniesAuthMessage>(
                nameof(DeleteUserAccount),
                DeleteAccount_NativeAPI);
        }

        // =========================================================
        // Session / Tokens
        // =========================================================

        public async Task<string> RefreshTokens(CancellationToken ct = default)
        {
            if (!IsInitialized || !ApisAreReady_NativeAPI())
            {
                return string.Empty;
            }

            var authMessage =
                await RunFunctionExclusiveAsync<GeniesAuthForceTokenRefreshResponse>(
                    nameof(RefreshTokens),
                    ForceRefreshTokens_NativeAPI);

            if (authMessage.Status == SUCCESS)
            {
                OnTokenRefresh?.Invoke(AuthAccessToken);
                _tokenExpiryCts?.Cancel();
                _tokenExpiryTask = null;
                StartTokenExpiryTimer();
                return string.Empty;
            }

            return authMessage.ErrorMessage;
        }

        public async Task<GeniesAuthInstantLoginResponse> TryInstantLoginAsync()
        {
            if (!IsInitialized)
            {
                return new GeniesAuthInstantLoginResponse
                {
                    ErrorMessage = "Initialization not completed",
                    Status = "error",
                    ResponseStatusCode = GeniesAuthInstantLoginResponse.StatusCode.None
                };
            }

            var result = await RunFunctionExclusiveAsync<GeniesAuthInstantLoginResponse>(
                nameof(TryInstantLoginAsync),
                TryRestoreSession_NativeAPI);

            var didSucceed = IsUserSignedIn() && result.IsSuccessful;

            if (didSucceed)
            {
                InvokeOnUserLoggedIn();
                StartTokenExpiryTimer();
                return result;
            }

            return new GeniesAuthInstantLoginResponse
            {
                ErrorMessage = "Invalid session",
                Status = "error",
                ResponseStatusCode = GeniesAuthInstantLoginResponse.StatusCode.SessionNotFound
            };
        }

        public async Task<GeniesAuthLogoutResponse> LogOutAsync()
        {
            if (!IsInitialized)
            {
                return new GeniesAuthLogoutResponse
                {
                    ErrorMessage = "Initialization not completed",
                    Status = "error",
                    ResponseStatusCode = GeniesAuthLogoutResponse.StatusCode.LogoutError
                };
            }

            // Local logout: wipe credentials
            var result = await RunFunctionExclusiveAsync<GeniesAuthLogoutResponse>(
                nameof(LogOutAsync),
                LocalLogOut_NativeAPI);

            if (result.IsSuccessful)
            {
                UserLoggedOut?.Invoke();
                _tokenExpiryCts?.Cancel();
                _tokenExpiryTask = null;
            }

            return result;
        }

        public async Task<GeniesAuthLogoutResponse> GlobalLogOutAsync()
        {
            if (!IsInitialized)
            {
                return new GeniesAuthLogoutResponse
                {
                    ErrorMessage = "Initialization not completed",
                    Status = "error",
                    ResponseStatusCode = GeniesAuthLogoutResponse.StatusCode.LogoutError
                };
            }

            // Server-side logout; clears local credentials
            var result = await RunFunctionExclusiveAsync<GeniesAuthLogoutResponse>(
                nameof(LogOutAsync),
                GlobalLogOut_NativeAPI);

            if (result.IsSuccessful)
            {
                UserLoggedOut?.Invoke();
                _tokenExpiryCts?.Cancel();
                _tokenExpiryTask = null;
            }

            return result;
        }

        public async Task<GeniesAuthClearCachedCredentialsResponse> ClearCachedCredentials()
        {
            if (!IsInitialized)
            {
                return new GeniesAuthClearCachedCredentialsResponse
                {
                    ErrorMessage = "Initialization not completed",
                    Status = "error",
                    ResponseStatusCode = GeniesAuthClearCachedCredentialsResponse.StatusCode.CredentialsError
                };
            }

            return await RunFunctionExclusiveAsync<GeniesAuthClearCachedCredentialsResponse>(
                nameof(ClearCachedCredentials),
                ClearStoredCredentials_NativeAPI);
        }

        public Task<bool> ClearCachedCredentials(string baseUrl, string appName)
        {
            return Task.FromResult(ClearStoredCredentialsWithoutInitializing_NativeAPI(baseUrl, appName));
        }

        public void StartTokenExpiryTimer()
        {
            if (!IsInitialized || (_tokenExpiryTask != null && !_tokenExpiryTask.IsCompleted))
            {
                return;
            }

            var seconds = GetTimeUntilTokensExpire_NativeAPI();
            if (seconds <= 0)
            {
                seconds = REFRESH_OFFSET + 5;
            }

            var delay = Math.Max(1, seconds - REFRESH_OFFSET);
            _tokenExpiryCts = new CancellationTokenSource();
            _tokenExpiryTask = RunTokenTimerAsync(delay, _tokenExpiryCts.Token);
        }

        // =========================================================
        // OTP / Magic Link / Password (v1/v2) — Public Facades
        // =========================================================

        internal Task<GeniesAuthInitiateOtpSignInResponse> BeginOtpLoginAsync(string phoneNumber) =>
            RunFunctionExclusiveAsync<GeniesAuthInitiateOtpSignInResponse>(
                nameof(BeginOtpLoginAsync),
                () => InitiateAuth_NativeAPI(phoneNumber, "sms"));

        internal Task<GeniesAuthSendOtpResponse> RespondToOtpAsync(string code) =>
            RunFunctionExclusiveAsync<GeniesAuthSendOtpResponse>(
                nameof(RespondToOtpAsync),
                () => RespondToChallenge_NativeAPI(code));

        internal Task<GeniesAuthOtpRefreshResponse> ResendOTPAsync(string phoneNumber)
        {
            if (!IsInitialized)
            {
                return Task.FromResult(new GeniesAuthOtpRefreshResponse
                {
                    ErrorMessage = "Initialization not completed",
                    Status = "error",
                    ResponseStatusCode = GeniesAuthOtpRefreshResponse.StatusCode.None
                });
            }

            return RunFunctionExclusiveAsync<GeniesAuthOtpRefreshResponse>(
                nameof(ResendOTPAsync),
                () => ResendChallenge_NativeAPI(phoneNumber));
        }

        // --- v2 wrappers ---

        internal Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email) =>
            RunFunctionExclusiveAsync<GeniesAuthUpgradeV1Response>(
                nameof(UpgradeUserV1Async),
                () => UpgradeUserV1_NativeAPI(email));

        internal Task<GeniesAuthStartEmailOtpResponse> StartMagicAuthAsync(string email) =>
            RunFunctionExclusiveAsync<GeniesAuthStartEmailOtpResponse>(
                nameof(StartMagicAuthAsync),
                () => StartMagicAuth_NativeAPI(email, string.Empty));
        
        internal Task<GeniesAuthStartHybridOtpResponse> StartMagicAuthWithPhoneAsync(string phone) =>
            RunFunctionExclusiveAsync<GeniesAuthStartHybridOtpResponse>(
                nameof(StartMagicAuthWithPhoneAsync),
                () => StartMagicAuth_NativeAPI(string.Empty, phone ?? string.Empty));
        
        internal Task<GeniesAuthVerifyMagicLinkResponse> VerifyMagicAuthAsync(string email, string code) =>
            RunFunctionExclusiveAsync<GeniesAuthVerifyMagicLinkResponse>(
                nameof(VerifyMagicAuthAsync),
                () => VerifyMagicAuth_NativeAPI(email ?? string.Empty, string.Empty, code ?? string.Empty));

        internal Task<GeniesAuthVerifyMagicLinkResponse> VerifyMagicAuthWithPhoneAsync(string phone, string code) =>
            RunFunctionExclusiveAsync<GeniesAuthVerifyMagicLinkResponse>(
                nameof(VerifyMagicAuthWithPhoneAsync),
                () => VerifyMagicAuth_NativeAPI(string.Empty, phone ?? string.Empty, code ?? string.Empty));

        internal Task<GeniesAuthResendMagicLinkResponse> ResendMagicAuthAsync(string email) =>
            RunFunctionExclusiveAsync<GeniesAuthResendMagicLinkResponse>(
                nameof(ResendMagicAuthAsync),
                () => ResendMagicAuth_NativeAPI(email ?? string.Empty, string.Empty));

        internal Task<GeniesAuthResendMagicLinkResponse> ResendMagicAuthWithPhoneAsync(string phone) =>
            RunFunctionExclusiveAsync<GeniesAuthResendMagicLinkResponse>(
                nameof(ResendMagicAuthWithPhoneAsync),
                () => ResendMagicAuth_NativeAPI(string.Empty, phone ?? string.Empty));

        internal Task<GeniesAuthSignUpV2Response> SignUpV2Async(
            string email, string password = "", string birthday = "", string firstName = "", string lastName = "") =>
            RunFunctionExclusiveAsync<GeniesAuthSignUpV2Response>(
                nameof(SignUpV2Async),
                () => SignUpV2_NativeAPI(
                    email ?? string.Empty,
                    string.Empty, // phone (unused in this email-based flow)
                    password ?? string.Empty,
                    birthday ?? string.Empty,
                    firstName ?? string.Empty,
                    lastName ?? string.Empty));

        internal Task<GeniesAuthSignUpV2Response> SignUpV2WithPhoneAsync(
            string phone, string password = "", string birthday = "", string firstName = "", string lastName = "") =>
            RunFunctionExclusiveAsync<GeniesAuthSignUpV2Response>(
                nameof(SignUpV2WithPhoneAsync),
                () => SignUpV2_NativeAPI(
                    string.Empty,
                    phone ?? string.Empty,
                    password ?? string.Empty,
                    birthday ?? string.Empty,
                    firstName ?? string.Empty,
                    lastName ?? string.Empty));
        
        internal Task<GeniesAuthSignInV2Response> SignInV2Async(string email, string password) =>
            RunFunctionExclusiveAsync<GeniesAuthSignInV2Response>(
                nameof(SignInV2Async),
                () => SignInV2_NativeAPI(email, password));

        internal Task<GeniesAuthVerifyEmailV2Response> VerifyEmailV2Async(string code) =>
            RunFunctionExclusiveAsync<GeniesAuthVerifyEmailV2Response>(
                nameof(VerifyEmailV2Async),
                () => VerifyEmailV2_NativeAPI(code));

        internal Task<GeniesAuthLogoutResponse> LogoutV2Async() =>
            RunFunctionExclusiveAsync<GeniesAuthLogoutResponse>(
                nameof(LogoutV2Async),
                LogoutV2_NativeAPI);

        // The existing delete account function (DeleteUserAccount) will call the correct
        // version in native depending on the type of token we have.
        //internal Task<GeniesAuthMessage> DeleteAccountV2Async() =>
        //    RunFunctionExclusiveAsync<GeniesAuthMessage>(
        //        nameof(DeleteAccountV2Async),
        //        DeleteAccountV2_NativeAPI);

        // --- anonymous wrappers ---
        internal Task<GeniesAuthAnonymousResponse> AnonymousSignUpAsync(string applicationId) =>
            RunFunctionExclusiveAsync<GeniesAuthAnonymousResponse>(
                nameof(AnonymousSignUpAsync),
                () => AnonymousSignUp_NativeAPI(applicationId));

        internal Task<GeniesAuthAnonymousResponse> AnonymousRefreshAsync() =>
            RunFunctionExclusiveAsync<GeniesAuthAnonymousResponse>(
                nameof(AnonymousRefreshAsync),
                AnonymousRefresh_NativeAPI);

        internal Task<GeniesAuthAnonymousResponse> AnonymousUpgradeAsync(
            string email, string birthday = "", string firstName = "", string lastName = "") =>
            RunFunctionExclusiveAsync<GeniesAuthAnonymousResponse>(
                nameof(AnonymousUpgradeAsync),
                () => AnonymousUpgrade_NativeAPI(email ?? "", birthday ?? "", firstName ?? "", lastName ?? ""));

        public void InvokeOnUserLoggedIn()
        {
            UserLoggedIn?.Invoke();
        }

        // =========================================================
        // Private Helpers
        // =========================================================

        private static string Utf8FromNativeAndFree(IntPtr p)
        {
            if (p == IntPtr.Zero)
            {
                return string.Empty;
            }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1 || UNITY_2022_1_OR_NEWER
            try
            {
                return Marshal.PtrToStringUTF8(p) ?? string.Empty;
            }
            finally
            {
                FreeBuffer(p);
            }
#else
            try
            {
                int len = 0;
                while (Marshal.ReadByte(p, len) != 0)
                {
                    len++;
                }

                var bytes = new byte[len];
                Marshal.Copy(p, bytes, 0, len);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            finally
            {
                FreeBuffer(p);
            }
#endif
        }

        private async Task RunTokenTimerAsync(int seconds, CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), ct);

                if (!ct.IsCancellationRequested && !string.IsNullOrEmpty(AuthAccessToken))
                {
                    OnTokenRefresh?.Invoke(AuthAccessToken);
                }

                _tokenExpiryTask = null;
                StartTokenExpiryTimer();
            }
            catch (TaskCanceledException)
            {
                CrashReporter.LogInternal("[GeniesNativeAPIAuth] Token refresh timer canceled.");
            }
        }

        // =========================================================
        // Concurrency Gate / JSON Marshal
        // =========================================================

        private async Task<T> RunFunctionExclusiveAsync<T>(string requestKey, Func<IntPtr> nativeFunction)
            where T : class
        {
            if (_ongoingRequests.TryGetValue(requestKey, out var existingTask))
            {
                return (T)await existingTask;
            }

            var task = Task.Run<object>(() =>
            {
                var resultPtr = nativeFunction();
                var resultJson = Utf8FromNativeAndFree(resultPtr);

                CrashReporter.LogInternal($"[GeniesNativeAPIAuth] resultJson: {resultJson}");

                try
                {
                    return JsonUtility.FromJson<T>(resultJson);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse JSON response: {ex.Message}");
                }
            });

            _ongoingRequests.TryAdd(requestKey, task);

            try
            {
                return (T)await task;
            }
            finally
            {
                _ongoingRequests.TryRemove(requestKey, out _);
            }
        }
    }
}
