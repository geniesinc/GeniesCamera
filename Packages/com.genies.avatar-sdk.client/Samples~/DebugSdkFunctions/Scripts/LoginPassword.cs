using System;
using System.Collections;
using Genies.Utilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static Genies.Utilities.InspectorButtonAttribute;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    internal class LoginPassword : IDisposable
    {
        private GameObject AttachedGameObject { get; set; }
        private string EmailPrefill { get; set; }
        private string EmailTarget { get; set; }

        public LoginPassword(GameObject attachedGameObject, string emailPrefill = null)
        {
            if (attachedGameObject == null)
            {
                throw new ArgumentNullException(nameof(attachedGameObject), "Must provide a valid game object.");
            }

            AttachedGameObject = attachedGameObject;
            EmailPrefill = emailPrefill;

            AvatarSdk.Events.UserLoggedIn += RestartLoginFlow;

            AvatarSdk.Events.LoginPasswordSignInPendingVerification += OnSignInPendingVerification;
            AvatarSdk.Events.LoginPasswordSignInComplete += OnSignInComplete;
            AvatarSdk.Events.LoginPasswordSignInFailed += OnSignInFailed;

            AvatarSdk.Events.LoginPasswordVerificationCodeSucceeded += OnVerificationCodeSucceeded;
            AvatarSdk.Events.LoginPasswordVerificationCodeFailed += OnVerificationCodeFailed;

            RestartLoginFlow();
        }

        public void Dispose()
        {
            AvatarSdk.Events.UserLoggedIn -= RestartLoginFlow;

            AvatarSdk.Events.LoginPasswordSignInPendingVerification -= OnSignInPendingVerification;
            AvatarSdk.Events.LoginPasswordSignInComplete -= OnSignInComplete;
            AvatarSdk.Events.LoginPasswordSignInFailed -= OnSignInFailed;

            AvatarSdk.Events.LoginPasswordVerificationCodeSucceeded -= OnVerificationCodeSucceeded;
            AvatarSdk.Events.LoginPasswordVerificationCodeFailed -= OnVerificationCodeFailed;

            DestroyIPasswordComponents();
        }

        private void RestartLoginFlow()
        {
            if (AttachedGameObject == null) { return; }

            EmailTarget = "";

            if (AvatarSdk.IsLoggedIn)
            {
                SetLoggedIn();
            }
            else
            {
                SetLoggedOut();
            }
        }

        private void DestroyIPasswordComponents()
        {
            if (AttachedGameObject == null) { return; }

            foreach (var component in AttachedGameObject.GetComponents<ILoginPasswordComponent>())
            {
                if (component is MonoBehaviour destroyable)
                {
                    GameObject.Destroy(destroyable);
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(AttachedGameObject);
#endif
        }

        private void OnSignInPendingVerification(string email)
        {
            EmailTarget = email;
            StartVerificationCodeSubmission();
        }

        private void OnSignInComplete(string email)
        {
            RemoveProcessing();
            // Logged-in event handles next steps.
        }

        private void OnSignInFailed((string email, string failReason) failResponse)
        {
            RemoveProcessing();
            Debug.LogError($"Password sign-in failed: {failResponse.failReason}");
            RestartLoginFlow();
        }

        private void OnVerificationCodeSucceeded(string code)
        {
            RemoveProcessing();
            // Logged-in event handles next steps.
        }

        private void OnVerificationCodeFailed((string code, string failReason) failResponse)
        {
            RemoveProcessing();
            Debug.LogError($"Verification code submission failed: {failResponse.failReason}");
            StartVerificationCodeSubmission();
        }

        private void SetLoggedIn()
        {
            if (AttachedGameObject == null) { return; }

            DestroyIPasswordComponents();

            var iPasswordComponent = AttachedGameObject.AddComponent<LoggedIn>();
            iPasswordComponent.LogOutRequest += async () =>
            {
                DestroyIPasswordComponents();

                SetProcessing("Logging out...");

                await AvatarSdk.LogOutAsync();

                RemoveProcessing();

                // Logged-out event handles next steps.
            };
        }

        private void SetProcessing(string state)
        {
            if (AttachedGameObject == null) { return; }

            if (AttachedGameObject.TryGetComponent<ProcessingState>(out var processingState))
            {
                processingState.CurrentState = state;
                return;
            }

            processingState = AttachedGameObject.AddComponent<ProcessingState>();
            processingState.CurrentState = state;
        }

        private void RemoveProcessing()
        {
            if (AttachedGameObject == null) { return; }

            if (AttachedGameObject.TryGetComponent<ProcessingState>(out var processingState))
            {
                GameObject.Destroy(processingState);
            }
        }

        private void SetLoggedOut()
        {
            if (AttachedGameObject == null) { return; }

            DestroyIPasswordComponents();

            var enterEmailAndPassword = AttachedGameObject.AddComponent<EnterEmailAndPassword>();
            enterEmailAndPassword.Email = EmailPrefill;
            enterEmailAndPassword.Submit += async (email, password) =>
            {
                DestroyIPasswordComponents();

                SetProcessing($"Signing in with {email}...");

                await AvatarSdk.StartLoginPasswordAsync(email, password);
            };
        }

        private void StartVerificationCodeSubmission()
        {
            if (AttachedGameObject == null) { return; }

            DestroyIPasswordComponents();

            var enterVerificationCode = AttachedGameObject.AddComponent<EnterVerificationCode>();
            enterVerificationCode.EmailTarget = EmailTarget;
            enterVerificationCode.Submit += async verificationCode =>
            {
                DestroyIPasswordComponents();

                SetProcessing("Submitting verification code...");

                await AvatarSdk.SubmitPasswordVerificationCodeAsync(verificationCode);
            };
        }

        // ==================================================
        // ILoginPasswordComponent
        // ==================================================

        private interface ILoginPasswordComponent { }

        private class ProcessingState : MonoBehaviour, ILoginPasswordComponent
        {
            [SerializeField] private string _currentState;

            private IEnumerator Start()
            {
                var waitForSeconds = new WaitForSeconds(0.5f);
                while (true)
                {
                    _currentState = CurrentState;
                    yield return waitForSeconds;
                }
            }

            public string CurrentState
            {
                get => _currentStateInternal;
                set => _currentStateInternal = value;
            }

            private string _currentStateInternal;
        }

        private class LoggedIn : MonoBehaviour, ILoginPasswordComponent
        {
            [Header("Welcome!")]
            [SerializeField] private string _username;

            public event Action LogOutRequest;

            private IEnumerator Start()
            {
                var waitForSeconds = new WaitForSeconds(0.5f);

                var username = "";
                while (string.IsNullOrWhiteSpace(username))
                {
                    var getUsernameTask = AvatarSdk.GetUserNameAsync();
                    yield return new WaitUntil(() => getUsernameTask.GetAwaiter().IsCompleted);
                    username = getUsernameTask.GetAwaiter().GetResult();
                }

                while (true)
                {
                    _username = username;
                    yield return waitForSeconds;
                }
            }

            [InspectorButton("Log Out", ExecutionMode.PlayMode)]
            private void LogOut()
            {
                LogOutRequest?.Invoke();
            }
        }

        private class EnterEmailAndPassword : MonoBehaviour, ILoginPasswordComponent
        {
            public event Action<string, string> Submit;

            public string Email
            {
                get => _email;
                set
                {
                    _email = value;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(gameObject);
#endif
                }
            }

            private string Password
            {
                get => _password;
            }

            private bool Invoked { get; set; }

            [Header("Enter your email address")]
            [SerializeField] private string _email;

            [Header("Enter your password")]
            [SerializeField] private string _password;

            [InspectorButton("\nSubmit Credentials\n", ExecutionMode.PlayMode)]
            private void SubmitCredentials()
            {
                if (Invoked)
                {
                    return;
                }
                Invoked = true;

                Submit?.Invoke(Email, Password);
            }

            [InspectorButton("New user? Click here to sign up", ExecutionMode.PlayMode)]
            private void SignUp()
            {
                Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
            }
        }

        private class EnterVerificationCode : MonoBehaviour, ILoginPasswordComponent
        {
            public event Action<string> Submit;

            public string EmailTarget { get; set; }

            public string VerificationCode
            {
                get => _verificationCode;
                set => _verificationCode = value;
            }

            private bool Invoked { get; set; }

            [Header("Check email for verification code")]
            [SerializeField] private string _codeSentTo;

            [Header("Enter verification code here")]
            [SerializeField] private string _verificationCode;

            private IEnumerator Start()
            {
                var waitForSeconds = new WaitForSeconds(0.5f);

                while (true)
                {
                    _codeSentTo = EmailTarget;
                    yield return waitForSeconds;
                }
            }

            [InspectorButton("\nSubmit Verification Code\n", ExecutionMode.PlayMode)]
            private void SubmitVerificationCode()
            {
                // Prevent multiple button presses
                if (Invoked)
                {
                    return;
                }
                Invoked = true;

                Submit?.Invoke(VerificationCode);
            }
        }
    }
}

