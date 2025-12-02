using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for cached credentials clearing operations.
    /// Contains status information about the process of removing stored authentication data from local storage.
    /// </summary>
    [Serializable]
    public class GeniesAuthClearCachedCredentialsResponse : GeniesAuthMessage
    {
        /// <summary>The specific status code for the credential clearing operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for credential clearing operations.</summary>
        public enum StatusCode
        {
            /// <summary>No specific status or default state.</summary>
            None,
            /// <summary>Cached credentials were successfully cleared from local storage.</summary>
            CredentialsCleared,
            /// <summary>An exception occurred while clearing cached credentials.</summary>
            CredentialsException,
            /// <summary>An error occurred during the credential clearing process.</summary>
            CredentialsError
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!Enum.TryParse(StatusCodeString, true, out ResponseStatusCode))
            {
                ResponseStatusCode = StatusCode.None;
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}, statusCode: {ResponseStatusCode}, statusCodeString: {StatusCodeString}";
        }
    }
}