using System;
using Cysharp.Threading.Tasks;
using VContainer;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// This part of the <see cref="ServiceManager"/> handles the <see cref="SingletonServiceContainer"/> and registrations into it.
    /// This part also handles converting <see cref="SingletonServiceContainer"/> into <see cref="GeniesSingletonLifetimeScope"/> if a new
    /// <see cref="GeniesLifetimeScope"/> awakes and the <see cref="_singletonContainerUpdated"/> is set to true.
    /// </summary>
    public static partial class ServiceManager
    {

        private static void OnScopeCreated(IObjectResolver container)
        {
            container.FallbackResolveType = type => _singletonServicesContainer.Get(type);
        }

        public static ServiceRegistrationBuilder RegisterService<T>(T instance)
        {
            // Create a new ServiceRegistrationBuilder instance and return it to allow fluent API usage
            var registration = new ServiceRegistrationBuilder(_singletonServicesContainer, instance, typeof(T));
            UpdateCache(instance, typeof(T));
            return registration;
        }
        
        private static void UpdateCache<T>(T instance, Type serviceType)
        {
            (Type, object, bool) cacheKey = (serviceType, null, false);
            _resolvedServicesCache[cacheKey] = instance;
        }

        public static ServiceRegistrationBuilder RegisterService<TImplementation, TInterface1>(TImplementation instance)
            where TImplementation : TInterface1
        {
            var builder = RegisterService(instance);
            return builder.As<TInterface1>();
        }

        public static ServiceRegistrationBuilder RegisterService<TImplementation, TInterface1, TInterface2>(TImplementation instance)
            where TImplementation : TInterface1, TInterface2
        {
            var builder = RegisterService(instance);
            return builder.As<TInterface1, TInterface2>();
        }

        public static ServiceRegistrationBuilder RegisterService<TImplementation, TInterface1, TInterface2, TInterface3>(
            TImplementation instance)
            where TImplementation : TInterface1, TInterface2, TInterface3
        {
            var builder = RegisterService(instance);
            return builder.As<TInterface1, TInterface2, TInterface3>();
        }
    }
}
