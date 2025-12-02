using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for OTP (One-Time Password) refresh/resend operations.
    /// Contains status information about attempts to resend verification codes to users.
    /// </summary>
    [Serializable]
    public class GeniesAuthOtpRefreshResponse : GeniesAuthMessage
    {
        /// <summary>The specific status code for the OTP refresh operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for OTP refresh operations.</summary>
        public enum StatusCode
        {
            None,
            NoActiveSession,
            NoPhoneNumber,
            NoUserID,
            PhoneMismatch,
            OtpRequestSuccess,
            OtpRequestError,
            OtpRequestException,
            OtpRequestFailed
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