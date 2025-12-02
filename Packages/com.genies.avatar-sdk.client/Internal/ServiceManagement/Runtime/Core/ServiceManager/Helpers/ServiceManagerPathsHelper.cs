#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;
using Path = System.IO.Path;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Helper for getting paths to service manager generated assets.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class ServiceManagerPathsHelper
    {
        private static readonly string LegacyBasePath = Path.Combine("Assets", "Genies_ServiceManager");
        private static readonly string CurrentBasePath = Path.Combine("Assets", "Genies", "ServiceManagement");

#if UNITY_EDITOR
        private static bool _migrationCompleted;
#endif

        public static string EditorResourcesPath
        {
            get
            {
                var path = Path.Combine(GetOrCreateServiceManagerAssetsPath(), "Resources");
#if UNITY_EDITOR
                GeneratePath(path);
#endif

                return path;
            }
        }

#if UNITY_EDITOR
        static ServiceManagerPathsHelper()
        {
            // Ensure backwards compatibility for projects that are using the legacy path
            MigrateLegacyPathIfNeeded();
        }
#endif

        public static string GetOrCreateServiceManagerAssetsPath()
        {
            var path = CurrentBasePath;

#if UNITY_EDITOR
            MigrateLegacyPathIfNeeded();
            GeneratePath(path);
#endif

            return path;
        }

#if UNITY_EDITOR
        private static void MigrateLegacyPathIfNeeded()
        {
            if (_migrationCompleted)
            {
                return;
            }

            var legacyPath = LegacyBasePath;
            var currentPath = CurrentBasePath;

            if (Directory.Exists(legacyPath) && !legacyPath.Equals(currentPath))
            {
                Debug.Log($"[ServiceManager] Migrating assets from '{LegacyBasePath}' to '{CurrentBasePath}'...");

                // Ensure target directory hierarchy exists in AssetDatabase
                EnsureFolderExists(currentPath);

                // Move all assets from legacy path to new path
                MoveDirectoryContents(legacyPath, currentPath);

                // Clean up empty directories and delete the legacy directory
                DeleteEmptyDirectoriesRecursive(legacyPath);

                if (Directory.Exists(legacyPath))
                {
                    AssetDatabase.DeleteAsset(legacyPath);
                }

                AssetDatabase.Refresh();
                Debug.Log($"[ServiceManager] Migration completed successfully.");
            }

            _migrationCompleted = true;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = Path.Combine(currentPath, parts[i]);

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }

        private static void DeleteEmptyDirectoriesRecursive(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            // First, recursively delete all empty subdirectories
            foreach (var subdirectory in Directory.GetDirectories(directoryPath))
            {
                DeleteEmptyDirectoriesRecursive(subdirectory);
            }

            // Then check if this directory is now empty and delete it
            var entries = Directory.GetFileSystemEntries(directoryPath);
            if (entries.Length == 0)
            {
                AssetDatabase.DeleteAsset(directoryPath);
            }
        }

        private static void MoveDirectoryContents(string sourcePath, string targetPath)
        {
            // Ensure target folder exists in AssetDatabase
            EnsureFolderExists(targetPath);

            // Move all files
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(file);

                // Skip .meta files, they'll be handled by Unity
                if (fileName.EndsWith(".meta"))
                {
                    continue;
                }

                var targetFile = Path.Combine(targetPath, fileName);

                if (File.Exists(targetFile))
                {
                    Debug.LogWarning($"[ServiceManager] File already exists at target: {targetFile}. Skipping.");
                    continue;
                }

                var sourceAssetPath = file.Replace(Path.GetFullPath("Assets"), "Assets").Replace("\\", "/");
                var targetAssetPath = targetFile.Replace(Path.GetFullPath("Assets"), "Assets").Replace("\\", "/");

                var moveResult = AssetDatabase.MoveAsset(sourceAssetPath, targetAssetPath);
                if (!string.IsNullOrWhiteSpace(moveResult))
                {
                    Debug.LogError($"[ServiceManager] Failed to move asset: {moveResult}");
                }
            }

            // Move all subdirectories
            foreach (var directory in Directory.GetDirectories(sourcePath))
            {
                var dirName = Path.GetFileName(directory);
                var targetDir = Path.Combine(targetPath, dirName);
                MoveDirectoryContents(directory, targetDir);
            }
        }
#endif

        private static void GeneratePath(string path)
        {
#if UNITY_EDITOR
            path = Path.GetFullPath(path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
