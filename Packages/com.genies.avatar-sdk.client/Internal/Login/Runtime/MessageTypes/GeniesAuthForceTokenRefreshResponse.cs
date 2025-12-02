using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for forced token refresh operations.
    /// Contains status information about attempts to refresh authentication tokens to extend the user session.
    /// </summary>
    [Serializable]
    public class GeniesAuthForceTokenRefreshResponse : GeniesAuthMessage
    {
        /// <summary>The specific status code for the token refresh operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for token refresh operations.</summary>
        public enum StatusCode
        {
            None,
            NoRefreshToken,
            TokensRefreshed,
            TokenRefreshError
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