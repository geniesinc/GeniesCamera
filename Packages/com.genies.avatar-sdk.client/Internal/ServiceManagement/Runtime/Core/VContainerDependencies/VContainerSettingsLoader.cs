using System.IO;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using VContainer.Unity;

namespace Genies.ServiceManagement
{

    /// <summary>
    /// Handles loading the <see cref="VContainerSettings"/> object which can be used to configure
    /// diagnostics and other useful settings for the VContainer framework.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class VContainerSettingsLoader
    {
        private static VContainerSettings _vContainerSettings;

        private static string SettingsResourcesPath
        {
            get
            {
                return "VContainerSettings";
            }
        }

        private static VContainerSettings VContainerSettings
        {
            get
            {
                if (_vContainerSettings == null)
                {
                    _vContainerSettings = Resources.Load<VContainerSettings>(SettingsResourcesPath);
                }
                return _vContainerSettings;
            }
            set => _vContainerSettings = value;
        }
#if UNITY_EDITOR
        private static string _SettingsEditorPath
        {
            get
            {
                return Path.Combine($"{ServiceManagerPathsHelper.EditorResourcesPath}", SettingsResourcesPath);
            }
        }
#endif

        static VContainerSettingsLoader()
        {
#if UNITY_EDITOR
            // Wait for Unity Editor to finish importing assets
            EditorApplication.delayCall += () =>
            {
                if (VContainerSettings == null)
                {
                    VContainerSettings = ScriptableObject.CreateInstance<VContainerSettings>();
                    CallOnEnable(VContainerSettings);
                    AssetDatabase.CreateAsset(VContainerSettings, _SettingsEditorPath + ".asset");
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    CallOnEnable(VContainerSettings);
                }
            };
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void SetInstance()
        {
            if (VContainerSettings != null)
            {
                CallOnEnable(VContainerSettings);
            }
        }

        private static void CallOnEnable(VContainerSettings instance)
        {
            MethodInfo onEnableMethod = instance.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onEnableMethod != null)
            {
                onEnableMethod.Invoke(instance, null);
            }
            else
            {
                Debug.LogError("OnEnable method not found!");
            }
        }
    }
}
