using System;

namespace Genies.S3Service.Models
{
    [Serializable]
    internal class GeniesS3OperationException : Exception
    {
        public GeniesS3OperationException(string message) : base(message)
        {
        }

        public GeniesS3OperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
