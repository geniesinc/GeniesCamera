using System;
using System.Collections.Generic;
using Genies.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    /// <summary>
    /// Add this component to a game object in the scene to test runtime functionality of <see cref="AvatarSdk"/>.
    /// Interfaces through the attached game object's Inspector.
    /// Intended for Editor-use only.
    /// </summary>
    internal class SdkFunctionsDebugger : MonoBehaviour
    {
        public enum LoginType
        {
            EmailOtp,
            Password
        }

        [Header("Login Options")]
        [Tooltip("Automatically login with cached credentials on start?")]
        [SerializeField] private bool _instantLoginOnStart = true;
        [Tooltip("The login type to use for authentication.")]
        [SerializeField] private LoginType _loginType = LoginType.EmailOtp;
        [Space]
        [Tooltip("The email address to prefill with the login flow.")]
        [SerializeField] private string _emailPrefill;

        [Header("Spawn Avatar Options")]
        [Tooltip("Optional: leave NULL if avatar should not be parented on spawn.")]
        [SerializeField] private Transform _avatarParent;
        [Tooltip("Optional: leave NULL if no custom animation controller should be applied.")]
        [SerializeField] private RuntimeAnimatorController _customAnimatorController;
        [Tooltip("Name for the default avatar.")]
        [SerializeField] private string _defaultAvatarName = "Default Genies Avatar";
        [Tooltip("Name for the user avatar.")]
        [SerializeField] private string _userAvatarName = "User Genies Avatar";

        [Header("Avatar Editor Options")]
        [Tooltip("The camera to use for the Avatar Editor. Auto-assigned to Camera.main if null.")]
        [SerializeField] private Camera _avatarEditorCamera;
        [Tooltip("The avatar to use for editing. Auto-assigned to the most recently spawned avatar.")]
        [SerializeField] private ManagedAvatarComponent _avatarToEdit;

        [Header("Avatar Modification Options")]
        [Tooltip("Asset ID to grant to the user (leave empty to use first available asset).")]
        [SerializeField] private string _assetIdToGrant = "";
        [Tooltip("Gender type to apply to the avatar.")]
        [SerializeField] private GenderType _genderType = GenderType.Female;
        [Tooltip("Body size to apply to the avatar.")]
        [SerializeField] private BodySize _bodySize = BodySize.Medium;
        [Tooltip("Skin color to apply to the avatar.")]
        [SerializeField] private Color _skinColor = Color.white;

        private LoginType CurrentLoginType
        {
            get => _loginType;
            set => _loginType = value;
        }

        private bool InstantLoginOnStart => _instantLoginOnStart;
        private string EmailPrefill => _emailPrefill;
        private Transform AvatarParent => _avatarParent;
        private RuntimeAnimatorController CustomAnimatorController => _customAnimatorController;
        private string DefaultAvatarName => _defaultAvatarName;
        private string UserAvatarName => _userAvatarName;
        private Camera AvatarEditorCamera => _avatarEditorCamera;

        private ManagedAvatarComponent AvatarToEdit
        {
            get => _avatarToEdit;
            set => _avatarToEdit = value;
        }

        private LoginEmailOtp LoginEmailOtpInstance { get; set; }
        private LoginPassword LoginPasswordInstance { get; set; }
        private SpawnAvatar SpawnAvatarStateDisplay { get; set; }
        private AvatarEditor AvatarEditorStateDisplay { get; set; }
        private List<ManagedAvatar> SpawnedAvatars { get; set; } = new List<ManagedAvatar>();

        private void OnEnable()
        {
            if (_avatarEditorCamera == null)
            {
                _avatarEditorCamera = Camera.main;
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;

            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;
            AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;

            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;

            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
            AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;
#endif
        }

        private async void Start()
        {
#if UNITY_EDITOR
            await AvatarSdk.InitializeAsync();

            SpawnAvatarStateDisplay = new SpawnAvatar(gameObject);
            AvatarEditorStateDisplay = new AvatarEditor(gameObject);

            if (InstantLoginOnStart)
            {
                var instantLoginResult = await AvatarSdk.TryInstantLoginAsync();
                if (instantLoginResult.isLoggedIn)
                {
                    Debug.Log($"Automatically logged in as '{instantLoginResult.username}'");
                    // Let the AvatarSdk.Events.UserLoggedIn event response handle logged in state.
                    return;
                }
            }

            StartLoginFlow();
#endif
        }

        private void OnUserLoggedIn()
        {
            if (LoginEmailOtpInstance == null && LoginPasswordInstance == null)
            {
                StartLoginFlow();
            }
        }

        private void OnUserLoggedOut()
        {
            StartLoginFlow();
        }

        private void OnAvatarEditorOpened()
        {
            Debug.Log("Avatar Editor opened");
        }

        private void OnAvatarEditorClosed()
        {
            Debug.Log("Avatar Editor closed");
        }

        private void StartLoginFlow()
        {
            switch (CurrentLoginType)
            {
                case LoginType.EmailOtp:
                    RestartEmailLogin();
                    break;
                case LoginType.Password:
                    RestartPasswordLogin();
                    break;
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;
            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
#endif

            LoginEmailOtpInstance?.Dispose();
            LoginEmailOtpInstance = null;

            LoginPasswordInstance?.Dispose();
            LoginPasswordInstance = null;

            SpawnAvatarStateDisplay?.Dispose();
            SpawnAvatarStateDisplay = null;

            AvatarEditorStateDisplay?.Dispose();
            AvatarEditorStateDisplay = null;

            DestroyAllSpawnedAvatars();
        }

        [InspectorButton("===== Account Management =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAccountManagement() { }

        [InspectorButton("(Re)Start Email OTP Login", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void RestartEmailLogin()
        {
            LoginEmailOtpInstance?.Dispose();
            LoginEmailOtpInstance = null;

            LoginPasswordInstance?.Dispose();
            LoginPasswordInstance = null;

            CurrentLoginType = LoginType.EmailOtp;
            LoginEmailOtpInstance = new LoginEmailOtp(gameObject, EmailPrefill);
        }

        [InspectorButton("(Re)Start Password Login", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void RestartPasswordLogin()
        {
            LoginEmailOtpInstance?.Dispose();
            LoginEmailOtpInstance = null;

            LoginPasswordInstance?.Dispose();
            LoginPasswordInstance = null;

            CurrentLoginType = LoginType.Password;
            LoginPasswordInstance = new LoginPassword(gameObject, EmailPrefill);
        }

        [InspectorButton("===== Login States =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderLoginState() { }

        [InspectorButton("\nCheck Login Status\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void CheckLoginStatus()
        {
            var isLoggedIn = AvatarSdk.IsLoggedIn;
            var userId = await AvatarSdk.GetUserIdAsync();
            var username = await AvatarSdk.GetUserNameAsync();

            var message = $"Is Logged In: {(isLoggedIn ? "TRUE" : "FALSE")}\n" +
                          $"User ID: {userId}\n" +
                          $"Username: {username}";

            ShowPopUp("Login Status", message);
        }

        [InspectorButton("===== Avatar Spawning =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarSpawning() { }

        [InspectorButton("Spawn Default Avatar", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SpawnDefaultAvatar()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Spawn Default Avatar", "Log in first!");
                return;
            }

            var managedAvatar = await AvatarSdk.LoadDefaultAvatarAsync(
                DefaultAvatarName,
                AvatarParent,
                CustomAnimatorController);

            if (managedAvatar is not null)
            {
                SpawnedAvatars.Add(managedAvatar);
                AvatarToEdit = managedAvatar.Component;
                SpawnAvatarStateDisplay?.UpdateStateDisplay(
                    SpawnedAvatars.ConvertAll(a => a.Component),
                    AvatarToEdit);
                AvatarEditorStateDisplay?.UpdateStateDisplay(AvatarToEdit, AvatarEditorCamera);
            }
        }

        [InspectorButton("\nSpawn User Avatar\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SpawnUserAvatar()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Spawn User Avatar", "Log in first!");
                return;
            }

            var managedAvatar = await AvatarSdk.LoadUserAvatarAsync(
                UserAvatarName,
                AvatarParent,
                CustomAnimatorController);

            if (managedAvatar is not null)
            {
                SpawnedAvatars.Add(managedAvatar);
                AvatarToEdit = managedAvatar.Component;
                SpawnAvatarStateDisplay?.UpdateStateDisplay(
                    SpawnedAvatars.ConvertAll(a => a.Component),
                    AvatarToEdit);
                AvatarEditorStateDisplay?.UpdateStateDisplay(AvatarToEdit, AvatarEditorCamera);
            }
        }

        [InspectorButton("===== Avatar Destroying =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarDestroying() { }

        [InspectorButton("\nDestroy Last Spawned Avatar\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void DestroyLastSpawnedAvatar()
        {
            if (SpawnedAvatars.Count == 0)
            {
                ShowPopUp("⚠️ Destroy Last Spawned Avatar", "No avatars to destroy!");
                return;
            }

            var lastAvatar = SpawnedAvatars[SpawnedAvatars.Count - 1];
            SpawnedAvatars.RemoveAt(SpawnedAvatars.Count - 1);

            lastAvatar?.Dispose();

            // Update AvatarToEdit to the new last avatar (stack behavior)
            AvatarToEdit = SpawnedAvatars.Count > 0 ? SpawnedAvatars[SpawnedAvatars.Count - 1].Component : null;

            SpawnAvatarStateDisplay?.UpdateStateDisplay(
                SpawnedAvatars.ConvertAll(a => a.Component),
                AvatarToEdit);
            AvatarEditorStateDisplay?.UpdateStateDisplay(AvatarToEdit, AvatarEditorCamera);
        }

        [InspectorButton("Destroy All Spawned Avatars", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void DestroyAllSpawnedAvatars()
        {
            foreach (var avatar in SpawnedAvatars)
            {
                avatar?.Dispose();
            }
            SpawnedAvatars.Clear();
            AvatarToEdit = null;
            SpawnAvatarStateDisplay?.UpdateStateDisplay(null, null);
            AvatarEditorStateDisplay?.UpdateStateDisplay(null, AvatarEditorCamera);
        }

        [InspectorButton("===== Avatar Editor =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarEditor() { }

        [InspectorButton("Check Is Avatar Editor Open", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void CheckIsAvatarEditorOpen()
        {
            var isOpen = AvatarSdk.IsAvatarEditorOpen;
            ShowPopUp("Is Avatar Editor Open?", $"{(isOpen ? "TRUE" : "FALSE")}");
        }

        [InspectorButton("\nOpen Avatar Editor\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void OpenAvatarEditor()
        {
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                ShowPopUp("⚠️ Open Avatar Editor", "Avatar Editor is already open!");
                return;
            }

            if (AvatarToEdit == null)
            {
                ShowPopUp("⚠️ Open Avatar Editor", "No avatars spawned! Spawn an avatar first!");
                return;
            }

            try
            {
                await AvatarSdk.OpenAvatarEditorAsync(AvatarToEdit.ManagedAvatar, AvatarEditorCamera);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open Avatar Editor: {ex.Message}");
            }
        }

        [InspectorButton("Close Avatar Editor", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void CloseAvatarEditor()
        {
            if (AvatarSdk.IsAvatarEditorOpen is false)
            {
                ShowPopUp("⚠️ Close Avatar Editor", "Avatar Editor is already closed!");
                return;
            }

            try
            {
                await AvatarSdk.CloseAvatarEditorAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to close Avatar Editor: {ex.Message}");
            }
        }

        [InspectorButton("===== Wearables & Assets =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderWearablesAssets() { }

        [InspectorButton("Get Wearable Asset Info List", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetWearableAssetInfoList()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Wearable Asset Info", "Log in first!");
                return;
            }

            try
            {
                var wearableAssets = await AvatarSdk.GetWearableAssetInfoListAsync();

                if (wearableAssets == null || wearableAssets.Count == 0)
                {
                    ShowPopUp("Wearable Asset Info", "No wearable assets found.");
                    return;
                }

                var message = $"Found {wearableAssets.Count} wearable assets:\n\n";
                for (int i = 0; i < Math.Min(wearableAssets.Count, 10); i++) // Show first 10 to avoid overwhelming the popup
                {
                    var asset = wearableAssets[i];
                    message += $"{i + 1}. {asset.Name}\n" +
                              $"   ID: {asset.AssetId}\n" +
                              $"   Type: {asset.AssetType}\n" +
                              $"   Category: {asset.Category}\n\n";
                }

                if (wearableAssets.Count > 10)
                {
                    message += $"... and {wearableAssets.Count - 10} more assets.\n";
                }

                Debug.Log($"Wearable Assets List:\n{message}");
                ShowPopUp("Wearable Asset Info", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get wearable asset info: {ex.Message}");
                ShowPopUp("⚠️ Get Wearable Asset Info", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet User's Assets\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetUsersAssets()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get User's Assets", "Log in first!");
                return;
            }

            try
            {
                var userAssets = await AvatarSdk.GetUsersAssetsAsync();

                if (userAssets == null || userAssets.Count == 0)
                {
                    ShowPopUp("User's Assets", "No assets found in user's inventory.");
                    return;
                }

                var message = $"Found {userAssets.Count} assets in user's inventory:\n\n";
                for (int i = 0; i < Math.Min(userAssets.Count, 10); i++) // Show first 10 to avoid overwhelming the popup
                {
                    var asset = userAssets[i];
                    message += $"{i + 1}. {asset.Name}\n" +
                              $"   ID: {asset.AssetId}\n" +
                              $"   Type: {asset.AssetType}\n" +
                              $"   Category: {asset.Category}\n\n";
                }

                if (userAssets.Count > 10)
                {
                    message += $"... and {userAssets.Count - 10} more assets.\n";
                }

                Debug.Log($"User's Assets List:\n{message}");
                ShowPopUp("User's Assets", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get user's assets: {ex.Message}");
                ShowPopUp("⚠️ Get User's Assets", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("===== Avatar Modifications =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarModifications() { }

        [InspectorButton("Give Asset to User", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GiveAssetToUser()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Give Asset to User", "Log in first!");
                return;
            }

            try
            {
                string assetId = _assetIdToGrant;

                // If no asset ID specified, get the first available asset
                if (string.IsNullOrEmpty(assetId))
                {
                    ShowPopUp("⚠️ Give Asset to User", "No asset ID specified!");
                    return;
                }


                var result = await AvatarSdk.GiveAssetToUserAsync(assetId);
                if (result.Item1)
                {
                    Debug.Log($"Successfully granted asset to user: {assetId}");
                    ShowPopUp("✅ Give Asset to User", $"Successfully granted asset: {assetId}");
                }
                else
                {
                    Debug.LogError($"Failed to give asset to user");
                    ShowPopUp("⚠️ Give Asset to User", $"Failed to give asset: {result.Item2}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to give asset to user: {ex.Message}");
                ShowPopUp("⚠️ Give Asset to User", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("Set Avatar Body Type", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SetAvatarBodyType()
        {
            if (AvatarToEdit?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Avatar Body Type", "No avatar selected for editing! Spawn an avatar first.");
                return;
            }

            try
            {
                await AvatarSdk.SetAvatarBodyTypeAsync(AvatarToEdit.ManagedAvatar, _genderType, _bodySize);

                var message = $"Successfully set body type:\nGender: {_genderType}\nBody Size: {_bodySize}";
                Debug.Log(message);
                ShowPopUp("✅ Set Avatar Body Type", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set avatar body type: {ex.Message}");
                ShowPopUp("⚠️ Set Avatar Body Type", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nSet Skin Color\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SetSkinColor()
        {
            if (AvatarToEdit?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Set Skin Color", "No avatar selected for editing! Spawn an avatar first.");
                return;
            }

            try
            {
                await AvatarSdk.SetSkinColorAsync(AvatarToEdit.ManagedAvatar, _skinColor);

                var message = $"Successfully set skin color:\nColor: {_skinColor}";
                Debug.Log(message);
                ShowPopUp("✅ Set Skin Color", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set skin color: {ex.Message}");
                ShowPopUp("⚠️ Set Skin Color", $"Error: {ex.Message}");
            }
        }

        private void ShowPopUp(string title, string message)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog(title, message, "OK");
#endif
            Debug.LogWarning($"{title}: {message}");
        }
    }
}
