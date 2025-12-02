using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Assertions;

namespace Genies.SdkBootstrap.Editor
{
    [InitializeOnLoad]
    internal class GeniesSdkBootstrapWizard : EditorWindow
    {
        private const int ProgressUpdateInterval = 16;
        private const string ShowWizardOnStartupPrefKey = "Genies.SdkBootstrap.Editor.ShowWizardOnStartup";

        // Static constructor called on domain reload
        static GeniesSdkBootstrapWizard()
        {
            EditorApplication.delayCall += OnEditorLoadSequence;
        }

        private static void OnEditorLoadSequence()
        {
            // First, initialize SDK version tracking for this session
            if (!GeniesSdkVersionChecker.InitializeSessionSdkVersion())
            {
                // Check if SDK version changed during this session and prompt for restart if needed
                var versionChangeInfo = GeniesSdkVersionChecker.CheckForSdkVersionChange();
                if (versionChangeInfo != null && versionChangeInfo.HasChanged)
                {
                    if (PromptEditorRestart(versionChangeInfo.Title, versionChangeInfo.Message))
                    {
                        return;
                    }
                }
            }

            // Then proceed with wizard checks
            CheckAndShowWizardOnLoad();
        }

        private static bool PromptEditorRestart(string dialogTitle, string dialogMessage)
        {
            if (EditorUtility.DisplayDialog(dialogTitle,
                dialogMessage,
                "Restart Now",
                "Restart Later (Not Recommended)"))
            {
                // User clicked "Restart Now" - restart the editor
                string projectPath = Application.dataPath.Replace("/Assets", "");
                EditorApplication.OpenProject(projectPath);
                return true;
            }

            return false;
        }

        private static void CheckAndShowWizardOnLoad()
        {
            // Check user preference for auto-showing wizard (inverted: true = disabled)
            bool disableAutoShow = EditorPrefs.GetBool(ShowWizardOnStartupPrefKey, false);
            bool showOnStartup = !disableAutoShow;

            // Check if SDK is already installed
            bool sdkInstalled = GeniesSdkPrerequisiteChecker.IsSdkInstalled();

            if (sdkInstalled)
            {
                // If SDK is installed, check if all prerequisites are met
                bool allPrerequisitesMet = GeniesSdkPrerequisiteChecker.AreAllPrerequisitesMet();

                if (!allPrerequisitesMet)
                {
                    if (showOnStartup)
                    {
                        // Show wizard if prerequisites aren't met even though SDK is installed
                        ShowWindow();
                    }
                    else
                    {
                        // Log warning instead of showing wizard
                        Debug.LogWarning(
                            $"[Genies SDK Bootstrap] Prerequisites are not fully met and are required for the Genies SDK to function. " +
                            $"Open the wizard manually via Tools > Genies > SDK Bootstrap Wizard to configure your project.");
                    }
                }
            }
            else
            {
                if (showOnStartup)
                {
                    // If SDK is not installed, show the wizard
                    ShowWindow();
                }
                else
                {
#if ENABLE_UPM
                    // Log warning instead of showing wizard
                    Debug.LogWarning(
                        $"[Genies SDK Bootstrap] The target SDK package ({GeniesSdkPrerequisiteChecker.GetPackageName()}) is NOT installed and is required for the Genies SDK to function. " +
                        $"Open the wizard manually via Tools > Genies > SDK Bootstrap Wizard to install it.");
#else
                    // When UPM is disabled, we assume SDK is always installed, so this should never trigger
                    // But keep a minimal log message just in case
                    Debug.LogWarning(
                        $"[Genies SDK Bootstrap] Open the wizard manually via Tools > Genies > SDK Bootstrap Wizard to configure your project.");
#endif
                }
            }
        }

        private Vector2 ScrollPosition { get; set; }
        [field: System.NonSerialized]
        private bool IsInstallingGeniesSdkAvatar { get; set; } = false;

        // Prerequisite check results
        [field: System.NonSerialized]
        private bool IsPlatformSupported { get; set; } = true;
        [field: System.NonSerialized]
        private bool Il2CppBackendConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private bool Il2CppBackendConfiguredAllPlatforms { get; set; } = false;
        [field: System.NonSerialized]
        private bool NetFrameworkConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private bool NetFrameworkConfiguredAllPlatforms { get; set; } = false;
        [field: System.NonSerialized]
        private bool ActivePlatformSupportsNetFramework { get; set; } = true;
        [field: System.NonSerialized]
        private string PlatformCompatibilityError { get; set; } = "";
        [field: System.NonSerialized]
        private bool GeniesAvatarSdkInstalled { get; set; } = false;
        [field: System.NonSerialized]
        private bool VulkanConfiguredForWindows { get; set; } = false;
        [field: System.NonSerialized]
        private bool VulkanConfiguredForAndroid { get; set; } = false;
        [field: System.NonSerialized]
        private bool Arm64ConfiguredForAndroid { get; set; } = false;
        [field: System.NonSerialized]
        private bool MinAndroidApiLevelConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private bool ActiveInputHandlingConfigured { get; set; } = false;

        private bool AllPrerequisitesMet => Il2CppBackendConfigured && NetFrameworkConfigured && VulkanConfiguredForWindows && VulkanConfiguredForAndroid && Arm64ConfiguredForAndroid && MinAndroidApiLevelConfigured && ActiveInputHandlingConfigured;

