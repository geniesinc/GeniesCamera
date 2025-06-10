using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading.Tasks;
using Genies.Avatars.Services;
using Genies.ServiceManagement;
using Genies.Utilities;
using Genies.Utilities.Internal;
using Genies.Login;
using Genies.Login.Native;
using Genies.Login.Otp;
using Genies.Services.Configs;
using TMPro;
using System.Net.NetworkInformation;
using Genies.NativeAPI;

namespace Genies.Components.Accounts
{
    public enum LOGIN_STATE
    {
        INITIAL_NULL_STATE,
        WAIT_VERIFY_CACHED_DATA,
        GET_USER_PHONE,
        WAIT_VERIFY_PHONE,
        GET_USER_PIN,
        WAIT_VERIFY_PIN,
        SOMETHING_WENT_WRONG,
        LOGGED_IN_STATE,
        NO_USER_ACCOUNT
    };

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
        [SerializeField] private GameObject inputField_plusSign;
        [SerializeField] private GameObject submitButton_getUserPhoneLabel;
        [SerializeField] private GameObject submitButton_dynamicLabel;
        [SerializeField] private Text submitButton_dynamicLabelText;
        [SerializeField] private GameObject loadingDots;
        [SerializeField] private bool _doAutoInitialize;

        private IOtpLoginFlowController _otpLoginFlowController;
        private LOGIN_STATE _currState = LOGIN_STATE.INITIAL_NULL_STATE;
        private Dictionary<LOGIN_STATE, Action> _stateToSetupFunction;
        private Dictionary<LOGIN_STATE, Action> _stateToCleanupFunction;

        private bool _isLoggedIn = false;
        public bool IsLoggedIn { get { return _isLoggedIn; } }

        // TODO: DO WE STILL NEED THIS?
        private bool _didInitializeViaCache = false;
        private bool _didRegisterInputCallbacks = false;
        private bool _didInitialize = false;

        private float _minLoginDisplayTimer = 1.5f;
        private float _minPingDisplayTimer = 1.0f;
        private const float _spinOutAnimationTime = 0.15f;

        private bool _isLoginUiOpen { get { return userLoginUiRoot.activeInHierarchy; } }
        private string _currUserId = "";
        public string UserId { get { return _currUserId; }}

        // Button strings
        const string _btnText_confirmPin = "confirm pin";
        const string _btnText_tryAgain = "try again";
        const string _btnText_logOut = "log out";
        const string _btnText_dismiss = "dismiss";
        const string _btnText_cancel = "cancel";
        const string _btnText_createAccount = "create account";

        // Status strings
        const string _statusText_waitVerifyCachedData = "attempting to log in with stored data...";
        const string _statusText_getUserPhone = "please enter phone number";

        const string _statusText_invalidUserPhone =
            "invalid phone number: expecting 11-13 digit number including country code";

        const string _statusText_waitVerifyPhone = "pinging your Genie...";
        const string _statusText_noUserAccount = "<b>no account found.</b>\nplease sign up through the 'Genies Party' app,\nor contact devrelations@genies.com";
        const string _statusText_waitVerifyPin = "confirming your PIN...";
        const string _statusText_invalidUserPin = "expecting 6 digit code";
        const string _statusText_loggedIn = "great to see you!";

        

        void Start()
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

            // Setup ability to transition to and from states
            InitializeUiStateMachine();

            // Set the initial UI state
            ChangeToState(LOGIN_STATE.WAIT_VERIFY_CACHED_DATA);

            // Try to login with cached data...
            bool isSuccessful = InitializeLoginService();
            if (!isSuccessful)
            {
                Debug.LogError("UserLoginController: Initialization failed.");
                return;
            }

            // If we can login from the cached token, do it!
            bool didInstantLogIn = await GeniesLoginSdk.TryInstantLoginAsync();
            if (didInstantLogIn)
            {
                CompleteLoggedInState();
            }
            else
            {
                ChangeToState(LOGIN_STATE.GET_USER_PHONE);
            }

