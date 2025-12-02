using System;

namespace Genies.ServiceManagement
{
    public class ServiceManagerException : Exception
    {
        public ServiceManagerException() { }
        public ServiceManagerException(string message) : base(message) { }
        public ServiceManagerException(string message, Exception inner) : base(message, inner) { }
    }
}
