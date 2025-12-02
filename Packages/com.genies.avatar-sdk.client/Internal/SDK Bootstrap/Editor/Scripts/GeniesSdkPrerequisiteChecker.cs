using UnityEditor;
using UnityEngine;

namespace Genies.SdkBootstrap.Editor
{
    /// <summary>
    /// Helper class to check prerequisites for the Genies SDK.
    /// Used by both the wizard UI and the domain reload check.
    /// </summary>
    internal static class GeniesSdkPrerequisiteChecker
    {
        private static BuildTargetGroup[] SupportedPlatforms { get; } = new[]
        {
#if UNITY_EDITOR_WIN
            BuildTargetGroup.Standalone, // Experimental support for standalone Windows builds
#else
            // Standalone builds are only supported on Windows
#endif
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
        };

        public static string GetPackageName()
        {
#if ENABLE_UPM
#if GENIES_SDK_CORE_UTILS
            return "com.genies.avatar-sdk.client";
#else
            return "com.genies.sdk.avatar";
#endif
#else
            return ""; // Not applicable when UPM is disabled
#endif
        }

        public static bool IsSdkInstalled()
        {
#if ENABLE_UPM
            var packagePath = $"Packages/{GetPackageName()}";
            return AssetDatabase.IsValidFolder(packagePath);
#else
            // When UPM is disabled, we assume the SDK is installed if the bootstrap assembly exists
            return true;
#endif
        }

        public static bool IsPlatformSupported(BuildTargetGroup group)
        {
            foreach (var supportedPlatform in SupportedPlatforms)
            {
                if (group == supportedPlatform)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsActivePlatformSupported()
        {
            var activeBuildTargetGroup = GetActiveBuildTargetGroup();
            return IsPlatformSupported(activeBuildTargetGroup);
        }

        public static bool IsIL2CPPConfigured(BuildTargetGroup group)
        {
            try
            {
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                var backend = PlayerSettings.GetScriptingBackend(namedBuildTarget);
                return backend == ScriptingImplementation.IL2CPP;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsIL2CPPConfiguredForActivePlatform()
        {
            return IsIL2CPPConfigured(GetActiveBuildTargetGroup());
        }

        public static bool IsIL2CPPConfiguredForAllPlatforms()
        {
            foreach (var group in SupportedPlatforms)
            {
                if (!IsIL2CPPConfigured(group))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsNetFrameworkConfigured(BuildTargetGroup group)
        {
            try
            {
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                var apiLevel = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget);
                return apiLevel == ApiCompatibilityLevel.NET_Unity_4_8;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNetFrameworkConfiguredForActivePlatform()
        {
            return IsNetFrameworkConfigured(GetActiveBuildTargetGroup());
        }

        public static bool IsNetFrameworkConfiguredForAllPlatforms()
        {
            foreach (var group in SupportedPlatforms)
            {
                if (!IsNetFrameworkConfigured(group))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsVulkanConfiguredForWindows()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
                return graphicsApis != null && graphicsApis.Length > 0 && graphicsApis[0] == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsVulkanConfiguredForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                return graphicsApis != null && graphicsApis.Length > 0 && graphicsApis[0] == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsArm64ConfiguredForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                var targetArchitectures = PlayerSettings.Android.targetArchitectures;
                return targetArchitectures == AndroidArchitecture.ARM64;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsMinAndroidApiLevelConfigured()
        {
#if UNITY_ANDROID
            try
            {
                return PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel31;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsActiveInputHandlingConfigured()
        {
            // Check using scripting defines
            // ENABLE_INPUT_SYSTEM = New Input System
            // ENABLE_LEGACY_INPUT_MANAGER = Old Input Manager
            // Both defined = "Both" which causes build errors on Android

#if UNITY_ANDROID
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            // Both enabled - not allowed on Android
            return false;
#else
            // Only one enabled - valid configuration for Android
            return true;
#endif
#else
            // For non-Android platforms, we encourage using the new input system
            // but "Both" doesn't cause build errors, so we consider it configured
            // (The UI will still show a warning to encourage using new)
            return true;
#endif
        }

        public static int GetActiveInputHandlerValue()
        {
            // Use scripting defines to determine the active input handler
            // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both

#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            return 2; // Both
#elif ENABLE_INPUT_SYSTEM
            return 1; // New Input System
#elif ENABLE_LEGACY_INPUT_MANAGER
            return 0; // Old Input Manager
#else
            return -1; // Neither (shouldn't happen in normal Unity projects)
#endif
        }

        public static bool AreAllPrerequisitesMet()
        {
            if (!IsActivePlatformSupported())
            {
                return false;
            }

            if (!IsIL2CPPConfiguredForActivePlatform())
            {
                return false;
            }

            if (!IsNetFrameworkConfiguredForActivePlatform())
            {
                return false;
            }

            if (!IsVulkanConfiguredForWindows())
            {
                return false;
            }

            if (!IsVulkanConfiguredForAndroid())
            {
                return false;
            }

            if (!IsArm64ConfiguredForAndroid())
            {
                return false;
            }

            if (!IsMinAndroidApiLevelConfigured())
            {
                return false;
            }

            if (!IsActiveInputHandlingConfigured())
            {
                return false;
            }

            return true;
        }

        public static BuildTargetGroup GetActiveBuildTargetGroup()
        {
            return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        }

        public static BuildTargetGroup[] GetSupportedPlatforms()
        {
            return SupportedPlatforms;
        }

        public static string GetPlatformDisplayName(BuildTargetGroup group)
        {
            switch (group)
            {
                case BuildTargetGroup.Standalone:
                    return "Standalone (limited support for Windows)";
                case BuildTargetGroup.Android:
                    return "Android";
                case BuildTargetGroup.iOS:
                    return "iOS";
                default:
                    return group.ToString();
            }
        }

        public static string GetSupportedPlatformsListString()
        {
            var platformsList = new System.Text.StringBuilder();
            foreach (var platform in SupportedPlatforms)
            {
                platformsList.Append("• ");
                platformsList.Append(GetPlatformDisplayName(platform));
                platformsList.Append("\n");
            }
            return platformsList.ToString().TrimEnd('\n');
        }
    }
}

