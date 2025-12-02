using System;
using System.Runtime.Serialization;

namespace Genies.DataRepositoryFramework
{
    [Serializable]
    public class DataRepositoryException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public DataRepositoryException()
        {
        }

        public DataRepositoryException(string message) : base(message)
        {
        }

        public DataRepositoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataRepositoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
