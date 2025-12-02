using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
    public class GeniesAuthUpgradeV1Response : GeniesAuthMessage
    {
        public enum StatusCode
        {
            None,
            UpgradeSuccess,
            ApiException,
            StdException,
            EmptyResponse,
            ClientNotInitialized,
            UpgradeFailed,
        }

        public StatusCode ResponseStatusCode = StatusCode.None;

        public override void OnAfterDeserialize()
        {
            if (!Enum.TryParse(StatusCodeString, out StatusCode parsed))
            {
                parsed = StatusCode.None;
            }

            if ((parsed == StatusCode.ApiException || parsed == StatusCode.None) &&
                !string.IsNullOrEmpty(ErrorMessage))
            {
                if (Contains(ErrorMessage, "upgradeuserv1") ||
                    Contains(ErrorMessage, "failed to create workos user"))
                {
                    parsed = StatusCode.UpgradeFailed;
                    StatusCodeString = nameof(StatusCode.UpgradeFailed);
                }
            }

            ResponseStatusCode = parsed;
        }

        private static bool Contains(string s, string needle) =>
            s?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}