using System;

namespace Genies.Looks.Service
{
    /// <summary>
    /// Exception thrown by the Looks service when an error occurs during look-related operations.
    /// This exception provides specific error information for troubleshooting looks functionality.
    /// </summary>
    public class LookServiceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LookServiceException"/> class.
        /// </summary>
        public LookServiceException() { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LookServiceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LookServiceException(string message) : base(message) { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LookServiceException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public LookServiceException(string message, Exception inner) : base(message, inner) { }
    }
}
