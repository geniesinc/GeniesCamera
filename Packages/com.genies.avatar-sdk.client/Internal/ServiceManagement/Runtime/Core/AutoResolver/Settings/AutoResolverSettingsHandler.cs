#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Genies.ServiceManagement
{
    /// <summary>
    /// Class for handling the <see cref="GlobalAutoResolverSettings"/> loading and saving
    /// </summary>
    internal partial class AutoResolver
    {
        public static AutoResolverSettings GetGlobalSettings()
        {
            // Load settings from Resources
            if (GlobalAutoResolverSettings == null)
            {
                GlobalAutoResolverSettings = ScriptableObject.CreateInstance<AutoResolverSettings>();
                AssetDatabase.CreateAsset(GlobalAutoResolverSettings, Path.Combine($"{ServiceManagerPathsHelper.EditorResourcesPath}", _autoResolverSettingsKey + ".asset"));
                AssetDatabase.SaveAssets();
            }

            UpdateSettings(GlobalAutoResolverSettings);

            return GlobalAutoResolverSettings;
        }

        public static void UpdateSettings(AutoResolverSettings settingsObj)
        {
            var allResolverTypes = GetTypeInformationCollection().ToList();

            var existingResolverSettings = settingsObj.ResolverSettingsList.Select(rs => rs.ResolverTypeName).ToList();

            // Remove entries that don't exist anymore
            for (var i = settingsObj.ResolverSettingsList.Count - 1; i >= 0; i--)
            {
                if (allResolverTypes.All(rt => rt.Type != settingsObj.ResolverSettingsList[i].ResolverType))
                {
                    settingsObj.ResolverSettingsList.RemoveAt(i);
                }
            }

            // Add new entries
            foreach (var resolverTypeInfo in allResolverTypes)
            {
                var resolverTypeFullName = resolverTypeInfo.Type.FullName;
                if (!existingResolverSettings.Contains(resolverTypeFullName))
                {
                    var resolverSetting = new ResolverSettings
                    {
                        IsInstallerEnabled = resolverTypeInfo.IsInstaller,
                        IsInitializerEnabled = resolverTypeInfo.IsInitializer,
                        ResolverInstance = GetOrCreateInstance(resolverTypeInfo.Type),
                    };
                    settingsObj.ResolverSettingsList.Add(resolverSetting);
                }
            }

            EditorUtility.SetDirty(settingsObj);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
