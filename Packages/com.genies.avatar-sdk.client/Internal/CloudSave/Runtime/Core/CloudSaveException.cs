using System;
using System.Runtime.Serialization;

namespace Genies.CloudSave
{
    [Serializable]
    public class CloudSaveException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public CloudSaveException()
        {
        }

        public CloudSaveException(string message) : base(message)
        {
        }

        public CloudSaveException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CloudSaveException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
