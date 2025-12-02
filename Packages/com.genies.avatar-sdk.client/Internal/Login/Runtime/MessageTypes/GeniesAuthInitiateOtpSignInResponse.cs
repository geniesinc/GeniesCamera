using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for OTP (One-Time Password) sign-in initiation operations.
    /// Contains status information about the process of starting phone number-based authentication.
    /// </summary>
    [Serializable]
    public class GeniesAuthInitiateOtpSignInResponse : GeniesAuthMessage
    {
        /// <summary>The specific status code for the OTP initiation operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Indicates whether a new user was signed up during the OTP initiation process.</summary>
        public bool UserSignedUp = false;

        /// <summary>Defines the possible status codes for OTP sign-in initiation operations.</summary>
        public enum StatusCode
        {
            None,
            ValidSession,
            SignUpFailed,
            InitiateAuthError,
            InitiateAuthException,
            InitiateAuthFailed,
            OtpRequestSent,
            OtpRequestError,
            OtpRequestException,
            OtpRequestFailed,
            UserNotConfirmed,
            UserNotFound
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
            return $"{base.ToString()}, statusCode: {ResponseStatusCode}, userSignedUp: {UserSignedUp}, statusCodeString: {StatusCodeString}";
        }
    }
}