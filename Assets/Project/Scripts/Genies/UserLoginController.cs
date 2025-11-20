using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Genies.Sdk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Components.Accounts
{
    public enum LOGIN_STATE
    {
        INITIAL_NULL_STATE,
        WAIT_VERIFY_CACHED_DATA,
        GET_USER_EMAIL,
        WAIT_VERIFY_EMAIL,
        GET_USER_OTP,
        WAIT_VERIFY_OTP,
        SOMETHING_WENT_WRONG,
        LOGGED_IN_STATE,
        NO_USER_ACCOUNT
    }

    public sealed class UserLoginController : MonoBehaviour
    {
        // Public events
        public event Action OnLoginSuccessful;
        public event Action OnLogOutSuccessful;
        public event Action OnLoginUiClosed;
        public event Action OnLoginStateAborted;

        // BACKEND CONFIG
        [SerializeField] private NetworkConnectionChecker networkConnectionChecker;

        // UI REFERENCES
        [SerializeField] private GameObject userLoginUiRoot;
        [SerializeField] private Text status;
        [SerializeField] private TextMeshProUGUI userIdLabel;
        [SerializeField] private InputField inputField;
        [SerializeField] private Text inputFieldPlaceholder;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button useWithoutAccountButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text skipButtonText;
        [SerializeField] private Button backButton;
        [SerializeField] private Text submitButton_dynamicLabelText;
        [SerializeField] private GameObject loadingDots;
        [SerializeField] private bool _doAutoInitialize;

        private LOGIN_STATE _currState = LOGIN_STATE.INITIAL_NULL_STATE;
        private Dictionary<LOGIN_STATE, Action> _stateToSetupFunction;
        private Dictionary<LOGIN_STATE, Action> _stateToCleanupFunction;

        private bool _isLoggedIn;
        public bool IsLoggedIn => _isLoggedIn;

        private bool _didInitialize;

        private float _minLoginDisplayTimer = 1.5f;
        private const float _spinOutAnimationTime = 0.15f;

        private bool _isLoginUiOpen => userLoginUiRoot != null && userLoginUiRoot.activeInHierarchy;

        private string _currUserId = string.Empty;
        public string UserId => _currUserId;

        // Button strings
        private const string _btnText_confirmOtp   = "confirm code";
        private const string _btnText_tryAgain     = "try again";
        private const string _btnText_logOut       = "log out";
        private const string _btnText_cancel       = "cancel";
        private const string _btnText_createAccount = "create account";
        private const string _btnText_submitEmail  = "submit email";

        // Status strings
        private const string _statusText_waitVerifyCachedData = "attempting to log in with stored data...";
        private const string _statusText_getUserEmail         = "please enter your email address";
        private const string _statusText_invalidUserEmail     = "please enter a valid email address";

        private const string _statusText_waitVerifyEmail = "sending verification code to your email...";
        private const string _statusText_noUserAccount =
            "<b>no account found.</b>\nplease sign up through the 'Genies Party' app,\nor contact devrelations@genies.com";

        private const string _statusText_waitVerifyOtp = "confirming your code...";
        private const string _statusText_invalidUserOtp = "expecting 6 digit code";
        private const string _statusText_loggedIn = "great to see you!";

        // For minimum display timing of the "sending code" state
        private float _emailRequestStartTime;
        private const float _minEmailRequestDisplayTime = 1.0f;

        // ==============================================================
        // Lifecycle
        // ==============================================================

        private void Start()
        {
            if (_doAutoInitialize)
            {
                Initialize();
            }
        }

        public async void Initialize()
        {
            if (_didInitialize)
            {
                return;
            }

            InitializeUiStateMachine();
            SubscribeAvatarSdkEvents();

            // Initial UI: try cached login
            ChangeToState(LOGIN_STATE.WAIT_VERIFY_CACHED_DATA);

            await AvatarSdk.InitializeAsync();
            var instantLoginResult = await AvatarSdk.TryInstantLoginAsync();

            if (!instantLoginResult.isLoggedIn)
            {
                ChangeToState(LOGIN_STATE.GET_USER_EMAIL);
            }
        
            _didInitialize = true;
        }

        private void OnEnable()
        {
            if (!_didInitialize)
            {
                return;
            }

            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnUserRequestSkip);

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnUserRequestBack);
        }

        private void Update()
        {
            if (_minLoginDisplayTimer >= 0f)
            {
                _minLoginDisplayTimer -= Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            if (!_didInitialize)
            {
                return;
            }

            skipButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveListener(OnUserRequestBack);

            networkConnectionChecker.NetworkConnectionStateChanged -= OnNetworkConnectionChanged;

            UnsubscribeAvatarSdkEvents();
        }

        // ==============================================================
        // AvatarSdk Events
        // ==============================================================

        private void SubscribeAvatarSdkEvents()
        {
            AvatarSdk.Events.UserLoggedIn += OnAvatarUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut += OnAvatarUserLoggedOut;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded += OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed += OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded += OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed += OnEmailCodeSubmissionFailed;
        }

        private void UnsubscribeAvatarSdkEvents()
        {
            AvatarSdk.Events.UserLoggedIn -= OnAvatarUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnAvatarUserLoggedOut;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded -= OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed -= OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded -= OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed -= OnEmailCodeSubmissionFailed;
        }

        private async void OnAvatarUserLoggedIn()
        {
#if CREATOR_BUILD
            _currUserId = AppManager.Instance.UserId;
#else
            _currUserId = await AvatarSdk.GetUserIdAsync();
#endif
            userIdLabel.text = _currUserId;

            _isLoggedIn = true;
            OnLoginSuccessful?.Invoke();

            // Skip button becomes "Close"
            skipButton.onClick.RemoveListener(OnUserRequestSkip);
            skipButton.onClick.AddListener(HideLoginUI);

            if (_minLoginDisplayTimer > 0f)
            {
                StartCoroutine(HideLoginUIAfterTime(_minLoginDisplayTimer));
            }
            else
            {
                HideLoginUI();
            }

            ChangeToState(LOGIN_STATE.LOGGED_IN_STATE);
        }

        private void OnAvatarUserLoggedOut()
        {
            _isLoggedIn = false;
            _currUserId = string.Empty;
            userIdLabel.text = string.Empty;

            OnLogOutSuccessful?.Invoke();

            ChangeToState(LOGIN_STATE.GET_USER_EMAIL);
        }

        // ==============================================================
        // Email OTP event handlers
        // ==============================================================

        private void OnEmailCodeRequestSucceeded(string email)
        {
            StartCoroutine(HandleEmailCodeRequestSucceededAfterDelay(email));
        }

        private IEnumerator HandleEmailCodeRequestSucceededAfterDelay(string email)
        {
            var elapsed = Time.realtimeSinceStartup - _emailRequestStartTime;
            if (elapsed < _minEmailRequestDisplayTime)
            {
                yield return new WaitForSeconds(_minEmailRequestDisplayTime - elapsed);
            }

            loadingDots.SetActive(false);
            ChangeToState(LOGIN_STATE.GET_USER_OTP);
            status.text = $"a verification code was sent to {email}.";
        }

        private void OnEmailCodeRequestFailed((string email, string failReason) fail)
        {
            StartCoroutine(HandleEmailCodeRequestFailedAfterDelay(fail));
        }

        private IEnumerator HandleEmailCodeRequestFailedAfterDelay((string email, string failReason) fail)
        {
            var elapsed = Time.realtimeSinceStartup - _emailRequestStartTime;
            if (elapsed < _minEmailRequestDisplayTime)
            {
                yield return new WaitForSeconds(_minEmailRequestDisplayTime - elapsed);
            }

            loadingDots.SetActive(false);

            if (!string.IsNullOrEmpty(fail.failReason) &&
                fail.failReason.IndexOf("failed to get user by email", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ChangeToState(LOGIN_STATE.NO_USER_ACCOUNT);
                status.text = _statusText_noUserAccount;
            }
            else
            {
                ChangeToState(LOGIN_STATE.SOMETHING_WENT_WRONG);
                status.text = string.IsNullOrWhiteSpace(fail.failReason)
                    ? "could not send verification email. please try again."
                    : fail.failReason;
            }
        }

        private void OnEmailCodeSubmissionSucceeded(string code)
        {
            loadingDots.SetActive(false);
            status.text = "code accepted. finalizing login...";
            // UserLoggedIn event will finish the flow.
        }

        private void OnEmailCodeSubmissionFailed((string code, string failReason) fail)
        {
            loadingDots.SetActive(false);
            ChangeToState(LOGIN_STATE.SOMETHING_WENT_WRONG);

            status.text = string.IsNullOrWhiteSpace(fail.failReason)
                ? "verification failed. please try again."
                : fail.failReason;
        }

        // ==============================================================
        // Initialization / state machine setup
        // ==============================================================

        private void InitializeUiStateMachine()
        {
            skipButton.onClick.AddListener(OnUserRequestSkip);
            backButton.onClick.AddListener(OnUserRequestBack);

            inputField.contentType = InputField.ContentType.Standard;
            inputField.lineType = InputField.LineType.SingleLine;

            networkConnectionChecker.NetworkConnectionStateChanged += OnNetworkConnectionChanged;

            _stateToSetupFunction = new Dictionary<LOGIN_STATE, Action>
            {
                { LOGIN_STATE.INITIAL_NULL_STATE,        CleanupContextualUiElements },
                { LOGIN_STATE.WAIT_VERIFY_CACHED_DATA,   SetupWaitVerifyCachedDataState },
                { LOGIN_STATE.GET_USER_EMAIL,            SetupGetUserEmailState },
                { LOGIN_STATE.WAIT_VERIFY_EMAIL,         SetupWaitVerifyEmailState },
                { LOGIN_STATE.GET_USER_OTP,              SetupGetUserOtpState },
                { LOGIN_STATE.WAIT_VERIFY_OTP,           SetupWaitVerifyOtpState },
                { LOGIN_STATE.SOMETHING_WENT_WRONG,      SetupSomethingWrongState },
                { LOGIN_STATE.NO_USER_ACCOUNT,           SetupNoUserAccountState },
                { LOGIN_STATE.LOGGED_IN_STATE,           SetupLoggedInState }
            };

            _stateToCleanupFunction = new Dictionary<LOGIN_STATE, Action>
            {
                { LOGIN_STATE.INITIAL_NULL_STATE,        CleanupContextualUiElements },
                { LOGIN_STATE.WAIT_VERIFY_CACHED_DATA,   CleanupWaitVerifyCachedDataState },
                { LOGIN_STATE.GET_USER_EMAIL,            CleanupGetUserEmailState },
                { LOGIN_STATE.WAIT_VERIFY_EMAIL,         CleanupContextualUiElements },
                { LOGIN_STATE.GET_USER_OTP,              CleanupGetUserOtpState },
                { LOGIN_STATE.WAIT_VERIFY_OTP,           CleanupContextualUiElements },
                { LOGIN_STATE.SOMETHING_WENT_WRONG,      CleanupSomethingWrongState },
                { LOGIN_STATE.NO_USER_ACCOUNT,           CleanupNoUserAccountState },
                { LOGIN_STATE.LOGGED_IN_STATE,           CleanupLoggedInState }
            };
        }

        private void ChangeToState(LOGIN_STATE nextState)
        {
            if (_stateToCleanupFunction.TryGetValue(_currState, out var cleanup))
            {
                cleanup?.Invoke();
            }

            _currState = nextState;

            if (_stateToSetupFunction.TryGetValue(_currState, out var setup))
            {
                setup?.Invoke();
            }
        }

        private void OnNetworkConnectionChanged(NetworkConnectionState newState)
        {
            if (_isLoginUiOpen && newState == NetworkConnectionState.NotConnected)
            {
                AbortUserLoginState();
                HideLoginUI();
            }
        }

        private async void LogOutUser()
        {
            await AvatarSdk.LogOutAsync();
            // OnAvatarUserLoggedOut will handle UI/state.
        }

        // ==============================================================
        // STATE MACHINE SETUP / CLEANUP
        // ==============================================================

        private void CleanupContextualUiElements()
        {
            inputField.gameObject.SetActive(false);

            submitButton.gameObject.SetActive(false);
            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(false);
            }

            backButton.gameObject.SetActive(false);

            inputField.DeactivateInputField();
            loadingDots.SetActive(false);
        }

        private void CleanupWaitVerifyCachedDataState()
        {
            CleanupContextualUiElements();
            submitButton.onClick.RemoveListener(OnUserRequestResetLoginState);
        }

        private void SetupWaitVerifyCachedDataState()
        {
            submitButton.gameObject.SetActive(true);

            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(true);
                submitButton_dynamicLabelText.text = _btnText_cancel;
            }

            submitButton.onClick.AddListener(OnUserRequestResetLoginState);

            status.text = _statusText_waitVerifyCachedData;
            loadingDots.SetActive(true);
        }

        private void SetupWaitVerifyEmailState()
        {
            inputField.gameObject.SetActive(true);
            status.text = _statusText_waitVerifyEmail;
            loadingDots.SetActive(true);
        }

        private void SetupWaitVerifyOtpState()
        {
            inputField.gameObject.SetActive(true);
            status.text = _statusText_waitVerifyOtp;
            loadingDots.SetActive(true);
        }

        private void SetupLoggedInState()
        {
            submitButton.gameObject.SetActive(true);

            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(true);
                submitButton_dynamicLabelText.text = _btnText_logOut;
            }

            status.text = _statusText_loggedIn;

            submitButton.onClick.AddListener(OnUserPressedLogOutButton);
        }

        private void CleanupLoggedInState()
        {
            CleanupContextualUiElements();
            submitButton.onClick.RemoveListener(OnUserPressedLogOutButton);
        }

        private void SetupGetUserEmailState()
        {
            inputField.text = string.Empty;
            inputField.gameObject.SetActive(true);

            inputField.onSubmit.AddListener(OnUserSubmitEmailViaButton);
            inputField.onValueChanged.AddListener(OnUserChangedEmail);

            submitButton.gameObject.SetActive(true);

            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(true);
                submitButton_dynamicLabelText.text = _btnText_submitEmail;
            }

            submitButton.onClick.AddListener(OnUserSubmitEmailViaEnter);

            status.text = _statusText_getUserEmail;
        }

        private void CleanupGetUserEmailState()
        {
            CleanupContextualUiElements();

            inputField.onValueChanged.RemoveListener(OnUserChangedEmail);
            submitButton.onClick.RemoveListener(OnUserSubmitEmailViaEnter);
            inputField.onSubmit.RemoveListener(OnUserSubmitEmailViaButton);
        }

        private void SetupGetUserOtpState()
        {
            backButton.gameObject.SetActive(true);

            status.text = "enter confirmation code";
            inputField.text = string.Empty;
            inputField.gameObject.SetActive(true);

            submitButton.gameObject.SetActive(true);

            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(true);
                submitButton_dynamicLabelText.text = _btnText_confirmOtp;
            }

            submitButton.onClick.AddListener(OnUserSubmitOtpViaField);
            inputField.onSubmit.AddListener(OnUserSubmitOtpViaButton);
            inputField.onValueChanged.AddListener(OnUserChangedOtp);
        }

        private void CleanupGetUserOtpState()
        {
            CleanupContextualUiElements();

            inputField.onValueChanged.RemoveListener(OnUserChangedOtp);
            submitButton.onClick.RemoveListener(OnUserSubmitOtpViaField);
            inputField.onSubmit.RemoveListener(OnUserSubmitOtpViaButton);
        }

        private void SetupNoUserAccountState()
        {
            submitButton.gameObject.SetActive(true);

            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(true);
                submitButton_dynamicLabelText.text = _btnText_createAccount;
            }

            submitButton.onClick.AddListener(OnUserCreateAccountButtonPressed);

            useWithoutAccountButton.gameObject.SetActive(true);
            useWithoutAccountButton.onClick.AddListener(OnUserRequestSkip);

            status.text = _statusText_noUserAccount;
        }

        private void CleanupNoUserAccountState()
        {
            CleanupContextualUiElements();

            useWithoutAccountButton.gameObject.SetActive(false);
            useWithoutAccountButton.onClick.RemoveListener(OnUserRequestSkip);
            submitButton.onClick.RemoveListener(OnUserCreateAccountButtonPressed);
        }

        private void SetupSomethingWrongState()
        {
            submitButton.gameObject.SetActive(true);

            if (submitButton_dynamicLabelText != null)
            {
                submitButton_dynamicLabelText.gameObject.SetActive(true);
                submitButton_dynamicLabelText.text = _btnText_tryAgain;
            }

            submitButton.onClick.AddListener(OnUserTryAgain);
        }

        private void CleanupSomethingWrongState()
        {
            CleanupContextualUiElements();
            submitButton.onClick.RemoveListener(OnUserTryAgain);
        }

        // ==============================================================
        // UI EVENTS
        // ==============================================================

        private void OnUserRequestSkip()
        {
            AbortUserLoginState();
            HideLoginUI();
        }

        private void OnUserRequestResetLoginState()
        {
            LogOutUser();
        }

        private void OnUserRequestBack()
        {
            ChangeToState(LOGIN_STATE.GET_USER_EMAIL);
        }

        private void OnUserCreateAccountButtonPressed()
        {
            Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
            ChangeToState(LOGIN_STATE.GET_USER_EMAIL);
        }

        private void OnUserSubmitEmailViaEnter()
        {
            OnUserSubmitEmailViaButton(inputField.text);
        }

        private async void OnUserSubmitEmailViaButton(string userString)
        {
            var email = (userString ?? string.Empty).Trim();

            if (!IsEmailValid(email))
            {
                status.text = _statusText_invalidUserEmail;
                return;
            }

            Debug.Log("UserLoginController submitting email: " + email);

            ChangeToState(LOGIN_STATE.WAIT_VERIFY_EMAIL);
            _emailRequestStartTime = Time.realtimeSinceStartup;

            await AvatarSdk.StartLoginEmailOtpAsync(email);
            // Events drive next transitions.
        }

        private async void OnUserSubmitOtpViaField()
        {
            await SubmitOtpCodeAsync(inputField.text);
        }

        private async void OnUserSubmitOtpViaButton(string arg)
        {
            await SubmitOtpCodeAsync(arg);
        }

        private async Task SubmitOtpCodeAsync(string code)
        {
            var trimmed = (code ?? string.Empty).Trim();

            if (!Regex.IsMatch(trimmed, @"^\d{6}$"))
            {
                status.text = _statusText_invalidUserOtp;
                return;
            }

            ChangeToState(LOGIN_STATE.WAIT_VERIFY_OTP);
            await AvatarSdk.SubmitEmailOtpCodeAsync(trimmed);
            // Events handle success/failure.
        }

        private void OnUserChangedEmail(string text)
        {
            var email = (text ?? string.Empty).Trim();

            if (!IsEmailValid(email))
            {
                status.text = _statusText_invalidUserEmail;
            }
            else
            {
                status.text = string.Empty;
            }
        }

        private void OnUserChangedOtp(string text)
        {
            if (Regex.IsMatch(text ?? string.Empty, @"^\d{6}$"))
            {
                status.text = string.Empty;
            }
            else
            {
                status.text = _statusText_invalidUserOtp;
            }
        }

        private void OnUserTryAgain()
        {
            ChangeToState(LOGIN_STATE.GET_USER_EMAIL);
        }

        private void OnUserPressedLogOutButton()
        {
            LogOutUser();
        }

        // ==============================================================
        // UI HELPERS
        // ==============================================================

        public void ShowLoginUI()
        {
            StartCoroutine(SpinLoginUiOpen());
        }

        private void HideLoginUI()
        {
            StartCoroutine(SpinLoginUiClose());

            OnLoginUiClosed?.Invoke();
            skipButtonText.text = "close";
        }

        private void AbortUserLoginState()
        {
            ChangeToState(LOGIN_STATE.GET_USER_EMAIL);
            OnLoginStateAborted?.Invoke();
        }

        private IEnumerator SpinLoginUiOpen()
        {
            userLoginUiRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            userLoginUiRoot.SetActive(true);

            var currTime = 0f;
            while (currTime <= _spinOutAnimationTime)
            {
                var spinVal = Mathf.Lerp(180f, 90f, currTime / _spinOutAnimationTime);
                userLoginUiRoot.transform.localRotation = Quaternion.Euler(0f, spinVal, 0f);
                currTime += Time.deltaTime;
                yield return null;
            }

            userLoginUiRoot.transform.localRotation = Quaternion.identity;
        }

        private IEnumerator SpinLoginUiClose()
        {
            var currTime = 0f;
            while (currTime <= _spinOutAnimationTime)
            {
                var spinVal = Mathf.Lerp(0f, 90f, currTime / _spinOutAnimationTime);
                userLoginUiRoot.transform.localRotation = Quaternion.Euler(0f, spinVal, 0f);
                currTime += Time.deltaTime;
                yield return null;
            }

            userLoginUiRoot.transform.localRotation = Quaternion.identity;
            userLoginUiRoot.SetActive(false);
        }

        private IEnumerator HideLoginUIAfterTime(float t)
        {
            yield return new WaitForSeconds(t);
            HideLoginUI();
        }

        // ==============================================================
        // GENERIC UTILS
        // ==============================================================

        private static bool IsEmailValid(string email)
        {
            // Simple heuristic; replace with more robust validation if needed.
            return !string.IsNullOrWhiteSpace(email) && email.Contains("@");
        }
    }
}