        // File watching
        [field: System.NonSerialized]
        private FileSystemWatcher ManifestWatcher { get; set; }
        [field: System.NonSerialized]
        private bool NeedsRefresh { get; set; } = false;
        [field: System.NonSerialized]
        private bool RefreshScheduled { get; set; } = false;
        [field: System.NonSerialized]
        private bool IsCompiling { get; set; } = false;

        [MenuItem("Tools/Genies/SDK Bootstrap Wizard", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<GeniesSdkBootstrapWizard>("Genies SDK Bootstrap Wizard");
            window.minSize = new Vector2(650, 500);
        }

        private void OnEnable()
        {
            IsCompiling = EditorApplication.isCompiling;
            RefreshPrerequisiteStatus();
            SetupFileWatchers();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            CleanupFileWatchers();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.isCompiling != IsCompiling)
            {
                IsCompiling = EditorApplication.isCompiling;
                Repaint();
            }
        }

        private void OnFocus()
        {
            // Refresh when window gains focus to detect external changes to project settings
            RefreshPrerequisiteStatus();
        }

        private void Update()
        {
            if (NeedsRefresh)
            {
                NeedsRefresh = false;
                RefreshPrerequisiteStatus();
                Repaint();
            }
        }

        private void OnGUI()
        {
            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

            // Add horizontal margins
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(15);

            // Refresh Status button (top right)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var refreshIcon = EditorGUIUtility.IconContent("Refresh");
            var refreshButton = new GUIContent(refreshIcon.image, "Refresh Status");
            if (GUILayout.Button(refreshButton, GUILayout.Width(24), GUILayout.Height(24)))
            {
                RefreshPrerequisiteStatus();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Large title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 20;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.wordWrap = true;
            EditorGUILayout.LabelField("Genies SDK Bootstrap Wizard", titleStyle);

            EditorGUILayout.Space(10);

            // Welcome message
            var welcomeStyle = new GUIStyle(EditorStyles.label);
            welcomeStyle.alignment = TextAnchor.MiddleCenter;
            welcomeStyle.wordWrap = true;
            welcomeStyle.fontSize = 12;
            EditorGUILayout.LabelField("Setup made easy\nDeveloper and Genies\nTogether we build", welcomeStyle);

            EditorGUILayout.Space(15);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "The Genies SDK Bootstrap Wizard is disabled while in Play mode.\n\n" +
                    "Please exit Play mode to use the wizard.",
                    MessageType.Warning);

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Exit Play Mode"))
                {
                    EditorApplication.isPlaying = false;
                }

                EditorGUILayout.Space(15);

                DrawQuickLinks();

                EditorGUILayout.Space(15);

                // Auto-show wizard preference
                DrawAutoShowWizardCheckbox();

                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                return;
            }

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox(
                    "The Genies SDK Bootstrap Wizard is disabled while the Editor is compiling.\n\n" +
                    "Please wait for compilation to complete.",
                    MessageType.Info);

                EditorGUILayout.Space(15);

                DrawQuickLinks();

                EditorGUILayout.Space(15);

                // Auto-show wizard preference
                DrawAutoShowWizardCheckbox();

                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                return;
            }

