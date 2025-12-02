using System;
using System.Collections.Generic;
using System.Linq;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Validates that all installer requirements are satisfied at runtime.
    /// </summary>
    internal static class InstallerRequirementValidator
    {
        /// <summary>
        /// Validates all requirements for an installer before it runs.
        /// Throws ServiceManagerException if requirements are not met.
        /// </summary>
        /// <param name="installer">The installer to validate</param>
        /// <param name="registeredInstallerTypes">Collection of installer types that have already been registered</param>
        public static void ValidateRequirements(IGeniesInstaller installer, IReadOnlyCollection<Type> registeredInstallerTypes)
        {
            if (!InstallerRequirementAnalyzer.HasRequirements(installer.GetType()))
            {
                return; // No requirements to validate
            }

            var installerType = installer.GetType();
            var requiredTypes = InstallerRequirementAnalyzer.GetRequiredInstallerTypes(installer);
            var missingRequirements = new List<string>();

            foreach (var requiredType in requiredTypes)
            {
                if (!IsRequirementSatisfied(requiredType, registeredInstallerTypes))
                {
                    missingRequirements.Add($"  - {requiredType.Name}");
                }
            }

            if (missingRequirements.Any())
            {
                var errorMessage = $"{installerType.Name} requires the following installers to be registered first. " +
                                   $"Ensure they have an earlier OperationOrder:\n" +
                                   string.Join("\n", missingRequirements);

                throw new ServiceManagerException(errorMessage);
            }
        }

        /// <summary>
        /// Validates that all required installer dependencies are present in the collection.
        /// </summary>
        /// <param name="installers">Collection of installers to validate</param>
        /// <exception cref="ServiceManagerException">Thrown when required dependencies are missing</exception>
        public static void ValidateAllDependenciesPresent(IEnumerable<IGeniesInstaller> installers)
        {
            var installerList = installers.ToList();
            var availableTypes = new HashSet<Type>(installerList.Select(i => i.GetType()));
            var missingDependencies = new List<string>();

            foreach (var installer in installerList)
            {
                var installerType = installer.GetType();
                var requiredTypes = InstallerRequirementAnalyzer.GetRequiredInstallerTypes(installer);

                foreach (var requiredType in requiredTypes)
                {
                    if (!IsRequirementSatisfied(requiredType, availableTypes))
                    {
                        missingDependencies.Add($"  {installerType.Name} requires {requiredType.Name}");
                    }
                }
            }

            if (missingDependencies.Any())
            {
                var errorMessage = "Missing required installer dependencies. The following installers are required but not provided:\n" +
                                   string.Join("\n", missingDependencies);

                throw new ServiceManagerException(errorMessage);
            }
        }

        /// <summary>
        /// Validates and automatically sorts installers based on their dependencies using topological sorting.
        /// This method ensures all dependencies are satisfied and detects circular dependencies.
        /// </summary>
        /// <param name="installers">Collection of installers to validate and sort</param>
        /// <returns>Installers sorted in dependency order</returns>
        /// <exception cref="ServiceManagerException">Thrown when circular dependencies or missing dependencies are detected</exception>
        public static List<IGeniesInstaller> ValidateAndSortInstallers(IEnumerable<IGeniesInstaller> installers)
        {
            // First validate all dependencies are present
            ValidateAllDependenciesPresent(installers);

            // Then sort them topologically (this will detect cycles)
            return InstallerTopologicalSorter.Sort(installers);
        }

        /// <summary>
        /// Checks if a required type is satisfied by any of the registered types, considering inheritance.
        /// </summary>
        /// <param name="requiredType">The type that is required</param>
        /// <param name="registeredTypes">Collection of registered types</param>
        /// <returns>True if the requirement is satisfied by inheritance or exact match</returns>
        private static bool IsRequirementSatisfied(Type requiredType, IReadOnlyCollection<Type> registeredTypes)
        {
            return registeredTypes.Any(registeredType => requiredType.IsAssignableFrom(registeredType));
        }
    }
}
