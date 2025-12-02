using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for login system shutdown operations.
    /// Contains status information about the shutdown process and cleanup operations.
    /// </summary>
    [Serializable]
    public class GeniesAuthShutdownResponse : GeniesAuthMessage
    {
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        public enum StatusCode
        {
            None,
            InitializationSuccess,
            ShutdownException,
            ShutdownFailure
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