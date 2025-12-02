using System;
using System.Text.RegularExpressions;

namespace Genies.Login.AuthMessages
{
    [Serializable]
    public class GeniesAuthSignInV2Response : GeniesAuthMessage
    {
        public enum StatusCode
        {
            None,
            ClientNotInitialized,
            EmptyResponse,
            SignInPending,
            SignInSuccess,
            ApiException,
            InvalidEmailOrPassword,
            StdException,
        }

        public StatusCode ResponseStatusCode = StatusCode.None;

        public override void OnAfterDeserialize()
        {
            if (!Enum.TryParse(StatusCodeString, out StatusCode parsed))
            {
                parsed = StatusCode.None;
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                try
                {
                    var match = Regex.Match(
                        ErrorMessage,
                        @"desc\s*=\s*(?<desc>[^""]+)",
                        RegexOptions.IgnoreCase
                    );

                    if (match.Success)
                    {
                        string desc = match.Groups["desc"].Value.Trim();
                        if (desc.IndexOf("invalid email or password", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            parsed = StatusCode.InvalidEmailOrPassword;
                            StatusCodeString = nameof(StatusCode.InvalidEmailOrPassword);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to parse ErrorMessage: {ex.Message}");
                }
            }

            ResponseStatusCode = parsed;
        }
    }
}