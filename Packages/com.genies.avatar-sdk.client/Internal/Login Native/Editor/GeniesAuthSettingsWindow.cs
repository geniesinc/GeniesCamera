#if UNITY_EDITOR && !GENIES_EXPERIENCE_SDK
using System.IO;
using System.Text;
using Genies.Login.Native.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Genies.Login.Native.Editor
{
    /// <summary>
    /// Editor-only persistent state using ScriptableSingleton.
    /// This stores the in-editor values and survives domain reloads / editor restarts.
    /// </summary>
    [FilePath("UserSettings/GeniesSettingsEditorState.asset", FilePathAttribute.Location.ProjectFolder)]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesSettingsEditorState : ScriptableSingleton<GeniesSettingsEditorState>
#else
    public class GeniesSettingsEditorState : ScriptableSingleton<GeniesSettingsEditorState>
#endif
    {
        public string ClientId = "";
        public string ClientSecret = "";

        private void OnEnable()
        {
            if (ClientId == null) ClientId = "";
            if (ClientSecret == null) ClientSecret = "";
        }

        public void SaveState() => Save(true);
    }

    /// <summary>
    /// Project Settings window for editing and saving Genies API credentials.
    /// Writes JSON bytes to Assets/Resources/GeniesAuthSettings.bytes (runtime-loadable).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthSettingsWindow : SettingsProvider
#else
    public class GeniesAuthSettingsWindow : SettingsProvider
#endif
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string JsonBaseName = "GeniesAuthSettings";       // Resources.Load("GeniesAuthSettings")
        private const string JsonBytesFile = JsonBaseName + ".bytes";   // on disk

        private static readonly GUIContent ClientIdLabel = new GUIContent("Client ID");
        private static readonly GUIContent ClientSecretLabel = new GUIContent("Client Secret");

        private SerializedObject _stateSO;
        private SerializedProperty _clientIdProp;
        private SerializedProperty _clientSecretProp;

        public GeniesAuthSettingsWindow(string path, SettingsScope scope) : base(path, scope) {}

        public static SettingsProvider CreateProvider()
        {
            var provider = new GeniesAuthSettingsWindow("Project/Genies/Auth Settings", SettingsScope.Project);
            provider.keywords = new System.Collections.Generic.HashSet<string>(new[] { "Genies", "Client", "Secret", "ID", "OAuth" });
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var state = GeniesSettingsEditorState.instance; // ensure load/create
            _stateSO = new SerializedObject(state);
            _clientIdProp = _stateSO.FindProperty("ClientId");
            _clientSecretProp = _stateSO.FindProperty("ClientSecret");
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Genies – API Credentials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter your Client ID and Client Secret. Click 'Save JSON to Resources' to write a runtime-loadable JSON blob at Assets/Resources/GeniesAuthSettings.bytes.",
                MessageType.Info
            );

            _stateSO.Update();

            EditorGUILayout.PropertyField(_clientIdProp, ClientIdLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(ClientSecretLabel);
                _clientSecretProp.stringValue = EditorGUILayout.PasswordField(_clientSecretProp.stringValue);
            }

            _stateSO.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save JSON to Resources", GUILayout.Height(24)))
                {
                    SaveJsonToResources(_clientIdProp.stringValue, _clientSecretProp.stringValue);
                    GeniesSettingsEditorState.instance.SaveState();
                    EditorGUILayout.HelpBox("Saved GeniesAuthSettings.bytes to Resources.", MessageType.None);
                }

                if (GUILayout.Button("Ping JSON", GUILayout.Height(24)))
                {
                    var path = Path.Combine(ResourcesDir, JsonBytesFile).Replace("\\", "/");
                    var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Genies Settings",
                            "No JSON file found yet. Click 'Save JSON to Resources' first.", "OK");
                    }
                }
            }
        }

        private static void SaveJsonToResources(string clientId, string clientSecret)
        {
            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
            }

            var data = new GeniesAuthSettings { ClientId = clientId, ClientSecret = clientSecret };
            string json = JsonUtility.ToJson(data, prettyPrint: false);

            // Convert to bytes
            byte[] rawBytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Optionally obfuscate
            byte key = 0x5A; // simple XOR key
            for (int i = 0; i < rawBytes.Length; i++)
            {
                rawBytes[i] ^= key;
            }

            var path = Path.Combine(ResourcesDir, JsonBytesFile).Replace("\\", "/");
            File.WriteAllBytes(path, rawBytes);

            AssetDatabase.ImportAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        [SettingsProvider]
        public static SettingsProvider Register() => CreateProvider();

        [MenuItem("Tools/Genies/Settings/Auth Settings")]
        private static void OpenWindow()
        {
            SettingsService.OpenProjectSettings("Project/Genies/Auth Settings");
        }
    }
}
#endif
