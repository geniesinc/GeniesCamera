using Cysharp.Threading.Tasks;
using System;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// A lazy instance of the service, will only request resolving when
    /// invoked.
    /// </summary>
    /// <typeparam name="T"> Type of the service </typeparam>
    public class LazyService<T>
    {
        private readonly Lazy<T> _lazyInstance;

        public LazyService(Func<T> factoryMethod)
        {
            _lazyInstance = new Lazy<T>(factoryMethod, true);
        }

        public T Instance => _lazyInstance.Value;

        /// <summary>
        /// Async resolve, timeout frames will be used to fail the operation
        /// if the instance was never resolved.
        /// </summary>
        /// <param name="timeoutFrameCount"> How many frames to wait until its resolved </param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public async UniTask<T> GetInstanceAsync(int timeoutFrameCount = 1)
        {
            int frameCount = 0;
            while (frameCount < timeoutFrameCount)
            {
                if (_lazyInstance.Value != null)
                {
                    return _lazyInstance.Value;
                }
                
                await UniTask.Yield(PlayerLoopTiming.Update);
                frameCount++;
            }

            throw new TimeoutException("Failed to get the service instance within the timeout period.");
        }
    }
}
