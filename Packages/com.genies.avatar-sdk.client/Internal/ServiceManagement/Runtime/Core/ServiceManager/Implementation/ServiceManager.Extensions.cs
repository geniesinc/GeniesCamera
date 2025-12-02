using System;
using UnityEngine;
using VContainer;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Useful extensions for registering or resolving services. Use to reduce verbosity.
    ///
    /// ex: this.GetService{T}();
    /// </summary>
    public static partial class ServiceManager
    {
        public static object GetService(this object caller, Type serviceType)
        {
            return GetWithContext(serviceType, caller);
        }
        
        public static T GetService<T>(this object caller)
        {
            return GetWithContext<T>(caller);
        }

        public static bool HasService<T>(this object caller)
        {
            return GetService<T>(caller) != null;
        }

        /// <summary>
        /// Extensions for the instance to register itself, it will map the instance to its type.
        /// if you want to have multiple interfaces mapped complete the registration using .As{T}
        ///
        /// ex: this.RegisterSelf{T}().As{TInterface}()
        ///                           .As{TOtherInterface();
        ///  
        /// </summary>
        /// <param name="instance"> The instance registering </param>
        /// <typeparam name="T"> Type of the instance </typeparam>
        /// <returns></returns>
        public static ServiceRegistrationBuilder RegisterSelf<T>(this T instance)
        {
            return RegisterService(instance);
        }

        public static ServiceRegistrationBuilder RegisterAs<TInstance, TInterface>(this TInstance instance)
            where TInstance : TInterface
        {
            return RegisterService(instance)
               .As<TInterface>();
        }

        public static ServiceRegistrationBuilder RegisterAs<TInstance, TInterface1, TInterface2>(this TInstance instance)
            where TInstance : TInterface1, TInterface2
        {
            return RegisterService(instance)
               .As<TInterface1, TInterface2>();
        }

        public static ServiceRegistrationBuilder RegisterAs<TInstance, TInterface1, TInterface2, TInterface3>(this TInstance instance)
            where TInstance : TInterface1, TInterface2, TInterface3
        {
            return RegisterService(instance)
               .As<TInterface1, TInterface2, TInterface3>();
        }
    }
}
