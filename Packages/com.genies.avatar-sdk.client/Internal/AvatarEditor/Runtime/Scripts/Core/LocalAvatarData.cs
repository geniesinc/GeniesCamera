using Genies.AvatarEditor;
using Genies.CrashReporting;
using Genies.Naf;
using System;
using System.Collections.Generic;
using System.Linq;
using GnWrappers;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AvatarEditor.Core
{
    /// <summary>
    /// Serializable wrapper for Dictionary<string, Color> to work with Unity's serialization
    /// </summary>
    [Serializable]
    public class SerializableColorDictionary
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<Color> values = new List<Color>();

        public Dictionary<string, Color> ToDictionary()
        {
            var dict = new Dictionary<string, Color>();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                if (!string.IsNullOrEmpty(keys[i]))
                {
                    dict[keys[i]] = values[i];
                }
            }
            return dict;
        }

        public void FromDictionary(Dictionary<string, Color> dict)
        {
            keys.Clear();
            values.Clear();

            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Serializable wrapper for Dictionary<string, float> to work with Unity's serialization
    /// </summary>
    [Serializable]
    public class SerializableFloatDictionary
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<float> values = new List<float>();

        public Dictionary<string, float> ToDictionary()
        {
            var dict = new Dictionary<string, float>();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                if (!string.IsNullOrEmpty(keys[i]))
                {
                    dict[keys[i]] = values[i];
                }
            }
            return dict;
        }

        public void FromDictionary(Dictionary<string, float> dict)
        {
            keys.Clear();
            values.Clear();

            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Serializable wrapper for Dictionary<MegaSkinTattooSlot, string> to work with Unity's serialization
    /// </summary>
    [Serializable]
    public class SerializableTattooDictionary
    {
        [SerializeField] private List<int> keys = new List<int>(); // Store enum as int
        [SerializeField] private List<string> values = new List<string>();

        public Dictionary<MegaSkinTattooSlot, string> ToDictionary()
        {
            var dict = new Dictionary<MegaSkinTattooSlot, string>();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                if (Enum.IsDefined(typeof(MegaSkinTattooSlot), keys[i]))
                {
                    dict[(MegaSkinTattooSlot)keys[i]] = values[i];
                }
            }
            return dict;
        }

        public void FromDictionary(Dictionary<MegaSkinTattooSlot, string> dict)
        {
            keys.Clear();
            values.Clear();

            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    keys.Add((int)kvp.Key);
                    values.Add(kvp.Value);
                }
            }
        }
    }

#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "LocalAvatarData", menuName = "Genies/LocalAvatarData")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class LocalAvatarData : ScriptableObject, ISerializationCallbackReceiver
#else
    public class LocalAvatarData : ScriptableObject, ISerializationCallbackReceiver
#endif
    {
        // Core avatar data
        [SerializeField] private List<string> equippedAssetIds = new List<string>();
        [SerializeField] private SerializableColorDictionary serializedColors = new SerializableColorDictionary();
        [SerializeField] private SerializableFloatDictionary serializedBodyAttributes = new SerializableFloatDictionary();
        [SerializeField] private SerializableTattooDictionary serializedTattooIds = new SerializableTattooDictionary();

        // Legacy field for backward compatibility - will be populated from serialized data
        [System.NonSerialized] public AvatarDefinition Definition;

        public Texture2D HeadshotTexture;
        [TextArea] public string HeadshotPath;

        /// <summary>
        /// Ensures the serializable wrapper objects are initialized. Call this when creating new instances.
        /// </summary>
        private void EnsureInitialized()
        {
            if (serializedColors == null)
            {
                serializedColors = new SerializableColorDictionary();
            }

            if (serializedBodyAttributes == null)
            {
                serializedBodyAttributes = new SerializableFloatDictionary();
            }

            if (serializedTattooIds == null)
            {
                serializedTattooIds = new SerializableTattooDictionary();
            }

            if (equippedAssetIds == null)
            {
                equippedAssetIds = new List<string>();
            }
        }

        public AvatarProfileData ToData()
        {
            // Reconstruct AvatarDefinition from serialized data
            var avatarDefinition = new AvatarDefinition
            {
                equippedAssetIds = new List<string>(equippedAssetIds),
                colors = serializedColors.ToDictionary(),
                bodyAttributes = serializedBodyAttributes.ToDictionary(),
                equippedTattooIds = serializedTattooIds.ToDictionary()
            };

            // Update legacy field for backward compatibility
            Definition = avatarDefinition;

            return new AvatarProfileData {
                Definition   = avatarDefinition,
                HeadshotPath = this.HeadshotPath,
            };
        }
        public void Apply(AvatarProfileData data)
        {
            if (data?.Definition == null)
            {
                CrashReporter.LogError("[LocalAvatarData.Apply] Avatar definition is null!");
                return;
            }

            // Ensure wrapper objects are initialized
            EnsureInitialized();

            // Store in legacy field for backward compatibility
            Definition = data.Definition;
            HeadshotPath = data.HeadshotPath;

            // Convert and store in Unity-serializable format
            var definition = data.Definition;

            // Store equipped asset IDs
            equippedAssetIds.Clear();
            if (definition.equippedAssetIds != null)
            {
                equippedAssetIds.AddRange(definition.equippedAssetIds);
            }

            // Store colors using serializable wrapper
            serializedColors.FromDictionary(definition.colors);

            // Store body attributes using serializable wrapper
            serializedBodyAttributes.FromDictionary(definition.bodyAttributes);

            // Store tattoo IDs using serializable wrapper
            serializedTattooIds.FromDictionary(definition.equippedTattooIds);

#if UNITY_EDITOR
            // Load texture as asset reference if it exists
            HeadshotTexture = LoadHeadshotAsAsset(data.HeadshotPath);
            EditorUtility.SetDirty(this);
#endif
        }

        #region ISerializationCallbackReceiver Implementation

        /// <summary>
        /// Called before Unity serializes the object. No special handling needed.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // No special handling needed before serialization
        }

        /// <summary>
        /// Called after Unity deserializes the object. Ensures AvatarDefinition is reconstructed from serialized data.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Ensure wrapper objects are initialized
            EnsureInitialized();

            // Reconstruct the AvatarDefinition from serialized data
            Definition = new AvatarDefinition
            {
                equippedAssetIds = new List<string>(equippedAssetIds),
                colors = serializedColors.ToDictionary(),
                bodyAttributes = serializedBodyAttributes.ToDictionary(),
                equippedTattooIds = serializedTattooIds.ToDictionary()
            };
        }

        #endregion

#if UNITY_EDITOR
        private Texture2D LoadHeadshotAsAsset(string headshotPath)
        {

            if (!System.IO.File.Exists(headshotPath))
            {
                CrashReporter.LogWarning($"Headshot file does not exist at: {headshotPath}");
                return null;
            }

            try
            {
                AssetDatabase.ImportAsset(headshotPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(headshotPath);
                return texture;
            }
            catch (Exception ex)
            {
                CrashReporter.LogWarning($"Failed to load headshot texture from {headshotPath}: {ex.Message}");
                return null;
            }
        }
#endif
    }
}