            // Current Build Platform section
            EditorGUILayout.LabelField("Current Build Platform", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var platformLabel = $"Platform: {activeBuildTarget}";
            EditorGUILayout.LabelField(platformLabel, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Open Build Settings", GUILayout.Width(150)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
            EditorGUILayout.EndHorizontal();

#if UNITY_STANDALONE_WIN
            // Warning about experimental Windows Standalone support
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Windows Standalone support is experimental and not yet fully supported. " +
                "You may encounter issues or limitations when building for this platform.",
                MessageType.Warning);
#endif

            // Check if platform is unsupported
            if (IsPlatformSupported is false)
            {
                var activeBuildTargetGroup = GeniesSdkPrerequisiteChecker.GetActiveBuildTargetGroup();
                var supportedPlatformsList = GeniesSdkPrerequisiteChecker.GetSupportedPlatformsListString();
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    $"Unsupported Platform: {activeBuildTargetGroup}\n\n" +
                    "The Genies SDK only supports the following platforms:\n" +
                    supportedPlatformsList + "\n" +
                    "Please switch to a supported platform in Build Settings to use this wizard.",
                    MessageType.Error);

                EditorGUILayout.Space(15);

                DrawQuickLinks();

                EditorGUILayout.Space(15);

                // Auto-show wizard preference
                DrawAutoShowWizardCheckbox();

                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField("Prerequisites", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawPlatformPrerequisiteCheck("IL2CPP Scripting Backend (Active Platform)", Il2CppBackendConfigured, Il2CppBackendConfiguredAllPlatforms, FixIl2CppBackendActivePlatform, FixIl2CppBackendAllPlatforms,
                "IL2CPP scripting backend is required for the Genies SDK.\n• Fix Active: Sets IL2CPP for the currently selected build target\n• Fix All: Sets IL2CPP for all supported build targets");

            DrawPlatformPrerequisiteCheck(".NET Framework 4.8 (Active Platform)", NetFrameworkConfigured, NetFrameworkConfiguredAllPlatforms, FixNetFrameworkActivePlatform, FixNetFrameworkAllPlatforms,
                ".NET Framework 4.8 is required for the Genies SDK.\n• Fix Active: Sets .NET Framework 4.8 for the currently selected build target\n• Fix All: Sets .NET Framework 4.8 for all compatible platforms");

            // Show error if active platform doesn't support .NET Framework
            if (!string.IsNullOrEmpty(PlatformCompatibilityError))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(PlatformCompatibilityError, MessageType.Error);
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            DrawPrerequisiteCheck("Vulkan Graphics API (Windows)", VulkanConfiguredForWindows, FixVulkanForWindows,
                "Vulkan is required as the graphics API for Windows Standalone builds when using the Genies SDK.");
#endif

#if UNITY_ANDROID
            DrawPrerequisiteCheck("Vulkan Graphics API (Android)", VulkanConfiguredForAndroid, FixVulkanForAndroid,
                "Vulkan is required as the graphics API for Android builds when using the Genies SDK.");

            DrawPrerequisiteCheck("ARM64 Architecture (Android)", Arm64ConfiguredForAndroid, FixArm64ForAndroid,
                "ARM64 architecture is required for Android builds when using the Genies SDK.");

            DrawPrerequisiteCheck("Minimum Android 12.0 (API Level 31)", MinAndroidApiLevelConfigured, FixMinAndroidApiLevel,
                "Android 12.0 (API level 31) is required as the minimum API level for Android builds when using the Genies SDK.");
#endif

            // Show Active Input Handling for all platforms to encourage using the new Input System
            DrawActiveInputHandlingCheck();

            EditorGUILayout.Space(10);

#if ENABLE_UPM
            // Install button and configure gear icon in horizontal group
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(IsInstallingGeniesSdkAvatar || GeniesAvatarSdkInstalled);

            var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
            var buttonText = GeniesAvatarSdkInstalled
                ? $"Genies Avatar SDK (Already Installed)\n{packageName}"
                : $"Install 'Genies Avatar SDK'\n{packageName}";
            if (GUILayout.Button(buttonText, GUILayout.Height(45)))
            {
                InstallGeniesSdkAvatar();
            }

            EditorGUI.EndDisabledGroup();

            // Configure gear icon button
            DrawConfigureButton(isDisabled: !GeniesAvatarSdkInstalled, applyHighlightColor: GeniesAvatarSdkInstalled);

            EditorGUILayout.EndHorizontal();

            if (!AllPrerequisitesMet)
            {
                EditorGUILayout.HelpBox("It is recommended to fix all prerequisites before installing 'Genies Avatar SDK'.", MessageType.Warning);
            }
            else if (GeniesAvatarSdkInstalled)
            {
                EditorGUILayout.HelpBox("Genies Avatar SDK is installed. Click the gear icon button above to configure required SDK settings.", MessageType.Info);
            }
            else if (!IsInstallingGeniesSdkAvatar)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    $"\nREQUIRED: The target SDK package ({GeniesSdkPrerequisiteChecker.GetPackageName()}) is NOT installed.\n\n" +
                    "The core packages should NEVER be used without the SDK package installed.\n" +
                    "Please click the button above to install it now.\n",
                    MessageType.Error);
                EditorGUILayout.Space(5);
            }

            if (IsInstallingGeniesSdkAvatar)
            {
                EditorGUILayout.HelpBox("Installing 'Genies Avatar SDK' package...", MessageType.Info);
            }
#else
            // When UPM is disabled, show a prominent centered configure button
            EditorGUILayout.Space(10);

