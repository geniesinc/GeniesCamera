using System;

namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Exception thrown when an invalid avatar modification is attempted
    /// </summary>
    public class InvalidAvatarModificationException : Exception
    {
        public InvalidAvatarModificationException(string message) : base(message)
        {
        }
    }
}