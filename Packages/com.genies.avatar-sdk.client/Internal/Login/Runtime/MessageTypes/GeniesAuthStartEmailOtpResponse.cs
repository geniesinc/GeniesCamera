using System;
using System.Text.RegularExpressions;
using Genies.Login;

namespace Genies.Login.AuthMessages
{
    [Serializable]
    public class GeniesAuthStartEmailOtpResponse : GeniesAuthMessage
    {
        public enum StatusCode
        {
            None,
            MagicLinkSent,
            MagicLinkError,
            FailedToFindUser,
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
                    var m = Regex.Match(
                        ErrorMessage,
                        @"desc\s*=\s*(?<desc>[^""}]+)",
                        RegexOptions.IgnoreCase
                    );

                    if (m.Success)
                    {
                        var desc = m.Groups["desc"].Value.Trim();

                        if (desc.IndexOf("failed to get user by email", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            parsed = StatusCode.FailedToFindUser;
                            StatusCodeString = nameof(StatusCode.FailedToFindUser);

                            if (string.IsNullOrEmpty(Message) ||
                                Message.Equals("Failed to start magic auth", StringComparison.OrdinalIgnoreCase))
                            {
                                Message = "Failed to get user by email";
                            }
                        }
                    }
                }
                catch
                {
                    // ignore parse errors
                }
            }

            ResponseStatusCode = parsed;
        }
    }
}