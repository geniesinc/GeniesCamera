using Genies.CrashReporting;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Implementation for <see cref="ServiceManager"/> initialization logic, the initialization logic, every app
    /// that wishes to use the <see cref="ServiceManager"/> should invoke <see cref="InitializeAppAsync"/> the initialization does the
    /// following:
    ///
    /// - Allows the user to pass custom <see cref="IGeniesInstaller"/> and <see cref="IGeniesInitializer"/> instances for initializing their app
    /// - Uses <see cref="AutoResolver"/> to get all Auto Service Resolvers that implement <see cref="IGeniesInstaller"/> and <see cref="IGeniesInitializer"/>
    /// - Creates the <see cref="GeniesSingletonLifetimeScope"/> so that the installers can get automatic injection for singleton instances that were registered
    ///   running the initialization.
    /// - Groups all <see cref="IGeniesInstaller"/> and <see cref="IGeniesInitializer"/> that have the same order <see cref="IGroupedOperation.OperationOrder"/>
    /// - Foreach group we create a new <see cref="GeniesRootLifetimeScope"/> parent it to the previous and install the services from all <see cref="IGeniesInstaller"/>
    ///   in group then we call <see cref="IGeniesInitializer.Initialize"/> for all initializers in that group.
    ///
    ///
    /// NOTE: For dev/editor we also try to resolve all the services (in production they are lazily resolved when required) to find any installation issues.
    /// NOTE: All scopes created are root scopes and will live as long as the app session lives.
    /// </summary>
    public static partial class ServiceManager
    {
        /// <summary>
        /// Initialize the app services, the initialization will create DontDestroyOnLoad scopes
        /// as we consider any app level services long lasting.
        /// </summary>
        /// <param name="customInstallers"> Extra installers </param>
        /// <param name="customInitializers"> Extra initializers </param>
        /// <param name="disableAutoResolve"> If you want to disable auto resolved services </param>
        /// <param name="overrideSettings"> If you want to override auto resolve settings for a demo or different scenes</param>
        public static async UniTask InitializeAppAsync(
            List<IGeniesInstaller> customInstallers = null,
            List<IGeniesInitializer> customInitializers = null,
            bool disableAutoResolve = false,
            AutoResolverSettings overrideSettings = null)
        {
            if (IsAppInitialized)
            {
                var exception = new ServiceManagerException("App was already initialized, if you need to re-initialize make sure to call ServiceManager.Dispose first.");
                CrashReporter.LogHandledException(exception);
                return;
            }

            if (IsAppInitializing)
            {
                Debug.LogWarning($"[{nameof(ServiceManager)}] App initialization is already in progress. Wait for the current initialization to complete before calling InitializeAppAsync again.");
                return;
            }

            IsAppInitializing = true;

            try
            {
                var autoResolvedInstallers   = disableAutoResolve ? new List<IGeniesInstaller>() : AutoResolver.GetAutoInstallers(overrideSettings).ToList();
                var autoResolvedInitializers = disableAutoResolve ? new List<IGeniesInitializer>() : AutoResolver.GetAutoInitializers(overrideSettings).ToList();

                customInstallers ??= new List<IGeniesInstaller>();
                customInitializers ??= new List<IGeniesInitializer>();

                customInitializers = customInitializers.Concat(autoResolvedInitializers).ToList();

                // Install custom installers first to ensure no duplicates.
                var orderedInstallersList = new List<IGeniesInstaller>();
                orderedInstallersList.AddRange(customInstallers);
                orderedInstallersList.AddRange(autoResolvedInstallers);

                //Create app root scopes
                await CreateScopeAsync(null, orderedInstallersList, customInitializers, dontDestroyOnLoad: true);
            }
            catch (ServiceManagerException ex)
            {
                // Log app initialization failure and report to crash reporting
                Debug.LogError($"[{nameof(ServiceManager)}] App initialization failed: {ex.Message}");
                CrashReporter.LogHandledException(ex);
                throw;
            }
            catch (Exception ex)
            {
                // Handle unexpected exceptions (e.g., AutoResolver reflection errors, constructor failures)
                var wrappedException = new ServiceManagerException($"Unexpected error during app initialization: {ex.Message}", ex);
                Debug.LogError($"[{nameof(ServiceManager)}] App initialization failed: {wrappedException.Message}");
                CrashReporter.LogHandledException(wrappedException);
                throw wrappedException;
            }
            finally
            {
                // Always set IsAppInitialized to true to force Dispose() call for cleanup
                // This prevents potential duplicate initialization when partial state may exist
                IsAppInitialized = true;

                // Always reset the initializing flag regardless of success or failure
                IsAppInitializing = false;
            }
        }
    }
}
