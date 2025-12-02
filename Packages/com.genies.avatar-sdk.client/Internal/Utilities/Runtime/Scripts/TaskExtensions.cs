using System;
using System.Threading.Tasks;

namespace Genies.Utilities
{
    public static class TaskExtensions
    {
        public static async Task WithTimeout(this Task task, TimeSpan timeout, bool throwException = false)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                if (throwException)
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }

            // Propagate any exception that may have occurred.
            await task;
        }

        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, bool throwException = false)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                if (throwException)
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }

            return await task;
        }
    }
}
