using System;

namespace Genies.ServiceManagement
{
    
    /// <summary>
    /// A builder pattern for dynamically registering singleton services using <see cref="ServiceManager.RegisterService{T}"/>
    /// this is a typical pattern for registering services and mapping them to multiple interfaces.
    /// </summary>
    public class ServiceRegistrationBuilder
    {
        private readonly SingletonServiceContainer _container;
        private readonly object _instance;

        internal ServiceRegistrationBuilder()
        {
        }

        internal ServiceRegistrationBuilder(SingletonServiceContainer container, object instance, Type instanceType)
        {
            _container = container;
            _instance = instance;
            
            // Register the instance immediately
            _container.Register(_instance, serviceType: instanceType);
        }

        public ServiceRegistrationBuilder AsSelf()
        {
            _container.Register(_instance, serviceType: _instance.GetType());
            return this;
        }

        public ServiceRegistrationBuilder AsImplementedInterfaces()
        {
            foreach (var interfaceType in _instance.GetType().GetInterfaces())
            {
                _container.Register(_instance, serviceType: interfaceType);
            }

            return this;
        }

        private ServiceRegistrationBuilder As(params Type[] serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                _container.Register(_instance, serviceType: serviceType);
            }

            return this;
        }

        public ServiceRegistrationBuilder As<TInterface>()
            => As(typeof(TInterface));

        public ServiceRegistrationBuilder As<TInterface1, TInterface2>()
            => As(typeof(TInterface1), typeof(TInterface2));

        public ServiceRegistrationBuilder As<TInterface1, TInterface2, TInterface3>()
            => As(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3));

        public ServiceRegistrationBuilder As<TInterface1, TInterface2, TInterface3, TInterface4>()
            => As(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4));
    }
}
