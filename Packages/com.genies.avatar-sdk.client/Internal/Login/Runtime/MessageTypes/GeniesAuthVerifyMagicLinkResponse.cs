using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
    public class GeniesAuthVerifyMagicLinkResponse : GeniesAuthMessage
    {
        public enum StatusCode
        {
            None,
            VerificationSuccess,
            InvalidCode,
            ExpiredCode,
            VerificationError
        }

        public StatusCode ResponseStatusCode = StatusCode.None;

        public override void OnAfterDeserialize()
        {
            if (!Enum.TryParse(StatusCodeString, out StatusCode parsed))
            {
                parsed = StatusCode.None;
            }

            ResponseStatusCode = parsed;
        }
    }
}