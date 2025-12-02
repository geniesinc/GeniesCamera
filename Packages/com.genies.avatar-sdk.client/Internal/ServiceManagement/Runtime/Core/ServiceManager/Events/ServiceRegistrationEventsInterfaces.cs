using System;

namespace Genies.ServiceManagement.Events
{
    /// <summary>
    /// Public interface for subscribing to service registration events
    /// </summary>
    public interface IServiceRegisteredSubscriber
    {
        /// <summary>
        /// Subscribe to be notified when a specific service type is registered
        /// </summary>
        /// <typeparam name="T">The service type to listen for</typeparam>
        /// <param name="callback">The callback to invoke when T is registered</param>
        /// <remarks>
        /// IMPORTANT: Always call Unsubscribe() with the same callback when you no longer need the subscription
        /// to prevent memory leaks.
        /// </remarks>
        void Subscribe<T>(Action<T> callback);

        /// <summary>
        /// Unsubscribe from being notified when a specific service type is registered
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <param name="callback">The callback to remove</param>
        void Unsubscribe<T>(Action<T> callback);
    }

    /// <summary>
    /// Internal interface for managing service registration events (includes notification methods)
    /// </summary>
    internal interface IServiceRegistrationEventsManager: IServiceRegisteredSubscriber
    {
        /// <summary>
        /// Notify subscribers about a service registration
        /// </summary>
        /// <param name="serviceType">The type that was registered</param>
        /// <param name="instance">The instance that was registered</param>
        void NotifyServiceRegistered(Type serviceType, object instance);

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        void Clear();
    }
}
