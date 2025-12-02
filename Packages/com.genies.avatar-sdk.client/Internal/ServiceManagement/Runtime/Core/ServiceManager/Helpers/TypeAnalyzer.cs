using System;
using System.Collections.Generic;
using System.Linq;

namespace Genies.ServiceManagement
{
    internal static class TypeAnalyzer
    {
        private static readonly Dictionary<Type, List<Type>> _sTypeCache = new Dictionary<Type, List<Type>>();
        private static readonly object _sLock = new object();

        /// <summary>
        /// Helper for returning all types implementing this interface.
        /// </summary>
        /// <param name="interfaceType"> Type to check </param>
        /// <returns></returns>
        public static IReadOnlyCollection<Type> FindImplementations(Type interfaceType)
        {
            lock (_sLock)
            {
                if (_sTypeCache.TryGetValue(interfaceType, out var implementations))
                {
                    return implementations;
                }

                implementations = AppDomain.CurrentDomain.GetAssemblies()
                                           .SelectMany(s => s.GetTypes())
                                           .Where(p => interfaceType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                                           .ToList();

                _sTypeCache[interfaceType] = implementations;

                return implementations;
            }
        }
    }

}
