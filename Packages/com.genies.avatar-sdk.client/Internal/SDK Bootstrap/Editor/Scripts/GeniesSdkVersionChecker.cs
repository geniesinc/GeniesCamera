using System.IO;
using UnityEditor;
using UnityEngine;

namespace Genies.SdkBootstrap.Editor
{
    /// <summary>
    /// Handles SDK version tracking and detects when the SDK is installed or updated during an active session.
    /// </summary>
    internal static class GeniesSdkVersionChecker
    {
        private const string SessionStartSdkVersionKey = "Genies.SdkBootstrap.Editor.SessionStartSdkVersion";
        private const string SessionInitializedKey = "Genies.SdkBootstrap.Editor.SessionInitialized";

        /// <summary>
        /// Information about SDK version changes during the session.
        /// </summary>
        internal class SdkVersionChangeInfo
        {
            public bool HasChanged { get; set; }
            public bool IsFreshInstall { get; set; }
            public string PreviousVersion { get; set; }
            public string CurrentVersion { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Initializes the session SDK version tracking on startup.
        /// Only runs once per Unity session (not on every domain reload).
        /// Stores the current SDK version without prompting for restart.
        /// </summary>
        public static bool InitializeSessionSdkVersion()
        {
            // Check if we've already initialized this session
            // SessionState persists across domain reloads but is cleared when Unity closes
            if (SessionState.GetBool(SessionInitializedKey, false))
            {
                // Already initialized this Unity session, skip to avoid resetting the baseline
                return false;
            }

            // Mark this session as initialized
            SessionState.SetBool(SessionInitializedKey, true);

            // On session start, just store the current version without prompting
            // This allows us to detect changes that happen DURING this session
            string currentVersion = GetInstalledSdkVersion();
            if (!string.IsNullOrWhiteSpace(currentVersion))
            {
                SessionState.SetString(SessionStartSdkVersionKey, currentVersion);
            }

            return true;
        }

        /// <summary>
        /// Checks if the SDK version has changed or was installed during this session.
        /// Returns information about the version change without prompting the user.
        /// </summary>
        /// <returns>Information about SDK version changes, or null if no change detected.</returns>
        public static SdkVersionChangeInfo CheckForSdkVersionChange()
        {
            // Get the version when this session started
            string sessionStartVersion = SessionState.GetString(SessionStartSdkVersionKey, "");

            // Check if SDK is currently installed
            if (!GeniesSdkPrerequisiteChecker.IsSdkInstalled())
            {
                // SDK is not installed - check if it was uninstalled during this session
                if (!string.IsNullOrWhiteSpace(sessionStartVersion))
                {
                    // SDK was installed at session start but is now gone - clear the session version
                    UpdateSessionSdkVersion("");
                }
                return null;
            }

            // Get the current SDK version
            string currentVersion = GetInstalledSdkVersion();
            if (string.IsNullOrWhiteSpace(currentVersion))
            {
                return null;
            }

            // If version hasn't changed during this session, no restart needed
            if (!string.IsNullOrWhiteSpace(sessionStartVersion) && sessionStartVersion == currentVersion)
            {
                return null;
            }

            // Determine the message based on whether this is a fresh install or version change
            bool isFreshInstall = string.IsNullOrWhiteSpace(sessionStartVersion);
            string message;
            string title;

            if (isFreshInstall)
            {
                // SDK was installed during this session
                title = "SDK Installed";
                message = $"The Genies SDK (version {currentVersion}) has been installed.\n\n" +
                          "Unity Editor should be restarted for the SDK to function properly.";
                Debug.LogWarning($"[Genies SDK Bootstrap] SDK version {currentVersion} was installed during this session. Unity Editor restart recommended.");
            }
            else
            {
                // SDK version changed during this session
                title = "SDK Version Changed";
                message = $"The Genies SDK version has changed:\n\n" +
                          $"Previous: {sessionStartVersion}\n" +
                          $"Current: {currentVersion}\n\n" +
                          "Unity Editor should be restarted for the changes to take full effect.";
                Debug.LogWarning($"[Genies SDK Bootstrap] SDK version changed from {sessionStartVersion} to {currentVersion} during this session. Unity Editor restart recommended.");
            }

            // Update the session version baseline to reflect the change
            UpdateSessionSdkVersion(currentVersion);

            return new SdkVersionChangeInfo
            {
                HasChanged = true,
                IsFreshInstall = isFreshInstall,
                PreviousVersion = sessionStartVersion,
                CurrentVersion = currentVersion,
                Title = title,
                Message = message
            };
        }

        /// <summary>
        /// Updates the session SDK version baseline.
        /// Used after SDK installation or when user chooses to restart later.
        /// </summary>
        public static void UpdateSessionSdkVersion(string version)
        {
            SessionState.SetString(SessionStartSdkVersionKey, version);
        }

        /// <summary>
        /// Reads and returns the installed SDK version from the package.json file.
        /// </summary>
        /// <returns>The SDK version string, or null if unable to read.</returns>
        private static string GetInstalledSdkVersion()
        {
            try
            {
                string packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
                string packageJsonPath = $"Packages/{packageName}/package.json";

                // Use AssetDatabase to resolve the path
                string fullPath = Path.GetFullPath(packageJsonPath);

                if (!File.Exists(fullPath))
                {
                    return null;
                }

                string jsonContent = File.ReadAllText(fullPath);

                // Simple JSON parsing to extract version
                // Looking for: "version": "x.y.z"
                var versionMatch = System.Text.RegularExpressions.Regex.Match(
                    jsonContent,
                    @"""version""\s*:\s*""([^""]+)""");

                if (versionMatch.Success && versionMatch.Groups.Count > 1)
                {
                    return versionMatch.Groups[1].Value;
                }

                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Genies SDK Bootstrap] Failed to read SDK version: {e.Message}");
                return null;
            }
        }
    }
}