            // Center-aligned prominent gear icon button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DrawConfigureButton(isDisabled: false, applyHighlightColor: true);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (!AllPrerequisitesMet)
            {
                EditorGUILayout.HelpBox("It is recommended to fix all prerequisites before using the Genies Avatar SDK.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("All prerequisites met! Click the gear icon above to configure required SDK settings.", MessageType.Info);
            }
#endif

            EditorGUILayout.Space(15);

            DrawQuickLinks();

            EditorGUILayout.Space(15);

            // Auto-show wizard preference
            DrawAutoShowWizardCheckbox();

            EditorGUILayout.EndVertical();
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void DrawPrerequisiteCheck(string label, bool isConfigured, System.Action fixAction, string tooltip = "")
        {
            EditorGUILayout.BeginHorizontal();

            // Status icon
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            iconStyle.normal.textColor = isConfigured ? Color.green : Color.red;
            EditorGUILayout.LabelField(isConfigured ? "✓" : "✗", iconStyle, GUILayout.Width(20));

            // Label with tooltip
            var labelContent = new GUIContent(label, tooltip);
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Fix button
            EditorGUI.BeginDisabledGroup(isConfigured);
            var buttonTooltip = isConfigured ? "Already configured" : $"Click to fix this prerequisite";
            var buttonContent = new GUIContent("Fix", buttonTooltip);
            if (GUILayout.Button(buttonContent, GUILayout.Width(50)))
            {
                fixAction?.Invoke();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlatformPrerequisiteCheck(string label, bool isActivePlatformConfigured, bool areAllPlatformsConfigured, System.Action fixActivePlatformAction, System.Action fixAllPlatformsAction, string tooltip = "")
        {
            EditorGUILayout.BeginHorizontal();

            // Status icon for active platform
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            iconStyle.normal.textColor = isActivePlatformConfigured ? Color.green : Color.red;
            EditorGUILayout.LabelField(isActivePlatformConfigured ? "✓" : "✗", iconStyle, GUILayout.Width(20));

            // Label with tooltip
            var labelContent = new GUIContent(label, tooltip);
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Fix Active Platform button
            EditorGUI.BeginDisabledGroup(isActivePlatformConfigured);
            var fixActiveTooltip = isActivePlatformConfigured ? "Active platform already configured" : "Configure setting for the currently selected build target only";
            var fixActiveContent = new GUIContent("Fix Active", fixActiveTooltip);
            if (GUILayout.Button(fixActiveContent, GUILayout.Width(75)))
            {
                fixActivePlatformAction?.Invoke();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

            // Fix All Platforms button
            EditorGUI.BeginDisabledGroup(areAllPlatformsConfigured);
            var fixAllTooltip = areAllPlatformsConfigured ? "All platforms already configured" : "Configure setting for all supported build targets";
            var fixAllContent = new GUIContent("Fix All", fixAllTooltip);
            if (GUILayout.Button(fixAllContent, GUILayout.Width(60)))
            {
                fixAllPlatformsAction?.Invoke();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActiveInputHandlingCheck()
        {
            EditorGUILayout.BeginHorizontal();

            // Check current input handler value
            int currentInputHandler = GeniesSdkPrerequisiteChecker.GetActiveInputHandlerValue();
            bool isSetToOld = currentInputHandler == 0;
            bool isSetToNew = currentInputHandler == 1;
            bool isSetToBoth = currentInputHandler == 2;

            // Status icon - green if new, yellow if old, red if both
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            if (isSetToNew)
            {
                iconStyle.normal.textColor = Color.green;
            }
            else if (isSetToOld)
            {
                iconStyle.normal.textColor = new Color(1.0f, 0.65f, 0.0f); // Orange/yellow for warning
            }
            else
            {
                iconStyle.normal.textColor = Color.red;
            }

            string statusIcon = isSetToNew ? "✓" : (isSetToOld ? "⚠" : "✗");
            EditorGUILayout.LabelField(statusIcon, iconStyle, GUILayout.Width(20));

            // Label with tooltip - include current setting suffix
            string currentSetting = isSetToNew ? "New" : (isSetToOld ? "Old" : (isSetToBoth ? "Both" : "Unknown"));
            var labelContent = new GUIContent(
                $"Active Input Handling (Current: {currentSetting})",
                "The new Input System is STRONGLY RECOMMENDED for all projects. " +
                "On Android, 'Both' is not allowed as it causes build errors.");
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Use Old Input System button - disabled if already set to old OR new
            EditorGUI.BeginDisabledGroup(isSetToOld || isSetToNew);
            var useOldTooltip = isSetToOld
                ? "Already set to Input Manager (Old)"
                : isSetToNew
                    ? "Already configured to use the new Input System. Setting to 'Old' only is discouraged. You may do so manually through Project Settings."
                    : "Set to Input Manager (Old) - Legacy input system";
            var useOldContent = new GUIContent("Use Old", useOldTooltip);
            if (GUILayout.Button(useOldContent, GUILayout.Width(70)))
            {
                FixActiveInputHandlingToOld();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

#if !UNITY_ANDROID
            // Use Both Input Systems button - only available on non-Android platforms, disabled if already set to both
            EditorGUI.BeginDisabledGroup(isSetToBoth);
            var useBothTooltip = isSetToBoth
                ? "Already set to Both"
                : "Enable both Input Manager and Input System (not recommended, Android incompatible)";
            var useBothContent = new GUIContent("Use Both", useBothTooltip);
            if (GUILayout.Button(useBothContent, GUILayout.Width(75)))
            {
                FixActiveInputHandlingToBoth();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();
#endif

            // Use New Input System button - disabled if already set to new
            EditorGUI.BeginDisabledGroup(isSetToNew);
            var originalColor = GUI.backgroundColor;
            if (!isSetToNew)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f, 1.0f); // Vibrant cyan to highlight recommendation
            }

            var useNewTooltip = isSetToNew
                ? "Already set to Input System Package (New)"
                : "RECOMMENDED: Set to Input System Package (New) - Modern, feature-rich input system";
            var useNewContent = new GUIContent("Use New ★", useNewTooltip);
            if (GUILayout.Button(useNewContent, GUILayout.Width(85)))
            {
                FixActiveInputHandlingToNew();
                RefreshPrerequisiteStatus();
            }

            GUI.backgroundColor = originalColor;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Warning/Error helpboxes based on current configuration
            string helpBoxMessage = null;
            MessageType helpBoxType = MessageType.None;

            if (isSetToOld)
            {
                // Old input system - warning on all platforms
                helpBoxMessage = "️You are using the legacy Input Manager (Old). The new Input System is STRONGLY RECOMMENDED for better functionality, performance, and future compatibility. Click 'Use New ★' to upgrade.";
                helpBoxType = MessageType.Warning;
            }
            else if (isSetToBoth)
            {
#if UNITY_ANDROID
                // Both enabled on Android - critical error
                helpBoxMessage = "Active Input Handling is set to 'Both' which will cause Android build errors. You must choose either 'Use New ★' (recommended) or 'Use Old'.";
                helpBoxType = MessageType.Error;
#else
                // Both enabled on non-Android - warning about Android compatibility
                helpBoxMessage = "Active Input Handling is set to 'Both'. While this works on your current platform, it will cause build errors if you target Android. We recommend using only the new Input System. Click 'Use New ★'.";
                helpBoxType = MessageType.Warning;
#endif
            }

            // Display helpbox if there's a message
            if (!string.IsNullOrEmpty(helpBoxMessage))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox(helpBoxMessage, helpBoxType);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
        }

        private void DrawQuickLinks()
        {
            EditorGUILayout.LabelField("Resources & Help", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // External links row
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Genies Hub", GUILayout.MinWidth(200)))
            {
                MenuItems.ExternaLinks.OpenGeniesHub();
            }

            // Divider
            GUILayout.Space(15);
            var dividerRect = EditorGUILayout.GetControlRect(GUILayout.Width(1), GUILayout.Height(20));
            EditorGUI.DrawRect(dividerRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GUILayout.Space(15);

            if (GUILayout.Button("Technical Documentation", GUILayout.MinWidth(200)))
            {
                MenuItems.ExternaLinks.OpenGeniesTechnicalDocumentation();
            }

            if (GUILayout.Button("Genies Support", GUILayout.MinWidth(200)))
            {
                MenuItems.ExternaLinks.OpenGeniesSupport();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAutoShowWizardCheckbox()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            bool disableAutoShow = EditorPrefs.GetBool(ShowWizardOnStartupPrefKey, false);
            bool newDisableAutoShow = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Disable wizard from showing automatically on startup",
                    "When checked, this wizard will NOT automatically appear when Unity starts. " +
                    "Instead, a warning will be logged to the console if prerequisites are not met or the SDK is not installed."),
                disableAutoShow);

            if (newDisableAutoShow != disableAutoShow)
            {
                EditorPrefs.SetBool(ShowWizardOnStartupPrefKey, newDisableAutoShow);
            }

            if (newDisableAutoShow)
            {
                EditorGUILayout.Space(3);
                var noteStyle = new GUIStyle(EditorStyles.miniLabel);
                noteStyle.wordWrap = true;
                EditorGUILayout.LabelField(
                    "Note: Warnings will be logged to the console when prerequisites are not met.",
                    noteStyle);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private bool DrawConfigureButton(bool isDisabled, bool applyHighlightColor)
        {
            var gearIcon = EditorGUIUtility.IconContent("Settings");
            var configureButton = new GUIContent(gearIcon.image, "Configure Genies SDK Project Settings");

            EditorGUI.BeginDisabledGroup(isDisabled);

            var originalColor = GUI.backgroundColor;
            if (applyHighlightColor)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f, 1.0f); // Vibrant cyan color
            }

            bool clicked = GUILayout.Button(configureButton, GUILayout.Width(45), GUILayout.Height(45));

            GUI.backgroundColor = originalColor;
            EditorGUI.EndDisabledGroup();

            if (clicked)
            {
                SettingsService.OpenProjectSettings("Project/Genies");
            }

            return clicked;
        }

        private void RefreshPrerequisiteStatus()
        {
            if (RefreshScheduled)
            {
                return;
            }
            RefreshScheduled = true;

            EditorApplication.delayCall += () =>
            {
                RefreshScheduled = false;

                CheckPlatformSupport();
                if (IsPlatformSupported)
                {
                    CheckIL2CPPBackend();
                    CheckIL2CPPBackendAllPlatforms();
                    CheckNetFramework();
                    CheckNetFrameworkAllPlatforms();
                    CheckVulkanForWindows();
                    CheckVulkanForAndroid();
                    CheckArm64ForAndroid();
                    CheckMinAndroidApiLevel();
                    CheckActiveInputHandling();
                    CheckGeniesAvatarSdkInstalled();
                }

                Repaint();
            };
        }

        private void CheckPlatformSupport()
        {
            IsPlatformSupported = GeniesSdkPrerequisiteChecker.IsActivePlatformSupported();
        }

        private void CheckIL2CPPBackend()
        {
            Il2CppBackendConfigured = GeniesSdkPrerequisiteChecker.IsIL2CPPConfiguredForActivePlatform();
        }

        private void CheckIL2CPPBackendAllPlatforms()
        {
            Il2CppBackendConfiguredAllPlatforms = GeniesSdkPrerequisiteChecker.IsIL2CPPConfiguredForAllPlatforms();
        }

        private void CheckNetFramework()
        {
            var activeBuildTargetGroup = GeniesSdkPrerequisiteChecker.GetActiveBuildTargetGroup();
            NetFrameworkConfigured = GeniesSdkPrerequisiteChecker.IsNetFrameworkConfiguredForActivePlatform();

            if (!NetFrameworkConfigured)
            {
                ActivePlatformSupportsNetFramework = GeniesSdkPrerequisiteChecker.IsPlatformSupported(activeBuildTargetGroup);
                if (!ActivePlatformSupportsNetFramework)
                {
                    var supportedPlatforms = string.Join(", ", System.Array.ConvertAll(
                        GeniesSdkPrerequisiteChecker.GetSupportedPlatforms(),
                        p => GeniesSdkPrerequisiteChecker.GetPlatformDisplayName(p)));
                    PlatformCompatibilityError = $"The active platform ({activeBuildTargetGroup}) is not supported by the Genies SDK. Please switch to a compatible platform: {supportedPlatforms}.";
                }
                else
                {
                    PlatformCompatibilityError = "";
                }
            }
            else
            {
                ActivePlatformSupportsNetFramework = true;
                PlatformCompatibilityError = "";
            }
        }

        private void CheckNetFrameworkAllPlatforms()
        {
            NetFrameworkConfiguredAllPlatforms = GeniesSdkPrerequisiteChecker.IsNetFrameworkConfiguredForAllPlatforms();
        }

        private void CheckGeniesAvatarSdkInstalled()
        {
            GeniesAvatarSdkInstalled = GeniesSdkPrerequisiteChecker.IsSdkInstalled();
        }

        private void CheckVulkanForWindows()
        {
            VulkanConfiguredForWindows = GeniesSdkPrerequisiteChecker.IsVulkanConfiguredForWindows();
        }

        private void CheckVulkanForAndroid()
        {
            VulkanConfiguredForAndroid = GeniesSdkPrerequisiteChecker.IsVulkanConfiguredForAndroid();
        }

        private void CheckArm64ForAndroid()
        {
            Arm64ConfiguredForAndroid = GeniesSdkPrerequisiteChecker.IsArm64ConfiguredForAndroid();
        }

        private void CheckMinAndroidApiLevel()
        {
            MinAndroidApiLevelConfigured = GeniesSdkPrerequisiteChecker.IsMinAndroidApiLevelConfigured();
        }

        private void CheckActiveInputHandling()
        {
            ActiveInputHandlingConfigured = GeniesSdkPrerequisiteChecker.IsActiveInputHandlingConfigured();
        }

#if ENABLE_UPM
        private async void SearchAndInstallPackage(string packageName, Action<AddRequest> onPackageInstallComplete)
        {
            var progress = 0f;
            var progressIncrement = ProgressUpdateInterval * 0.001f;

            var search = Client.Search(packageName);

            while (search.IsCompleted is false)
            {
                EditorUtility.DisplayProgressBar($"Fetching {packageName}", "", Math.Min(progress, 0.9f));
                await Task.Delay(ProgressUpdateInterval);
                progress += progressIncrement;
            }
            EditorUtility.ClearProgressBar();

            if (search.Error is not null)
            {
                Debug.LogError($"Failed to fetch {packageName}: {search.Error}");
                EditorUtility.DisplayDialog("Failed to fetch package", $"Failed to fetch {packageName}: {search.Error}", "OK");
                return;
            }

            progress = 0f;
            EditorUtility.DisplayProgressBar($"Installing {packageName}", "", Math.Min(progress, 0.9f));
            var request = Client.Add(packageName);

            EditorApplication.update -= OnComplete;
            EditorApplication.update += OnComplete;

            while (request.IsCompleted is false)
            {
                EditorUtility.DisplayProgressBar($"Installing {packageName}", "", Math.Min(progress, 0.9f));
                await Task.Delay(ProgressUpdateInterval);
                progress += progressIncrement;
            }

            void OnComplete()
            {
                if (request.IsCompleted)
                {
                    EditorApplication.update -= OnComplete;

                    EditorUtility.ClearProgressBar();
                    onPackageInstallComplete?.Invoke(request);
                }
            }
        }
#endif

        private void FixIl2CppBackendActivePlatform()
        {
            FixPlatformSetting(
                activePlatformOnly: true,
                setPlatformSetting: (group) =>
                {
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    PlayerSettings.SetScriptingBackend(namedBuildTarget, ScriptingImplementation.IL2CPP);
                },
                settingName: "IL2CPP scripting backend"
            );
        }

        private void FixIl2CppBackendAllPlatforms()
        {
            FixPlatformSetting(
                activePlatformOnly: false,
                setPlatformSetting: (group) =>
                {
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    PlayerSettings.SetScriptingBackend(namedBuildTarget, ScriptingImplementation.IL2CPP);
                },
                settingName: "IL2CPP scripting backend"
            );
        }

        private void FixNetFrameworkActivePlatform()
        {
            var activeBuildTargetGroup = GeniesSdkPrerequisiteChecker.GetActiveBuildTargetGroup();
            if (!GeniesSdkPrerequisiteChecker.IsPlatformSupported(activeBuildTargetGroup))
            {
                var supportedPlatforms = string.Join(", ", System.Array.ConvertAll(
                    GeniesSdkPrerequisiteChecker.GetSupportedPlatforms(),
                    p => GeniesSdkPrerequisiteChecker.GetPlatformDisplayName(p)));
                Debug.LogError($"Cannot set .NET Framework 4.8 for {activeBuildTargetGroup}. This platform is not supported by the Genies SDK.");
                EditorUtility.DisplayDialog("Platform Not Supported",
                    $"The active platform ({activeBuildTargetGroup}) is not supported by the Genies SDK.\n\nPlease switch to a compatible platform: {supportedPlatforms}.",
                    "OK");
                return;
            }

            FixPlatformSetting(
                activePlatformOnly: true,
                setPlatformSetting: (group) =>
                {
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    PlayerSettings.SetApiCompatibilityLevel(namedBuildTarget, ApiCompatibilityLevel.NET_Unity_4_8);
                },
                settingName: ".NET Framework 4.8"
            );
        }

        private void FixNetFrameworkAllPlatforms()
        {
            FixPlatformSetting(
                activePlatformOnly: false,
                setPlatformSetting: (group) =>
                {
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    PlayerSettings.SetApiCompatibilityLevel(namedBuildTarget, ApiCompatibilityLevel.NET_Unity_4_8);
                },
                settingName: ".NET Framework 4.8"
            );
        }

        private void FixVulkanForWindows()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var graphicsApis = new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan };
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, graphicsApis);
                EnsureSettingsAreSaved();
                Debug.Log("Vulkan graphics API configured for Windows Standalone builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Vulkan graphics API for Windows: {e.Message}");
            }
#else
            Debug.LogWarning("Vulkan configuration is only available on Windows platforms.");
#endif
        }

        private void FixVulkanForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                var graphicsApis = new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan };
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsApis);
                EnsureSettingsAreSaved();
                Debug.Log("Vulkan graphics API configured for Android builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Vulkan graphics API for Android: {e.Message}");
            }
#else
            Debug.LogWarning("Vulkan configuration is only available when Android support is installed.");
#endif
        }

        private void FixArm64ForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                EnsureSettingsAreSaved();
                Debug.Log("ARM64 architecture configured for Android builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set ARM64 architecture for Android: {e.Message}");
            }