            _didInitialize = true;
        }

        private void Update()
        {
            // Ensure we display the login UI for at least N seconds
            if (_minLoginDisplayTimer >= 0)
            {
                _minLoginDisplayTimer -= Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            if (_didInitialize)
            {
                skipButton.onClick.RemoveAllListeners();
                backButton.onClick.RemoveListener(OnUserRequestBack);

                networkConnectionChecker.NetworkConnectionStateChanged -= OnNetworkConnectionChanged;   
            }
        }

        private bool InitializeLoginService()
        {
            string loginEndPoint = "";
#if PRODUCTION_BUILD
            BackendEnvironment environment = BackendEnvironment.Prod;
            loginEndPoint = "https://api.genies.com";
#else
            BackendEnvironment environment = BackendEnvironment.Dev;
            loginEndPoint = "https://api.dev.genies.com";
#endif
            
            // 1) Set up login - Must be done first. Calling GeniesApiConfigManager.SetApiConfig will
            // force the usage of the default login package
            GeniesNativeAPIAuth auth = new GeniesNativeAPIAuth();
            auth.InitializeAPI(loginEndPoint, "GeniesOnTheScene");
            auth.RegisterSelf().As<IGeniesLogin>();
            
            // 2) Set up the API configuration
            GeniesApiConfigManager.SetApiConfig(new GeniesApiConfig
            {
                TargetEnv = environment,
            }, overwriteCurrent: true);

            // 3) Set up the master dependency list
            IAvatarService avatarService = new AvatarService();
            avatarService.RegisterSelf().As<IAvatarService>();
            
            return true;
        }

        private async void CompleteLoggedInState()
        {
            #if CREATOR_BUILD
                userId = AppManager.Instance.UserId;
            #else
                _currUserId = await GeniesLoginSdk.GetUserIdAsync();
            #endif
            userIdLabel.text = _currUserId;
            Debug.Log("User ID: " + _currUserId);

            if (_currState == LOGIN_STATE.WAIT_VERIFY_CACHED_DATA)
            {
                _didInitializeViaCache = true;
            }

            _isLoggedIn = true;
            OnLoginSuccessful?.Invoke();

            // Update UI
            // We are no longer a skip button, we are becoming a Close button
            skipButton.onClick.RemoveListener(OnUserRequestSkip);
            skipButton.onClick.AddListener(HideLoginUI);

            if (_minLoginDisplayTimer > 0)
            {
                StartCoroutine(HideLoginUIAfterTime(_minLoginDisplayTimer));
            }
            else
            {
                HideLoginUI();
            }

            // Update State
            ChangeToState(LOGIN_STATE.LOGGED_IN_STATE);
        }

        private async void SubmitPhoneNumber(string phoneNumber)
        {
            // time this process
            float startTime = Time.realtimeSinceStartup;

            ChangeToState(LOGIN_STATE.WAIT_VERIFY_PHONE);

            var result = await _otpLoginFlowController.SubmitPhoneNumberAsync(phoneNumber);
            Debug.Log($"UserLoginController: SubmitPhoneNumberAsync result: {result.statusCode} {result.errorMessage}");

            // Make sure the user has enough time to read
            // the status message before we change it
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            if (elapsedTime < _minPingDisplayTimer)
            {
                await Task.Delay((int)((_minPingDisplayTimer - elapsedTime) * 1000));
            }

            // Update the UI
            if (result.isSuccessful)
            {
                ChangeToState(LOGIN_STATE.GET_USER_PIN);
            }
            else if (result.statusCode == GeniesAuthInitiateOtpSignInResponse.StatusCode.USER_NOT_FOUND ||
                    result.statusCode == GeniesAuthInitiateOtpSignInResponse.StatusCode.USER_NOT_CONFIRMED ||
                    result.statusCode == GeniesAuthInitiateOtpSignInResponse.StatusCode.SIGN_UP_FAILED)
            {
                Debug.Log("UserLoginController: User not found or not confirmed, or sign up failed.");
                ChangeToState(LOGIN_STATE.NO_USER_ACCOUNT);
            }
            else
            {
                string errorMessage = $"[ERROR {result.statusCodeString}] Failed to submit phone number.\n{result.errorMessage}";
                Debug.LogError("UserLoginController: " + errorMessage);
                ChangeToState(LOGIN_STATE.SOMETHING_WENT_WRONG);
                status.text = errorMessage;
            }
        }

        private async void SubmitOtpCode(string code)
        {
            ChangeToState(LOGIN_STATE.WAIT_VERIFY_PIN);
            var result = await _otpLoginFlowController.SubmitOtpCodeAsync(code);
            if (result.isSuccessful)
            {
                CompleteLoggedInState();
            }
            else
            {
                string errorMessage = $"[ERROR {result.statusCodeString}] Failed to accept PIN.\n{result.errorMessage}";
                Debug.LogError("UserLoginController: " + errorMessage);
                ChangeToState(LOGIN_STATE.SOMETHING_WENT_WRONG);
                status.text = errorMessage;
            }
        }

        private void InitializeUiStateMachine()
        {
            // Ui Setup
            skipButton.onClick.AddListener(OnUserRequestSkip);
            backButton.onClick.AddListener(OnUserRequestBack);

            // Simplify what they can enter
            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.lineType = InputField.LineType.SingleLine;

            // Implement a 'no internet connection' state
            networkConnectionChecker.NetworkConnectionStateChanged += OnNetworkConnectionChanged;

            // State machine setup
            _stateToSetupFunction = new Dictionary<LOGIN_STATE, Action>()
            {
                { LOGIN_STATE.INITIAL_NULL_STATE, CleanupContextualUiElements},
                { LOGIN_STATE.WAIT_VERIFY_CACHED_DATA, SetupWaitVerifyCachedDataState },
                { LOGIN_STATE.GET_USER_PHONE, SetupGetUserPhoneState },
                { LOGIN_STATE.WAIT_VERIFY_PHONE, SetupWaitVerifyUserPhoneState },
                { LOGIN_STATE.GET_USER_PIN, SetupGetUserPinState },
                { LOGIN_STATE.WAIT_VERIFY_PIN, SetupWaitVerifyUserPinState },
                { LOGIN_STATE.SOMETHING_WENT_WRONG, SetupSomethingWrongState },
                { LOGIN_STATE.NO_USER_ACCOUNT, SetupNoUserAccountState },
                { LOGIN_STATE.LOGGED_IN_STATE, SetupLoggedInState },
            };

            _stateToCleanupFunction = new Dictionary<LOGIN_STATE, Action>()
            {
                { LOGIN_STATE.INITIAL_NULL_STATE, CleanupContextualUiElements},
                { LOGIN_STATE.WAIT_VERIFY_CACHED_DATA, CleanupWaitVerifyCachedDataState },
                { LOGIN_STATE.GET_USER_PHONE, CleanupGetUserPhoneState },
                { LOGIN_STATE.WAIT_VERIFY_PHONE, CleanupContextualUiElements },
                { LOGIN_STATE.GET_USER_PIN, CleanupGetUserPinState },
                { LOGIN_STATE.WAIT_VERIFY_PIN, CleanupContextualUiElements },
                { LOGIN_STATE.SOMETHING_WENT_WRONG, CleanupSomethingWrongState },
                { LOGIN_STATE.NO_USER_ACCOUNT, CleanupNoUserAccountState },
                { LOGIN_STATE.LOGGED_IN_STATE, CleanupLoggedInState }
            };
        }

        private void OnNetworkConnectionChanged(NetworkConnectionState newState)
        {
            // If you're on the login screen, and no internet connection is detected,
            // send the user into offline mode.
            if (_isLoginUiOpen && newState == NetworkConnectionState.NotConnected)
            {
                AbortUserLoginState();
                HideLoginUI();
            }
        }

        private async void LogOutUser()
        {
            var logoutResult = await GeniesLoginSdk.LogOutAsync();
            if (!logoutResult.isSuccessful)
            {
                Debug.LogError($"UserLoginController: Logout failed: {logoutResult.errorMessage} Status code {logoutResult.statusCodeString}");
                return;
            }

            // If the listeners are not there...
            if (_didInitializeViaCache && !_didRegisterInputCallbacks)
            {
                // See comments in Awake(). From here, we will just restart the app.
                Debug.LogError("Warning: User has entered an unsupported UI state, so we will " +
                               "give them the option to restart the game.");

                // Restart game confirmation screen
                ChangeToState(LOGIN_STATE.GET_USER_PHONE);
            }

            else
            {
                ChangeToState(LOGIN_STATE.GET_USER_PHONE);
            }

            _isLoggedIn = false;
            userIdLabel.text = string.Empty;

            OnLogOutSuccessful?.Invoke();
        }

        private void ChangeToState(LOGIN_STATE nextState)
        {
            // Call the clean-up process affiliated with the last state.
            _stateToCleanupFunction[_currState]();
            // Call the set-up process affiliated with the new state.
            _currState = nextState;
            _stateToSetupFunction[_currState]();
        }

        // ==============================================================
        // STATE MACHINE SETUP AND CLEANUP
        // ==============================================================
        //
        // This function reverts the UI to its default state
        private void CleanupContextualUiElements()
        {
            // Turn off all input field related stuff
            inputField.gameObject.SetActive(false);
            inputField_plusSign.SetActive(false);

            // Turn off all submit button related stuff
            submitButton.gameObject.SetActive(false);
            submitButton_dynamicLabel.SetActive(false);
            submitButton_getUserPhoneLabel.SetActive(false);
            submitButton_dynamicLabel.SetActive(false);

            // Turn off 'waiting for user pin' stuff
            backButton.gameObject.SetActive(false);

            // Hide mobile keyboard
            inputField.DeactivateInputField();

            // Turn off loading dots
            loadingDots.SetActive(false);
        }

        private void CleanupWaitVerifyCachedDataState()
        {
            CleanupContextualUiElements();

            submitButton.onClick.RemoveListener(OnUserRequestResetLoginState);
        }

        private void SetupWaitVerifyCachedDataState()
        {
            // TODO: This is our workaround for this state hanging if
            // there is bad cached data here. In general, it would be better
            // to just catch the exception and react (xr-372)
            submitButton.gameObject.SetActive(true);
            submitButton_dynamicLabel.SetActive(true);
            submitButton_dynamicLabelText.text = _btnText_cancel;
            submitButton.onClick.AddListener(OnUserRequestResetLoginState);

            status.text = _statusText_waitVerifyCachedData;
            loadingDots.SetActive(true);
        }

        private void SetupWaitVerifyUserPhoneState()
        {
            // let them recall the data they submit:
            inputField.gameObject.SetActive(true);
            inputField_plusSign.SetActive(true);

            status.text = _statusText_waitVerifyPhone;
            loadingDots.SetActive(true);
        }

        private void SetupWaitVerifyUserPinState()
        {
            // let them recall the data they submit:
            inputField.gameObject.SetActive(true);

            status.text = _statusText_waitVerifyPin;
            loadingDots.SetActive(true);
        }

        private void SetupLoggedInState()
        {
            submitButton.gameObject.SetActive(true);
            submitButton_dynamicLabelText.text = _btnText_logOut;
            submitButton_dynamicLabel.SetActive(true);
            status.text = _statusText_loggedIn;

            submitButton.onClick.AddListener(OnUserPressedLogOutButton);
        }

        private void CleanupLoggedInState()
        {
            CleanupContextualUiElements();

            submitButton.onClick.RemoveListener(OnUserPressedLogOutButton);
        }

        private void SetupGetUserPhoneState()
        {
            // Input field
            inputField.text = string.Empty;
            inputField.gameObject.SetActive(true);
            inputField_plusSign.SetActive(true);
            inputField.onSubmit.AddListener(OnUserSubmitPhoneNumberViaButton);
            inputField.onValueChanged.AddListener(OnUserChangedPhoneNumber);

            // Submit button
            submitButton.gameObject.SetActive(true);
            submitButton_getUserPhoneLabel.SetActive(true);
            submitButton.onClick.AddListener(OnUserSubmitPhoneNumberViaEnter);

            // Status message
            status.text = _statusText_getUserPhone;
        }

        private void CleanupGetUserPhoneState()
        {
            // Turn off all UI visuals
            CleanupContextualUiElements();

            // Turn off state-specific listeners
            inputField.onValueChanged.RemoveListener(OnUserChangedPhoneNumber);
            submitButton.onClick.RemoveListener(OnUserSubmitPhoneNumberViaEnter);
            inputField.onSubmit.RemoveListener(OnUserSubmitPhoneNumberViaButton);
        }

        private void SetupGetUserPinState()
        {
            // let them realize they entered the wrong phone number
            backButton.gameObject.SetActive(true);

            status.text = "enter confirmation code";
            inputField.text = string.Empty;
            //inputFieldPlaceholder.text = "123456";
            inputField.gameObject.SetActive(true);

            submitButton.gameObject.SetActive(true);
            submitButton_dynamicLabel.SetActive(true);
            submitButton_dynamicLabelText.text = _btnText_confirmPin;

            submitButton.onClick.AddListener(OnUserSubmitPinViaField);
            inputField.onSubmit.AddListener(OnUserSubmitPinViaButton);
            inputField.onValueChanged.AddListener(OnUserChangedPin);
        }

        private void CleanupGetUserPinState()
        {
            // Turn off all UI visuals
            CleanupContextualUiElements();

            // Turn off state-specific listeners
            inputField.onValueChanged.RemoveListener(OnUserChangedPin);
            submitButton.onClick.RemoveListener(OnUserSubmitPinViaField);
            inputField.onSubmit.RemoveListener(OnUserSubmitPinViaButton);
        }

        private void SetupNoUserAccountState()
        {
            // Create account button
            submitButton.gameObject.SetActive(true);
            submitButton_dynamicLabel.SetActive(true);
            submitButton_dynamicLabelText.text = _btnText_createAccount;
            submitButton.onClick.AddListener(OnUserCreateAccountButtonPressed);

            // Use without account button
            useWithoutAccountButton.gameObject.SetActive(true);
            useWithoutAccountButton.onClick.AddListener(OnUserRequestSkip);

            status.text = _statusText_noUserAccount;
        }

        private void CleanupNoUserAccountState()
        {
            // Turn off all UI visuals
            CleanupContextualUiElements();
            // Turn off state-specific items
            useWithoutAccountButton.gameObject.SetActive(false);
            // State specific listeners
            useWithoutAccountButton.onClick.RemoveListener(OnUserRequestSkip);
            submitButton.onClick.RemoveListener(OnUserCreateAccountButtonPressed);
        }

        private void SetupSomethingWrongState()
        {
            submitButton.gameObject.SetActive(true);
            submitButton_dynamicLabelText.text = _btnText_tryAgain;
            submitButton_dynamicLabel.SetActive(true);

            submitButton.onClick.AddListener(OnUserTryAgain);
        }

        private void CleanupSomethingWrongState()
        {
            // Turn off all UI visuals
            CleanupContextualUiElements();

            // Turn off state-specific listeners
            submitButton.onClick.RemoveListener(OnUserTryAgain);
        }

        // ==============================================================
        // IN RESPONSE TO UI BUTTON PRESS & TYPING
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

        // The back button is only shown during the PIN confirmation state,
        // so the only place to go is back to the initial phone number state.
        private void OnUserRequestBack()
        {
            ChangeToState(LOGIN_STATE.GET_USER_PHONE);
        }

        private void OnUserCreateAccountButtonPressed()
        {
            Debug.Log("Launch Genies Party app to create account");
        }

        private void OnUserSubmitPhoneNumberViaEnter()
        {
            OnUserSubmitPhoneNumberViaButton(inputField.text);
        }

        private void OnUserSubmitPhoneNumberViaButton(string userString)
        {
            // clean up their string by stripping any nonsense and attempting
            // to fixg common mistakes such as missing country code
            string cleanString = GetCleanPhoneNumber(userString);

            // show the user what we will submit (note: plus sign is already perma-displayed)
            inputField.text = cleanString.Replace("+", "");

            // try to input
            if (!PhoneNumberValidator.IsPhoneNumberValid(cleanString))
            {
                // If they typo, we still stay on this screen, so no need to cleanup anything:
                status.text = _statusText_invalidUserPhone;
            }
            else
            {
                Debug.Log("UserLoginController submitting number: " + cleanString);

                // Setup entire login system

                _otpLoginFlowController?.Dispose();
                _otpLoginFlowController = GeniesLoginSdk.StartOtpLogin();
                SubmitPhoneNumber(cleanString);
            }
        }        

        private void OnUserSubmitPinViaField()
        {
            SubmitOtpCode(inputField.text);
        }

        private void OnUserSubmitPinViaButton(string arg)
        {
            // change to wait
            SubmitOtpCode(arg);
        }

        private void OnUserChangedPhoneNumber(string text)
        {
            if (!PhoneNumberValidator.IsPhoneNumberValid(GetCleanPhoneNumber(text)))
            {
                status.text = _statusText_invalidUserPhone;
            }
            else
            {
                status.text = string.Empty;
            }
        }

        private void OnUserChangedPin(string text)
        {
            if (Regex.IsMatch(text, @"^\d{6}$"))
            {
                status.text = string.Empty;
            }
            else
            {
                status.text = _statusText_invalidUserPin;
            }
        }

        private void OnUserTryAgain()
        {
            // Reset the new state to Getting the user's phone number. Map to base class's
            // more limited vocabulary to achieve this:
            ChangeToState(LOGIN_STATE.GET_USER_PHONE);
        }

        private void OnUserPressedLogOutButton()
        {
            LogOutUser();
        }

        // ==============================================================
        /// UI HELPER FUNCTIONS
        // ==============================================================
        public void ShowLoginUI()
        {
            StartCoroutine(SpinLoginUiOpen());
        }

        private void HideLoginUI()
        {
            StartCoroutine(SpinLoginUiClose());

            OnLoginUiClosed?.Invoke();

            // We only want it to say "skip" the first time.
            // From there, it should say "close".
            skipButtonText.text = "close";
        }

        private void AbortUserLoginState()
        {
            ChangeToState(LOGIN_STATE.GET_USER_PHONE);

            OnLoginStateAborted?.Invoke();
        }

        private IEnumerator SpinLoginUiOpen()
        {
            userLoginUiRoot.transform.localRotation = Quaternion.Euler(0, 90, 0);
            userLoginUiRoot.SetActive(true);

            float currTime = 0f;
            while (currTime <= _spinOutAnimationTime)
            {
                float spinVal = Mathf.Lerp(180, 90, currTime / _spinOutAnimationTime);
                userLoginUiRoot.transform.localRotation = Quaternion.Euler(0, spinVal, 0);
                currTime += Time.deltaTime;
                yield return null;
            }

            userLoginUiRoot.transform.localRotation = Quaternion.identity;
        }

        private IEnumerator SpinLoginUiClose()
        {
            float currTime = 0f;
            while (currTime <= _spinOutAnimationTime)
            {
                float spinVal = Mathf.Lerp(0, 90, currTime / _spinOutAnimationTime);
                userLoginUiRoot.transform.localRotation = Quaternion.Euler(0, spinVal, 0);
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
        private string GetCleanPhoneNumber(string userString)
        {
            // they put random stuff in? shouldn't be possible due to our
            // keyboard type but whatever.
            string cleanString = Regex.Replace(userString, @"[^\d]", "");

            // they forgot a country code and are probably american?
            if (cleanString.Length == 10)
            {
                cleanString = $"1{cleanString}";
            }

            // we stripped the plus sign out in the first sanity-check,
            // and also it shouldn't be possible due to our keyboard type,
            // but just in case:
            if (!cleanString.StartsWith("+"))
            {
                cleanString = $"+{cleanString}";
            }

            return cleanString;
        }
    }
}