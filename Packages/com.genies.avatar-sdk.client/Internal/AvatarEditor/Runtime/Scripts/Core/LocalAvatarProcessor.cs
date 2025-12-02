using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AvatarEditor.Core;
using Genies.CrashReporting;
using Genies.Naf;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Genies.AvatarEditor.Core
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class LocalAvatarProcessor
#else
    public static class LocalAvatarProcessor
#endif
    {
        public const string TemplateName = "Template";
        public const string TemplatePath = "Assets/Genies/AvatarEditor/Profiles/Resources";
        public const string HeadshotPath = "Assets/Genies/AvatarEditor/Headshots";

        public static string NewTemplateName { get; set; }
        public static string NewTemplatePath { get; set; }

        /// <summary>
        /// Saves or updates the profile identified by <paramref name="profileId"/>.
        /// At runtime: writes JSON to <see cref="GetRuntimePath"/>.
        /// In Editor: always writes JSON; optionally mirrors to a .asset when assetPath is provided.
        /// </summary>
        public static void SaveOrUpdate(
            string profileId,
            AvatarDefinition definition,
            string headshotPath,
            string assetPathIfEditor = null
        )
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                profileId = TemplateName;
            }

            var data = new AvatarProfileData { Definition = definition, HeadshotPath = headshotPath, };

            // 1) Always persist JSON for cross-platform compatibility
            var jsonPath = GetRuntimePath(profileId); // where JSON lives
            EnsureDirectory(jsonPath);

            var jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            AtomicWriteJson(jsonPath, jsonString);

#if UNITY_EDITOR
            // 2) (Optional) Mirror to a ScriptableObject asset for Editor workflows if requested

            if (string.IsNullOrEmpty(assetPathIfEditor))
            {
                assetPathIfEditor = $"{TemplatePath}/{profileId}.asset";
            }

            EnsureInResources(assetPathIfEditor, out var finalPath, out var resourceKey);

            SaveScriptableObject(finalPath, data, profileId);
#endif
        }

        /// <param name="resourcesPath">Path under Resources without extension, e.g. "Genies/AvatarEditor/Profiles/Alex"</param>
        public static AvatarProfileData LoadFromResources(string profileId, string resourcesPath = null)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                // Convert asset path to Resources path: "Assets/Genies/AvatarEditor/Profiles/Resources" -> "Genies/AvatarEditor/Profiles"
                resourcesPath = profileId;
            }


            var so = Resources.Load<LocalAvatarData>(resourcesPath);
            if (so == null)
            {
                return null;
            }

            return so.ToData();
        }

        /// <summary>
        /// Load the profile by id. Returns null if missing.
        /// Always reads from JSON (works in builds + Editor).
        /// </summary>
        public static AvatarProfileData LoadFromJson(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return null;
            }

            var jsonPath = GetRuntimePath(profileId);
            if (!File.Exists(jsonPath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(jsonPath);
                var data = JsonConvert.DeserializeObject<AvatarProfileData>(json);
                return data;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to load AvatarProfileData at {jsonPath}: {e}");
                return null;
            }
        }

        /// <summary>
        /// Returns the JSON path used for cross-platform storage.
        /// Customize if you prefer another base folder (e.g., working dir).
        /// </summary>
        public static string GetRuntimePath(string profileId, bool useDataPath = true)
        {
            string baseDir = null;
            if (useDataPath)
            {
                // Option A: persistentDataPath (survives updates; platform-safe)
                baseDir = Application.persistentDataPath;
            }
            else
            {
                // Option B: working directory
                baseDir = Directory.GetCurrentDirectory();
            }

            return Path.Combine(baseDir, "AvatarProfiles", $"{Sanitize(profileId)}.json");
        }

        // ---------- helpers ----------

        /// <summary>
        /// Ensures the asset is saved under a Resources folder and returns both:
        /// - finalAssetPath  => "Assets/.../Resources/.../MyAvatar.asset"
        /// - resourcesKey    => ".../MyAvatar" (for Resources.Load&lt;T&gt;(key))
        /// </summary>
        private static void EnsureInResources(
            string desiredAssetPath,
            out string finalAssetPath,
            out string resourcesKey)
        {
            // If caller didn’t target a Resources folder, redirect it:
            var dir = Path.GetDirectoryName(desiredAssetPath)?.Replace('\\', '/');
            var file = Path.GetFileName(desiredAssetPath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file))
            {
                throw new System.ArgumentException($"Invalid asset path: {desiredAssetPath}");
            }

            if (!dir.EndsWith("/Resources"))
            {
                dir = $"{dir}/Resources";
            }

            finalAssetPath = $"{dir}/{file}";
            EnsureDirectory(finalAssetPath);

            // Compute Resources key (part after ".../Resources/")
            resourcesKey = GetResourcesKeyFromAssetPath(finalAssetPath);
        }

        public static string GetResourcesKeyFromAssetPath(string assetPath)
        {
            // assetPath: "Assets/.../Resources/Sub/Name.asset" -> "Sub/Name"
            var normalized = assetPath.Replace('\\', '/');
            var i = normalized.LastIndexOf("/Resources/");
            if (i < 0)
            {
                return null;
            }

            var after = normalized.Substring(i + "/Resources/".Length);
            return Path.ChangeExtension(after, null); // strip extension
        }

        private static void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static void AtomicWriteJson(string path, string json)
        {
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path))
            {
                // Replace ensures atomicity on most platforms
                File.Replace(tmp, path, null);
            }
            else
            {
                File.Move(tmp, path);
            }
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            return s.Trim();
        }

#if UNITY_EDITOR
        private static void SaveScriptableObject(string finalPath, AvatarProfileData data, string profileId)
        {
            try
            {
                AssetDatabase.Refresh();

                var existing = AssetDatabase.LoadAssetAtPath<LocalAvatarData>(finalPath);

                if (existing == null)
                {
                    // Create new asset
                    var created = ScriptableObject.CreateInstance<LocalAvatarData>();
                    created.Apply(data);

                    AssetDatabase.CreateAsset(created, finalPath);
                    EditorUtility.SetDirty(created);

                    // Force immediate write and import
                    AssetDatabase.SaveAssetIfDirty(created);
                    AssetDatabase.ImportAsset(finalPath, ImportAssetOptions.ForceUpdate);
                }
                else
                {
                    // Update existing asset
                    Undo.RecordObject(existing, "Update Avatar Profile SO");
                    existing.Apply(data);
                    EditorUtility.SetDirty(existing);

                    // Force immediate write and import
                    AssetDatabase.SaveAssetIfDirty(existing);
                    AssetDatabase.ImportAsset(finalPath, ImportAssetOptions.ForceUpdate);
                }

            }
            catch (Exception ex)
            {
                CrashReporter.LogWarning($"[LocalAvatarProcessor] Asset save attempt failed for {profileId}: {ex.Message}");
            }
        }
#endif
    }
}
