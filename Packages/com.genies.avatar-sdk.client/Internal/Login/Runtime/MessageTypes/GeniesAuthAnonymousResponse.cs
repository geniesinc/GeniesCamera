using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
    public class GeniesAuthAnonymousResponse : GeniesAuthMessage
    {
        public enum StatusCode
        {
            None,
            AnonymousSignUpSuccess,
            AnonymousRefreshSuccess,
            AnonymousUpgradeSuccess,
            AnonymousError
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