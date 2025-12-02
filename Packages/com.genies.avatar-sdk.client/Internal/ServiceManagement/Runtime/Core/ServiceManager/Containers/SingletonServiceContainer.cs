using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Diagnostics;
using VContainer.Internal;
using VContainer.Unity;
using Genies.ServiceManagement.Events;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// A custom container for holding services registered using the service locator pattern, supports
    /// registering using IDs and mapping to different interfaces.
    /// NOTE: Doesn't handle creating instances, just registering them.
    /// </summary>
    internal class SingletonServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, object> _AnonymousServiceRegistry = new Dictionary<Type, object>();
        private IServiceRegistrationEventsManager RegistrationEventsManager { get; }

        public GameObject Owner { get; private set; }
        public IServiceContainer ParentContainer { get; set; }
        public IObjectResolver Root => null;

        private readonly CompositeDisposable _Disposables = new CompositeDisposable();

        /// <summary>
        /// Subscriber for service registration notifications
        /// </summary>
        public IServiceRegisteredSubscriber ServiceRegistered => RegistrationEventsManager;

        public SingletonServiceContainer() : this(new ServiceRegistrationEventsManager())
        {
        }

        internal SingletonServiceContainer(IServiceRegistrationEventsManager registrationEventsManager)
        {
            RegistrationEventsManager = registrationEventsManager ?? throw new ArgumentNullException(nameof(registrationEventsManager));
        }

        /// <summary>
        /// Register an instance
        /// </summary>
        /// <param name="instance"> The instance to register </param>
        /// <param name="serviceType"> The type to map the instance to </param>
        /// <param name="id"> A custom id, this allows registering multiple instances of the same type </param>
        /// <exception cref="InvalidOperationException"> Thrown if instance is null </exception>
        /// <returns>True if the instance is added to a service registry, otherwise false (e.g. the instance is already added).</returns>
        internal void Register(object instance, Type serviceType)
        {
            if (instance == null)
            {
                throw new InvalidOperationException($"Can't register null instance {serviceType}");
            }

            if (ReferenceEquals(GetService(serviceType), instance))
            {
                // Instance is already registered
                return;
            }

            //TODO rework when refactoring service manager
            // if (instance is IDisposable asDisposable)
            // {
            //     _Disposables.Add(asDisposable);
            // }

            _AnonymousServiceRegistry[serviceType] = instance;

            // Notify subscribers about the registration
            try
            {
                RegistrationEventsManager.NotifyServiceRegistered(serviceType, instance);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error notifying service registration events for {serviceType.Name}: {ex.Message}");
            }
        }

        public object Get(Type serviceType)
        {
            return GetService(serviceType);
        }

        public T Get<T>()
        {
            return (T)GetService(typeof(T));
        }

        private object GetService(Type serviceType)
        {
            return _AnonymousServiceRegistry.GetValueOrDefault(serviceType);
        }

        public IReadOnlyCollection<object> GetCollection(Type serviceType)
        {
            return GetCollectionOfType(serviceType);
        }

        public IReadOnlyCollection<T> GetCollection<T>()
        {
            return new List<T>((IEnumerable<T>)GetCollectionOfType(typeof(T)));
        }

        private IReadOnlyCollection<object> GetCollectionOfType(Type serviceType)
        {
            var collection = new List<object>();


            var types = TypeAnalyzer.FindImplementations(serviceType);

            foreach (var type in types)
            {
                if (_AnonymousServiceRegistry.TryGetValue(type, out var instance))
                {
                    collection.Add(instance);
                }
            }

            return collection.AsReadOnly();
        }

        public void Clear()
        {
            _AnonymousServiceRegistry.Clear();
        }

        public void Dispose()
        {
            //TODO rework when refactoring
            // _Disposables.Dispose();
            _AnonymousServiceRegistry.Clear();

            // Clear event subscriptions
            RegistrationEventsManager?.Clear();

            ParentContainer?.Dispose();
            ParentContainer = null;
            Owner = null;
        }

        /// <summary>
        /// Returns all registered instances.
        /// </summary>
        public Dictionary<object, List<Type>> GetAllServices()
        {
            var instanceToTypesMap = new Dictionary<object, List<Type>>();

            // First add all anonymous services
            foreach (var entry in _AnonymousServiceRegistry)
            {
                if (!instanceToTypesMap.ContainsKey(entry.Value))
                {
                    instanceToTypesMap[entry.Value] = new List<Type>();
                }

                instanceToTypesMap[entry.Value].Add(entry.Key);
            }

            return instanceToTypesMap;
        }
    }
}
