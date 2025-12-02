using System;
using UnityEngine;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Base class for all Genies authentication response messages.
    /// Provides common properties for status checking and error handling across all authentication operations.
    /// </summary>
    [Serializable]
    public class GeniesAuthMessage : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Gets a value indicating whether the authentication operation was successful.
        /// </summary>
        public bool IsSuccessful => Status == "success";

        /// <summary>
        /// The status of the authentication operation (e.g., "success", "failed").
        /// </summary>
        public string Status;

        /// <summary>
        /// A descriptive message about the operation result.
        /// </summary>
        public string Message;

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// String representation of the status code for serialization purposes.
        /// </summary>
        public string StatusCodeString = "None";

        /// <summary>
        /// Returns a string representation of the authentication message with key information.
        /// </summary>
        /// <returns>A formatted string containing status, message, and error details.</returns>
        public override string ToString()
        {
            return $"LoginResponse: {{ " +
                $"status: {(string.IsNullOrEmpty(Status) ? "null" : Status)}, " +
                $"reason: {(string.IsNullOrEmpty(Message) ? "null" : Message)}, " +
                $"errorMessage: {(string.IsNullOrEmpty(ErrorMessage) ? "null" : ErrorMessage)} }}";
        }

        /// <summary>
        /// Called before Unity serializes the object. Currently unused but required by <see cref="ISerializationCallbackReceiver"/>.
        /// </summary>
        public virtual void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Called after Unity deserializes the object. Subclasses override this to set their specific status codes from the statusCodeString.
        /// </summary>
        public virtual void OnAfterDeserialize()
        {
        }
    }
}
