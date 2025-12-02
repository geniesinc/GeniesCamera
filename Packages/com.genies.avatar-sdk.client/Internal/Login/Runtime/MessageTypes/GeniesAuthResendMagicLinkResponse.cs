using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
    public class GeniesAuthResendMagicLinkResponse : GeniesAuthMessage
    {
        public enum StatusCode
        {
            None,
            ResentSuccessfully,
            ResendError
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