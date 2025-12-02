using System;

namespace Genies.PerformanceMonitoring
{
    internal class CustomInstrumentationException : Exception
    {
        public CustomInstrumentationException(string message) : base(message)
        {
        }
    }
}
