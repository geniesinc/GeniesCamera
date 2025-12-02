using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for user attribute operations including retrieval and updates.
    /// Contains status information about attribute-related operations.
    /// </summary>
    [Serializable]
    public class GeniesAuthAttributeResponse : GeniesAuthMessage
    {
        /// <summary>
        /// The specific status code for the attribute operation.
        /// </summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>
        /// Defines the possible status codes for user attribute operations.
        /// </summary>
        public enum StatusCode
        {
            /// <summary>No specific status or default state.</summary>
            None,
            /// <summary>Failed to retrieve user attributes.</summary>
            UserAttributesRetrievalFailed,
            /// <summary>Failed to update user attributes.</summary>
            UserAttributesUpdateFailed,
            /// <summary>User attributes were updated successfully.</summary>
            UserAttributesUpdateSuccess,
            /// <summary>User attributes were retrieved successfully.</summary>
            UserAttributesRetrievalSuccess
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
            return $"{base.ToString()}, " +
                   $"statusCode: {ResponseStatusCode}, " +
                   $"statusCodeString: {StatusCodeString}";
        }
    }
}
