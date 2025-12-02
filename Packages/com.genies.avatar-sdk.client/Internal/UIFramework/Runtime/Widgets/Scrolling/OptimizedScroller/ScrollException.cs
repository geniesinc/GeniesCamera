using System;

namespace Genies.UI.Scroller
{
    public class ScrollException : Exception
    {
        public ScrollException()
        {
        }

        public ScrollException(string message) : base(message)
        {
        }

        public ScrollException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}