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
    internal class LoginEmailOtp : IDisposable
    {
        private GameObject AttachedGameObject { get; set; }
        private string EmailPrefill { get; set; }
        private string EmailTarget { get; set; }

        public LoginEmailOtp(GameObject attachedGameObject, string emailPrefill = null)
        {
            if (attachedGameObject == null)
            {
                throw new ArgumentNullException(nameof(attachedGameObject), "Must provide a valid game object.");
            }

            AttachedGameObject = attachedGameObject;
            EmailPrefill = emailPrefill;

            AvatarSdk.Events.UserLoggedIn += RestartLoginFlow;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded += OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed += OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded += OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed += OnEmailCodeSubmissionFailed;

            RestartLoginFlow();
        }

        public void Dispose()
        {
            AvatarSdk.Events.UserLoggedIn -= RestartLoginFlow;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded -= OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed -= OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded -= OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed -= OnEmailCodeSubmissionFailed;

            DestroyIEmailComponents();
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

        private void DestroyIEmailComponents()
        {
            if (AttachedGameObject == null) { return; }

            foreach (var component in AttachedGameObject.GetComponents<ILoginEmailComponent>())
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

        private void OnEmailCodeRequestSucceeded(string email)
        {
            EmailTarget = email;
            StartEmailCodeSubmission();
        }

        private void OnEmailCodeRequestFailed((string email, string failReason) failResponse)
        {
            Debug.LogError($"Email code request failed: {failResponse.failReason}");

            if (AvatarSdk.IsAwaitingEmailOtpCode)
            {
                // Resend failed while waiting for email code, stay on code entry screen
                StartEmailCodeSubmission();
            }
            else
            {
                // Initial login failed, restart the flow
                RestartLoginFlow();
            }
        }

        private void OnEmailCodeSubmissionSucceeded(string code)
        {
            RemoveProcessing();
            // Logged-in event handles next steps.
        }

        private void OnEmailCodeSubmissionFailed((string code, string failReason) failResponse)
        {
            RemoveProcessing();
            Debug.LogError($"Email code submission failed: {failResponse.failReason}");
            StartEmailCodeSubmission();
        }

        private void SetLoggedIn()
        {
            if (AttachedGameObject == null) { return; }

            DestroyIEmailComponents();

            var iEmailComponent = AttachedGameObject.AddComponent<LoggedIn>();
            iEmailComponent.LogOutRequest += async () =>
            {
                DestroyIEmailComponents();

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

            DestroyIEmailComponents();

            var enterEmail = AttachedGameObject.AddComponent<EnterEmail>();
            enterEmail.Email = EmailPrefill;
            enterEmail.Submit += async email =>
            {
                DestroyIEmailComponents();

                SetProcessing($"Sending verification code to {email}...");

                await AvatarSdk.StartLoginEmailOtpAsync(email);
            };
        }

        private void StartEmailCodeSubmission()
        {
            if (AttachedGameObject == null) { return; }

            DestroyIEmailComponents();

            var enterEmailCode = AttachedGameObject.AddComponent<EnterLoginEmailCode>();
            enterEmailCode.EmailTarget = EmailTarget;
            enterEmailCode.Submit += async emailCode =>
            {
                DestroyIEmailComponents();

                SetProcessing("Submitting email code...");

                await AvatarSdk.SubmitEmailOtpCodeAsync(emailCode);
            };

            enterEmailCode.ResendRequest += async () =>
            {
                DestroyIEmailComponents();

                SetProcessing("Resending email code...");

                await AvatarSdk.ResendEmailCodeAsync();

                // Event handlers will take care of next steps
            };
        }

        // ==================================================
        // ILoginEmailComponent
        // ==================================================

        private interface ILoginEmailComponent { }

        private class ProcessingState : MonoBehaviour, ILoginEmailComponent
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

        private class LoggedIn : MonoBehaviour, ILoginEmailComponent
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

        private class EnterEmail : MonoBehaviour, ILoginEmailComponent
        {
            public event Action<string> Submit;

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

            private bool Invoked { get; set; }

            [Header("Enter your email address")]
            [SerializeField] private string _email;

            [InspectorButton("\nSubmit Email\n", ExecutionMode.PlayMode)]
            private void SubmitEmail()
            {
                if (Invoked)
                {
                    return;
                }
                Invoked = true;

                Submit?.Invoke(Email);
            }

            [InspectorButton("New user? Click here to sign up", ExecutionMode.PlayMode)]
            private void SignUp()
            {
                Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
            }
        }

        private class EnterLoginEmailCode : MonoBehaviour, ILoginEmailComponent
        {
            public event Action<string> Submit;
            public event Action ResendRequest;

            public string EmailTarget { get; set; }

            public string EmailCode
            {
                get => _emailCode;
                set => _emailCode = value;
            }

            private bool Invoked { get; set; }

            [Header("Check email for OTP")]
            [SerializeField] private string _codeSentTo;

            [Header("Enter OTP here")]
            [SerializeField] private string _emailCode;

            private IEnumerator Start()
            {
                var waitForSeconds = new WaitForSeconds(0.5f);

                while (true)
                {
                    _codeSentTo = EmailTarget;
                    yield return waitForSeconds;
                }
            }

            [InspectorButton("\nSubmit Email Code\n", ExecutionMode.PlayMode)]
            private void SubmitEmailCode()
            {
                // Prevent multiple button presses
                if (Invoked)
                {
                    return;
                }
                Invoked = true;

                Submit?.Invoke(EmailCode);
            }

            [InspectorButton("Resend Email Code", ExecutionMode.PlayMode)]
            private void ResendEmailCode()
            {
                // Prevent multiple button presses
                if (Invoked)
                {
                    return;
                }
                Invoked = true;

                ResendRequest?.Invoke();
            }
        }
    }
}

