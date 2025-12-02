using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.ServiceManagement.Events
{
    /// <summary>
    /// Type-specific service registration event management
    /// </summary>
    internal class ServiceRegistrationEventsManager : IServiceRegistrationEventsManager
    {
        private Dictionary<Type, List<Delegate>> TypeCallbacks { get; } = new Dictionary<Type, List<Delegate>>();
        private object Lock { get; } = new object();

        void IServiceRegisteredSubscriber.Subscribe<T>(Action<T> callback)
        {
            if (callback == null)
            {
                return;
            }

            var serviceType = typeof(T);

            lock (Lock)
            {
                if (!TypeCallbacks.TryGetValue(serviceType, out var callbacks))
                {
                    callbacks = new List<Delegate>();
                    TypeCallbacks[serviceType] = callbacks;
                }

                if (!callbacks.Contains(callback))
                {
                    callbacks.Add(callback);
                }
            }
        }

        void IServiceRegisteredSubscriber.Unsubscribe<T>(Action<T> callback)
        {
            if (callback == null)
            {
                return;
            }

            var serviceType = typeof(T);

            lock (Lock)
            {
                if (TypeCallbacks.TryGetValue(serviceType, out var callbacks))
                {
                    callbacks.Remove(callback);

                    if (callbacks.Count == 0)
                    {
                        TypeCallbacks.Remove(serviceType);
                    }
                }
            }
        }

        void IServiceRegistrationEventsManager.NotifyServiceRegistered(Type serviceType, object instance)
        {
            if (serviceType == null || instance == null)
            {
                return;
            }

            List<Delegate> currentCallbacks = null;

            lock (Lock)
            {
                if (TypeCallbacks.TryGetValue(serviceType, out var callbacks))
                {
                    currentCallbacks = new List<Delegate>(callbacks);
                }
            }

            if (currentCallbacks != null)
            {
                foreach (var callback in currentCallbacks)
                {
                    try
                    {
                        // Invoke the typed callback
                        callback?.DynamicInvoke(instance);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in service registration callback for {serviceType.Name}: {ex.Message}");
                    }
                }
            }
        }

        void IServiceRegistrationEventsManager.Clear()
        {
            lock (Lock)
            {
                TypeCallbacks.Clear();
            }
        }
    }
}
