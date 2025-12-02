using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for user logout operations.
    /// Contains status information about the logout process and specific status codes for different logout scenarios.
    /// </summary>
    [Serializable]
    public class GeniesAuthLogoutResponse : GeniesAuthMessage
    {
        /// <summary>The specific status code for the logout operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for logout operations.</summary>
        public enum StatusCode
        {
            None,
            LogoutSuccess,
            LogoutError,
            LogoutException
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