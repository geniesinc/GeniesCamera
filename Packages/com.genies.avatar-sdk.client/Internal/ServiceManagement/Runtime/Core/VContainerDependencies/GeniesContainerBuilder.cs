using Genies.CrashReporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VContainer;
using VContainer.Diagnostics;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// This builder will decorate the default <see cref="IContainerBuilder"/> when installing
    /// the service manager dependencies. Its used to avoid duplicate registration of the same
    /// instances in a scope/container.
    /// </summary>
    internal class GeniesContainerBuilder : IContainerBuilder
    {
        private readonly List<GeniesContainerBuilder> _previousBuilders = new List<GeniesContainerBuilder>();
        private readonly IContainerBuilder _decoratedBuilder;
        private static FieldInfo _implementationTypeFieldInfo;
        private IGeniesInstaller _currentInstaller;
        private readonly HashSet<Type> _registeredInstallerTypes = new HashSet<Type>();

        public object ApplicationOrigin
        {
            get => _decoratedBuilder.ApplicationOrigin;
            set => _decoratedBuilder.ApplicationOrigin = value;
        }

        public DiagnosticsCollector Diagnostics
        {
            get => _decoratedBuilder.Diagnostics;
            set => _decoratedBuilder.Diagnostics = value;
        }

        public int Count => _decoratedBuilder.Count;

        public RegistrationBuilder this[int index]
        {
            get => _decoratedBuilder[index];
            set => _decoratedBuilder[index] = value;
        }

        public GeniesContainerBuilder(IContainerBuilder decoratedBuilder, List<GeniesContainerBuilder> previousBuilders)
        {
            _decoratedBuilder = decoratedBuilder;
            _previousBuilders = previousBuilders;
            _previousBuilders.Add(this);

            // Inherit registered installer types from previous builders
            foreach (var builder in _previousBuilders)
            {
                if (builder != this) // Don't include self
                {
                    foreach (var installerType in builder._registeredInstallerTypes)
                    {
                        _registeredInstallerTypes.Add(installerType);
                    }
                }
            }
        }

        public T Register<T>(T registrationBuilder) where T : RegistrationBuilder
        {
            var fieldInfo          = GetImplementationTypeFieldInfo();
            var implementationType = (Type)fieldInfo.GetValue(registrationBuilder);

            //If it already exists return
            if (_previousBuilders.Any(i => i.Exists(implementationType, true)))
            {
                var message = $"Detected duplicate registration <b>{implementationType.Name}</b>.";

                if (_currentInstaller != null)
                {
                    message += $" In Installer <b>{_currentInstaller.GetType().Name}</b>";
                }

                CrashReporter.LogHandledException(new ServiceManagerException(message));
                return registrationBuilder;
            }

            //Override diagnostics collection to show the correct registration call stack.
            var pDiagnostics = _decoratedBuilder.Diagnostics;
            pDiagnostics?.TraceRegister(new RegisterInfo(registrationBuilder));
            _decoratedBuilder.Diagnostics = null;

            registrationBuilder = _decoratedBuilder.Register(registrationBuilder);
            _decoratedBuilder.Diagnostics = pDiagnostics;

            return registrationBuilder;
        }

        /// <summary>
        /// Due to <see cref="RegistrationBuilder.ImplementationType"/> being internal
        /// we use reflection to get it.
        /// </summary>
        /// <returns></returns>
        private static FieldInfo GetImplementationTypeFieldInfo()
        {
            if (_implementationTypeFieldInfo != null)
            {
                return _implementationTypeFieldInfo;
            }

            // Get the Type object representing the RegistrationBuilder class
            Type builderType = typeof(RegistrationBuilder);

            // Get the FieldInfo object representing the ImplementationType field
            _implementationTypeFieldInfo = builderType.GetField("ImplementationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (_implementationTypeFieldInfo == null)
            {
                Console.WriteLine("Field not found");
            }

            return _implementationTypeFieldInfo;
        }

        public void RegisterBuildCallback(Action<IObjectResolver> container)
        {
            _decoratedBuilder.RegisterBuildCallback(container);
        }

        public bool Exists(Type type, bool includeInterfaceTypes = false)
        {
            return _decoratedBuilder.Exists(type, includeInterfaceTypes);
        }

        public void SetCurrentInstaller(IGeniesInstaller installer)
        {
            _currentInstaller = installer;
        }

        /// <summary>
        /// Installs an installer with requirement validation.
        /// Validates dependencies before executing the installation.
        /// </summary>
        /// <param name="installer">The installer to install</param>
        /// <exception cref="ServiceManagerException">Thrown when validation fails or installation fails</exception>
        public void InstallWithValidation(IGeniesInstaller installer)
        {
            var installerType = installer.GetType();

            try
            {
                // Validate requirements before installation
                InstallerRequirementValidator.ValidateRequirements(installer, _registeredInstallerTypes);

                // Track this installer as registered only after successful validation
                _registeredInstallerTypes.Add(installerType);

                // Set current installer for context
                SetCurrentInstaller(installer);

                // Execute the installation
                installer.Install(this);
            }
            catch (ServiceManagerException)
            {
                // Re-throw validation or installation exceptions without adding to registered types
                // This ensures failed installers are not tracked as successfully installed
                throw;
            }
            catch (Exception ex)
            {
                // Wrap unexpected exceptions in ServiceManagerException for consistent error handling
                throw new ServiceManagerException($"Failed to install {installerType.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the types of installers that have been registered so far.
        /// </summary>
        /// <returns>Read-only collection of registered installer types</returns>
        public IReadOnlyCollection<Type> GetRegisteredInstallerTypes()
        {
            return _registeredInstallerTypes;
        }

        /// <summary>
        /// Validates and automatically sorts a collection of installers based on their dependencies.
        /// Uses topological sorting to ensure proper dependency order and detects circular dependencies.
        /// </summary>
        /// <param name="installers">Collection of installers to validate and sort</param>
        /// <returns>Installers sorted in dependency order</returns>
        public static List<IGeniesInstaller> ValidateAndSortInstallerCollection(IEnumerable<IGeniesInstaller> installers)
        {
            return InstallerRequirementValidator.ValidateAndSortInstallers(installers);
        }
    }
}
