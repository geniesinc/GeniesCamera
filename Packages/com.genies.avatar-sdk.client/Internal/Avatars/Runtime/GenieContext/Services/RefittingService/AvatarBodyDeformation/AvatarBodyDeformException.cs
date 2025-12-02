using System;
using System.Runtime.Serialization;

namespace Genies.Avatars
{
    [Serializable]
    public sealed class AvatarBodyDeformException : Exception
    {
        public AvatarBodyDeformException()
        {
        }

        public AvatarBodyDeformException(string message) : base(message)
        {
        }

        public AvatarBodyDeformException(string message, Exception inner) : base(message, inner)
        {
        }

        public AvatarBodyDeformException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}