#else
            Debug.LogWarning("ARM64 configuration is only available when Android support is installed.");
#endif
        }

        private void FixMinAndroidApiLevel()
        {
#if UNITY_ANDROID
            try
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel31;
                EnsureSettingsAreSaved();
                Debug.Log("Minimum Android API level set to Android 12.0 (API level 31) for Android builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set minimum Android API level: {e.Message}");
            }
#else
            Debug.LogWarning("Minimum Android API level configuration is only available when Android support is installed.");
#endif
        }

        private void FixActiveInputHandlingToNew()
        {
            try
            {
#if HAS_INPUT_SYSTEM // Custom define to check if the Input System package is installed
                // Set active input handler to Input System Package (New)
                // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
                if (SetActiveInputHandlerInProjectSettings(1))
                {
                    _ = ApplySettingAndPromptRestart(
                        "Active Input Handling set to 'Input System Package (New)' - RECOMMENDED for modern projects. Unity Editor will restart for changes to take effect.",
                        "Input Handling Changed",
                        "Active Input Handling has been set to 'Input System Package (New)'.\n\n" +
                        "Unity Editor needs to restart for the changes to take effect.");
                }
                else
                {
                    Debug.LogError("Failed to update Active Input Handling setting in ProjectSettings.asset");
                }
#else
                Debug.LogError("Cannot set to Input System Package (New) because the Input System package is not installed. " +
                    "The Input System package will be installed automatically when you install the Genies Avatar SDK. " +
                    "You can also install it manually via Window > Package Manager, or use 'Use Old' instead.");
                EditorUtility.DisplayDialog("Input System Package Not Installed",
                    "The Input System package is not installed in your project.\n\n" +
                    "The Input System package will be installed automatically when you install the Genies Avatar SDK.\n\n" +
                    "Alternatively:\n" +
                    "• Install it manually via Window > Package Manager\n" +
                    "• Click 'Use Old' to use the legacy Input Manager (not recommended)",
                    "OK");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Active Input Handling to new input system: {e.Message}");
            }
        }

        private void FixActiveInputHandlingToOld()
        {
            // Show warning and confirmation dialog
            if (!EditorUtility.DisplayDialog("Use Old Input System?",
                "⚠️ WARNING: The new Input System is STRONGLY RECOMMENDED for modern Unity projects.\n\n" +
                "The old Input Manager has limited functionality and is considered legacy.\n\n" +
                "Are you sure you want to use the old Input Manager instead of the new Input System?",
                "Yes, Use Old",
                "Cancel"))
            {
                // User clicked "Cancel" - abort
                return;
            }

            try
            {
                // Set active input handler to Input Manager (Old)
                // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
                if (SetActiveInputHandlerInProjectSettings(0))
                {
                    _ = ApplySettingAndPromptRestart(
                        "Active Input Handling set to 'Input Manager (Old)' - Consider upgrading to the new Input System for better functionality. Unity Editor will restart for changes to take effect.",
                        "Input Handling Changed",
                        "Active Input Handling has been set to 'Input Manager (Old)'.\n\n" +
                        "Unity Editor needs to restart for the changes to take effect.");
                }
                else
                {
                    Debug.LogError("Failed to update Active Input Handling setting in ProjectSettings.asset");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Active Input Handling to old input system: {e.Message}");
            }
        }

        private void FixActiveInputHandlingToBoth()
        {
            // Show warning and confirmation dialog
            if (!EditorUtility.DisplayDialog("Use Both Input Systems?",
                "⚠️ WARNING: The new Input System ONLY is STRONGLY RECOMMENDED for modern Unity projects.\n\n" +
                "Using 'Both' enables both the old Input Manager and new Input System simultaneously, which:\n" +
                "• Increases build size and complexity\n" +
                "• Will cause build errors on Android\n" +
                "• Is not recommended for production\n\n" +
                "Are you sure you want to enable both input systems instead of using only the new Input System?",
                "Yes, Use Both",
                "Cancel"))
            {
                // User clicked "Cancel" - abort
                return;
            }

            try
            {
                // Set active input handler to Both
                // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
                if (SetActiveInputHandlerInProjectSettings(2))
                {
                    _ = ApplySettingAndPromptRestart(
                        "Active Input Handling set to 'Both' - This is not recommended and will cause Android build errors. Unity Editor will restart for changes to take effect.",
                        "Input Handling Changed",
                        "Active Input Handling has been set to 'Both'.\n\n" +
                        "Unity Editor needs to restart for the changes to take effect.");
                }
                else
                {
                    Debug.LogError("Failed to update Active Input Handling setting in ProjectSettings.asset");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Active Input Handling to both input systems: {e.Message}");
            }
        }

        private bool SetActiveInputHandlerInProjectSettings(int value)
        {
            try
            {
                string projectSettingsPath = Path.Combine(Application.dataPath, "../ProjectSettings/ProjectSettings.asset");
                if (!File.Exists(projectSettingsPath))
                {
                    Debug.LogError("ProjectSettings.asset file not found.");
                    return false;
                }

                string[] lines = File.ReadAllLines(projectSettingsPath);
                bool found = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("activeInputHandler:"))
                    {
                        lines[i] = $"  activeInputHandler: {value}";
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    File.WriteAllLines(projectSettingsPath, lines);
                    AssetDatabase.Refresh();
                    return true;
                }
                else
                {
                    Debug.LogError("activeInputHandler property not found in ProjectSettings.asset");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating ProjectSettings.asset: {e.Message}");
                return false;
            }
        }

        private bool ApplySettingAndPromptRestart(string logMessage, string dialogTitle, string dialogMessage)
        {
            EnsureSettingsAreSaved();
            Debug.Log(logMessage);
            return PromptEditorRestart(dialogTitle, dialogMessage);
        }

        private void FixPlatformSetting(bool activePlatformOnly, System.Action<BuildTargetGroup> setPlatformSetting, string settingName)
        {
            if (activePlatformOnly)
            {
                var activeBuildTargetGroup = GeniesSdkPrerequisiteChecker.GetActiveBuildTargetGroup();
                try
                {
                    setPlatformSetting(activeBuildTargetGroup);
                    EnsureSettingsAreSaved();
                    Debug.Log($"{settingName} configured for active platform: {activeBuildTargetGroup}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to set {settingName} for active platform {activeBuildTargetGroup}: {e.Message}");
                }
            }
            else
            {
                foreach (var group in GeniesSdkPrerequisiteChecker.GetSupportedPlatforms())
                {
                    try
                    {
                        setPlatformSetting(group);
                    }
                    catch
                    {
                        continue;
                    }
                }

                EnsureSettingsAreSaved();
                Debug.Log($"{settingName} configured for all supported platforms.");
            }
        }

#if ENABLE_UPM
        private void InstallGeniesSdkAvatar()
        {
            IsInstallingGeniesSdkAvatar = true;
            SearchAndInstallPackage(GeniesSdkPrerequisiteChecker.GetPackageName(), OnGeniesSdkInstallComplete);
            IsInstallingGeniesSdkAvatar = false;
        }

        private void OnGeniesSdkInstallComplete(AddRequest addRequest)
        {
            Assert.IsTrue(addRequest.IsCompleted);

            var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Successfully installed {packageName}");
                EditorUtility.DisplayDialog("Success",
                    $"'Genies Avatar SDK' ({packageName}) has been successfully installed!", "OK");
            }
            else
            {
                Debug.LogError($"Failed to install {packageName}: {addRequest.Error.message}");
                EditorUtility.DisplayDialog("Installation Failed",
                    $"Failed to install 'Genies Avatar SDK':\n{addRequest.Error.message}", "OK");
            }

            RefreshPrerequisiteStatus();
            Repaint();
        }
#endif

#if ENABLE_UPM
        private string GetManifestPath()
        {
            return Path.Combine(Application.dataPath, "../Packages/manifest.json");
        }
#endif

        private void SetupFileWatchers()
        {
            CleanupFileWatchers();

#if ENABLE_UPM
            try
            {
                // Watch manifest.json
                var manifestPath = GetManifestPath();
                if (File.Exists(manifestPath))
                {
                    var manifestDirectory = Path.GetDirectoryName(manifestPath);
                    var manifestFileName = Path.GetFileName(manifestPath);

                    ManifestWatcher = new FileSystemWatcher(manifestDirectory, manifestFileName);
                    ManifestWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime;
                    ManifestWatcher.Changed += OnFileChanged;
                    ManifestWatcher.Created += OnFileChanged;
                    ManifestWatcher.Renamed += OnFileRenamed;
                    ManifestWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to setup file watchers: {e.Message}");
            }
#endif
        }

        private void CleanupFileWatchers()
        {
            if (ManifestWatcher != null)
            {
                ManifestWatcher.Changed -= OnFileChanged;
                ManifestWatcher.Created -= OnFileChanged;
                ManifestWatcher.Renamed -= OnFileRenamed;
                ManifestWatcher.Dispose();
                ManifestWatcher = null;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            NeedsRefresh = true;
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            NeedsRefresh = true;
        }

        private void EnsureSettingsAreSaved()
        {
            // Save and serialize assets to ensure settings persist
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}
