using System;
using Newtonsoft.Json;

namespace Genies.Services.Auth
{
    [Serializable]
    public class ErrorResponse
    {
        [JsonProperty("__type")]
        public string Type;

        [JsonProperty("message")]
        public string Message;

        public ErrorResponse()
        {

        }

        public ErrorResponse(string type, string message)
        {
            Type = type;
            Message = message;
        }
    }

    public class Exception : System.Exception
    {
        public Exception() : base() { }
        public Exception(string message) : base(message) { }
        public Exception(string message, Exception inner) : base(message, inner) { }

        public static Exception ExceptionForErrorResponse(ErrorResponse error)
        {
            switch (error.Type)
            {
                case "CodeMismatchException":
                    return new CodeMismatchException(error.Message);
                case "ExpiredCodeException":
                    return new ExpiredCodeException(error.Message);
                case "InvalidParameterException":
                    return new InvalidParameterException(error.Message);
                case "NotAuthorizedException":
                    return new NotAuthorizedException(error.Message);
                case "UsernameExistsException":
                    return new UsernameExistsException(error.Message);
                default:
                    return new Exception($"{error.Type}: {error.Message}");
            }
        }
    }

    public class CodeMismatchException : Exception
    {
        public CodeMismatchException() : base() { }
        public CodeMismatchException(string message) : base(message) { }
        public CodeMismatchException(string message, Exception inner) : base(message, inner) { }
    }

    public class ExpiredCodeException : Exception
    {
        public ExpiredCodeException() : base() { }
        public ExpiredCodeException(string message) : base(message) { }
        public ExpiredCodeException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidParameterException : Exception
    {
        public InvalidParameterException() : base() { }
        public InvalidParameterException(string message) : base(message) { }
        public InvalidParameterException(string message, Exception inner) : base(message, inner) { }
    }

    public class NotAuthorizedException : Exception
    {
        public NotAuthorizedException() : base() { }
        public NotAuthorizedException(string message) : base(message) { }
        public NotAuthorizedException(string message, Exception inner) : base(message, inner) { }
    }

    public class UsernameExistsException : Exception
    {
        public UsernameExistsException() : base() { }
        public UsernameExistsException(string message) : base(message) { }
        public UsernameExistsException(string message, Exception inner) : base(message, inner) { }
    }
}
