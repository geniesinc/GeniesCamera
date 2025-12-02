namespace Genies.ServiceManagement.Editor
{
    using UnityEditor;

    /// <summary>
    /// Window used for configuring <see cref="AutoResolverSettings"/> object.
    /// </summary>
    public class AutoResolverSettingsWindow : EditorWindow
    {
        private AutoResolverSettings _autoResolverSettings;
        private Editor _autoResolverSettingsEditor;

#if GENIES_INTERNAL
        [MenuItem("Genies/ServiceManager/AutoResolverSettings")]
#endif
        public static void ShowWindow()
        {
            GetWindow<AutoResolverSettingsWindow>("Auto Resolver Settings").Initialize();
        }

        private void Initialize()
        {
            _autoResolverSettings = AutoResolver.GetGlobalSettings();
            _autoResolverSettingsEditor = Editor.CreateEditor(_autoResolverSettings, typeof(AutoResolverSettingsEditor));
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnGUI()
        {
            if (_autoResolverSettings != null)
            {
                if (_autoResolverSettingsEditor?.target != _autoResolverSettings)
                {
                    _autoResolverSettingsEditor = Editor.CreateEditor(_autoResolverSettings);
                }

                _autoResolverSettingsEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.LabelField("Auto Resolver Settings not found.");
            }
        }
    }
}
