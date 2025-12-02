using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for login system initialization operations.
    /// Contains status information about the initialization process and user sign-up status.
    /// </summary>
    [Serializable]
    public class GeniesAuthInitializationResponse : GeniesAuthMessage
    {
        /// <summary>Indicates whether a new user was signed up during the initialization process.</summary>
        public bool UserSignedUp = false;

        /// <summary>The specific status code for the initialization operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for initialization operations.</summary>
        public enum StatusCode
        {
            None,
            InitializationSuccess,
            InitializationException,
            InitializationFailed,
            ConfigUpdated
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
            return $"{base.ToString()}, statusCode: {ResponseStatusCode}, statusCodeString: {StatusCodeString}, userSignedUp: {UserSignedUp}";
        }
    }
}