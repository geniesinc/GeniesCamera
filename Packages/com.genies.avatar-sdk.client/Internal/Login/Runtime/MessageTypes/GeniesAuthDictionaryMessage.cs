using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for operations that return key-value dictionary data.
    /// This class provides functionality to convert comma-separated key and value strings into a dictionary format.
    /// </summary>
    [Serializable]
    public class GeniesAuthDictionaryMessage : GeniesAuthMessage
    {
        /// <summary>The specific status code for the dictionary operation.</summary>
        [NonSerialized]
        public StatusCode MessageStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for dictionary-based operations.</summary>
        public enum StatusCode
        {
            None,
            UserAttributesRetrievalFailed,
            UserAttributesUpdateFailed,
            UserAttributesUpdateSuccess,
            UserAttributesRetrievalSuccess
        }

        /// <summary>Comma-separated string containing dictionary keys.</summary>
        public string Keys;

        /// <summary>Comma-separated string containing dictionary values corresponding to the keys.</summary>
        public string Values;

        /// <summary>
        /// Converts the comma-separated key and value strings into a dictionary.
        /// Handles mismatched key-value counts by using empty strings for missing values.
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            if (string.IsNullOrEmpty(Keys) || string.IsNullOrEmpty(Values))
            {
                Debug.LogError("Keys or values are null or empty.");
                return null;
            }

            var keysList = Keys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var valuesList = Values.Split(new[] { ',' });

            if (keysList.Length != valuesList.Length)
            {
                Debug.LogWarning(
                    $"Key-value count mismatch: keys({keysList.Length}), values({valuesList.Length}). You may be missing some data");
            }

            var dictionary = new Dictionary<string, string>(keysList.Length);
            for (int i = 0; i < keysList.Length; i++)
            {
                var value = (i < valuesList.Length) ? valuesList[i] : string.Empty;
                dictionary[keysList[i]] = value;
            }

            return dictionary;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!Enum.TryParse(StatusCodeString, true, out MessageStatusCode))
            {
                MessageStatusCode = StatusCode.None;
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}, " +
                   $"statusCode: {MessageStatusCode}, " +
                   $"statusCodeString: {StatusCodeString}, " +
                   $"keys: [{Keys}], " +
                   $"values: [{Values}]";
        }
    }
}
