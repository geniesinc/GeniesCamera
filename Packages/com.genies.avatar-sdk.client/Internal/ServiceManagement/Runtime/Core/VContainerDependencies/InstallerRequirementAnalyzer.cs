using System;
using System.Collections.Generic;
using System.Linq;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Analyzes installer types to detect their requirements through IRequiresInstaller&lt;T&gt; interfaces.
    /// </summary>
    internal static class InstallerRequirementAnalyzer
    {
        /// <summary>
        /// Gets all required installer types for a given installer.
        /// </summary>
        /// <param name="installerType">The installer type to analyze</param>
        /// <returns>Collection of installer types that must be registered before this installer</returns>
        public static IEnumerable<Type> GetRequiredInstallerTypes(Type installerType)
        {
            if (!typeof(IGeniesInstaller).IsAssignableFrom(installerType))
            {
                yield break;
            }

            var interfaces = installerType.GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IRequiresInstaller<>))
                {
                    var requiredInstallerType = interfaceType.GetGenericArguments()[0];
                    yield return requiredInstallerType;
                }
            }
        }

        /// <summary>
        /// Checks if an installer has any requirements.
        /// </summary>
        /// <param name="installerType">The installer type to check</param>
        /// <returns>True if the installer implements IHasInstallerRequirements</returns>
        public static bool HasRequirements(Type installerType)
        {
            return typeof(IHasInstallerRequirements).IsAssignableFrom(installerType);
        }

        /// <summary>
        /// Gets all required installer types for a given installer instance.
        /// </summary>
        /// <param name="installer">The installer instance to analyze</param>
        /// <returns>Collection of installer types that must be registered before this installer</returns>
        public static IEnumerable<Type> GetRequiredInstallerTypes(IGeniesInstaller installer)
        {
            return GetRequiredInstallerTypes(installer.GetType());
        }
    }
}
