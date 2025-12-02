using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Genies.ServiceManagement
{
    
    /// <summary>
    /// This class handles finding all types marked with <see cref="AutoResolveAttribute"/>
    /// in an optimized manner. Types marked with the AutoResolve attribute will have to either inherited
    /// <see cref="IGeniesInstaller"/> or <see cref="IGeniesInitializer"/> this class also handles returning the
    /// settings/configuration for a specific auto resolver type.
    /// </summary>
    internal static partial class AutoResolver
    {
        private const string _autoResolverSettingsKey = "AutoResolverSettings";

        private static AutoResolverSettings _autoResolverSettings;

        private static AutoResolverSettings GlobalAutoResolverSettings
        {
            get
            {
                if (_autoResolverSettings == null)
                {
                    _autoResolverSettings = Resources.Load<AutoResolverSettings>(_autoResolverSettingsKey);
                }

                return _autoResolverSettings;
            }
            set => _autoResolverSettings = value;
        }

        private static bool _cacheInitialized = false;
        private static readonly Dictionary<Type, TypeInformation> _typeCache = new Dictionary<Type, TypeInformation>();
        private static readonly Dictionary<Type, object> _instanceCache = new Dictionary<Type, object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ResetCache()
        {
            _cacheInitialized = false;
        }
        
        public static IEnumerable<IGeniesInstaller> GetAutoInstallers(AutoResolverSettings overrideSettings = null)
        {
            var settings = overrideSettings ? overrideSettings : GlobalAutoResolverSettings;
            
            return GetTypeInformationCollection()
                  .Where(t => t.IsInstaller && (settings == null || settings.IsInstallationEnabled(t.Type)))
                  .Select(t => (IGeniesInstaller)GetOrCreateInstance(t.Type, overrideSettings));
        }

        public static IEnumerable<IGeniesInitializer> GetAutoInitializers(AutoResolverSettings overrideSettings = null)
        {
            var settings = overrideSettings ? overrideSettings : GlobalAutoResolverSettings;
            
            return GetTypeInformationCollection()
                  .Where(t => t.IsInitializer && (settings == null || settings.IsInitializationEnabled(t.Type)))
                  .Select(t => (IGeniesInitializer)GetOrCreateInstance(t.Type, overrideSettings));
        }

        /// <summary>
        /// Returns if the type inherits <see cref="IGeniesInstaller"/>
        /// </summary>
        /// <param name="type"> Type to check </param>
        /// <returns></returns>
        public static bool IsInstallerAvailable(Type type)
        {
            return typeof(IGeniesInstaller).IsAssignableFrom(type);
        }

        /// <summary>
        /// Returns if the type inherits <see cref="IGeniesInitializer"/>
        /// </summary>
        /// <param name="type"> Type to check </param>
        /// <returns></returns>
        public static bool IsInitializerAvailable(Type type)
        {
            return typeof(IGeniesInitializer).IsAssignableFrom(type);
        }

        /// <summary>
        /// Creates a new auto resolve type instance if it wasn't already serialized 
        /// </summary>
        /// <param name="type"> Type marked with Auto Resolve </param>
        /// <param name="overrideSettings"> override settings </param>
        /// <returns></returns>
        private static object GetOrCreateInstance(Type type, AutoResolverSettings overrideSettings = null)
        {
            var settings = overrideSettings ? overrideSettings : GlobalAutoResolverSettings;
            var instance = settings != null ? settings.GetResolverInstance(type) : null;

            if (instance != null)
            {
                return instance;
            }

            if (!_instanceCache.TryGetValue(type, out instance))
            {
                instance = Activator.CreateInstance(type);
                _instanceCache[type] = instance;
            }

            return instance;
        }

        /// <summary>
        /// Returns auto resolve type information from the cache if it was already initialized
        /// else will create a new type information and cache it
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<TypeInformation> GetTypeInformationCollection()
        {
            if (_cacheInitialized)
            {
                return _typeCache.Values;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<AutoResolveAttribute>();
                    if (attribute != null)
                    {
                        var typeInfo = new TypeInformation(type);
                        _typeCache[type] = typeInfo;
                    }
                }
            }

            _cacheInitialized = true;
            return _typeCache.Values;
        }


        /// <summary>
        /// Information about the auto resolve marked type
        /// </summary>
        private class TypeInformation
        {
            public Type Type { get; }
            public bool IsInstaller { get; }
            public bool IsInitializer { get; }

            public TypeInformation(Type type)
            {
                Type = type;

                if (typeof(IGeniesInstaller).IsAssignableFrom(type))
                {
                    IsInstaller = true;
                }

                if (typeof(IGeniesInitializer).IsAssignableFrom(type))
                {
                    IsInitializer = true;
                }
            }
        }
    }
}
