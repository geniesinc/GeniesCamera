using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using Debug = UnityEngine.Debug;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// This part of the service manager works similar to <see cref="InitializeAppAsync"/> but
    /// instead of creating root scopes it will do the same grouping and initialization logic
    /// but will be tied to a scope in the current scene
    /// </summary>
    public static partial class ServiceManager
    {
        /// <summary>
        /// Use for installing services owned by a specific scene/owner, the lifetime of those services
        /// depends on the lifetime of the scene/owner
        /// </summary>
        /// <param name="scopeOwner"> Optional owner of the scopes, can pass null if you want the active scene to own it </param>
        /// <param name="installers"> installers to install </param>
        /// <param name="initializers"> Initializers to initialize </param>
        /// <param name="dontDestroyOnLoad"> If the scopes should persist across scene loads if set to true the scope owner will be ignored </param>
        public static async UniTask CreateScopeAsync(
            GameObject scopeOwner,
            List<IGeniesInstaller> installers,
            List<IGeniesInitializer> initializers = null,
            bool dontDestroyOnLoad = false)
        {
            var containerBuilders = new List<GeniesContainerBuilder>();

            installers ??= new List<IGeniesInstaller>();
            initializers ??= new List<IGeniesInitializer>();

            // Validate and sort ALL installers first, before grouping
            var sortedInstallers = GeniesContainerBuilder.ValidateAndSortInstallerCollection(installers);

            var groups = CreateResolveGroups(sortedInstallers, initializers);

            //Create the scope for each group
            GeniesLifetimeScope newScope = null;

            async UniTask CreateScope(Action<IContainerBuilder> builder, string scopeName)
            {
                if (newScope == null)
                {
                    newScope = GeniesLifetimeScope.Create<GeniesLifetimeScope>(scopeName, builder, scopeOwner);
                }
                else
                {
                    newScope.CreateChild<GeniesLifetimeScope>(scopeName, builder, scopeOwner);
                }

                await UniTask.WaitUntil(() => newScope.Container != null).Timeout(TimeSpan.FromSeconds(1));
            }

            var ownerName = "";
            if (!dontDestroyOnLoad)
            {
                ownerName = scopeOwner ? scopeOwner.name : SceneManager.GetActiveScene().name;
            }
            else
            {
                ownerName = "DontDestroyOnLoad";
            }

            // Process each group
            foreach (var group in groups)
            {
                try
                {
                    group.GroupName = $"Lifetime Scope Group: {group.GroupNumber} - Owner: {ownerName}";
                    await ProcessResolveGroupAsync(group, dontDestroyOnLoad ? CreateRootScope : CreateScope, containerBuilders);
                }
                catch (ServiceManagerException ex)
                {
                    // Log scope creation failure and re-throw to fail the entire CreateScopeAsync operation
                    Debug.LogError($"[{nameof(ServiceManager)}] Failed to create scope for owner '{ownerName}': {ex.Message}");
                    throw;
                }
            }
        }

        private static List<ResolveGroup> CreateResolveGroups(
            List<IGeniesInstaller> customInstallers,
            List<IGeniesInitializer> customInitializers)
        {
            customInstallers ??= new List<IGeniesInstaller>();
            customInitializers ??= new List<IGeniesInitializer>();

            // Create a HashSet to eliminate duplicates
            var uniqueInitializers = new HashSet<IGeniesInitializer>(customInitializers);

            var uniqueInstances = new HashSet<object>();
            var uniqueTypes     = new HashSet<Type>();

            var groups = customInstallers
                        .Concat(uniqueInitializers.OfType<IGroupedOperation>())
                        .Where(x => uniqueInstances.Add(x) && uniqueTypes.Add(x.GetType()))
                        .GroupBy(i => i.OperationOrder)
                        .Select(
                                g => new ResolveGroup
                                {
                                    GroupNumber = g.Key,
                                    Installers = g.OfType<IGeniesInstaller>().ToList(),
                                    Initializers = g.OfType<IGeniesInitializer>().ToList(),
                                }
                               )
                        .OrderBy(g => g.GroupNumber)
                        .ToList();

            return groups;
        }

        private static async UniTask ProcessResolveGroupAsync(
            ResolveGroup group,
            Func<Action<IContainerBuilder>, string, UniTask> createScopeFunc,
            List<GeniesContainerBuilder> geniesContainerBuilders
        )
        {
            // Step 1: Create a new scope for Installers
            if (group.Installers.Any())
            {
                try
                {
                    await CreateScopeFromInstallersAsync(group.Installers, createScopeFunc, group.GroupName ?? $"Scope Group: {group.GroupNumber}", geniesContainerBuilders);
                }
                catch (ServiceManagerException ex)
                {
                    // Log installer failure at group level and re-throw to fail the entire scope creation
                    Debug.LogError($"[{nameof(ServiceManager)}] Failed to process resolve group {group.GroupNumber}: {ex.Message}");
                    throw;
                }
            }

            // Step 2: Group initializers by InitializationOrder and initialize them in groups
            // Duplicate initialization might occur, handle on the service level.
            var initializerGroups = group.Initializers
                                         .GroupBy(i => i.InitializationOrder)
                                         .OrderBy(g => g.Key);

            foreach (var initializerGroup in initializerGroups)
            {
                var initializationTasks = Enumerable.Select(initializerGroup, initializer => initializer.Initialize())
                                                    .ToList();

                #if UNITY_EDITOR
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                #endif

                try
                {
                    await UniTask.WhenAll(initializationTasks);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{nameof(ServiceManager)}]Creation: Error during initialization{e}");
                }
                finally
                {
                    #if UNITY_EDITOR
                    stopwatch.Stop();
                    CrashReporter.LogInternal($"Scope Group: {group.GroupNumber} Initialization completed in {stopwatch.ElapsedMilliseconds} ms.");
                    #endif
                }
            }
        }

        private static async UniTask CreateScopeFromInstallersAsync(
            List<IGeniesInstaller> installers,
            Func<Action<IContainerBuilder>, string, UniTask> createScopeFunc,
            string scopeName,
            List<GeniesContainerBuilder> geniesContainerBuilders
        )
        {
            // Installers are already validated and sorted at the top level
            IContainerBuilder currentBuilder = null;

            try
            {
                await createScopeFunc(
                                      builder =>
                                      {
                                          var overrideBuilder = new GeniesContainerBuilder(builder, geniesContainerBuilders);

                                          currentBuilder = overrideBuilder;

                                          foreach (var installer in installers)
                                          {
                                              // Use validation-enabled installation method
                                              overrideBuilder.InstallWithValidation(installer);
                                          }
                                      },
                                      scopeName
                                     );
            }
            catch (ServiceManagerException ex)
            {
                // Log the scope creation failure with comprehensive context
                Debug.LogError($"[{nameof(ServiceManager)}] Failed to create scope '{scopeName}': {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Wrap unexpected scope creation failures in ServiceManagerException
                var errorMessage = $"Unexpected error during scope creation for '{scopeName}': {ex.Message}";
                Debug.LogError($"[{nameof(ServiceManager)}] {errorMessage}");
                throw new ServiceManagerException(errorMessage, ex);
            }


            //Will only run in Dev/Editor
            DebugContainerServiceResolving(currentBuilder);
        }

        private static async UniTask CreateRootScope(Action<IContainerBuilder> builder, string scopeName)
        {
            GeniesRootLifetimeScope newScope = null;
            GeniesLifetimeScope     lastRoot = LastRootScope;
            if (lastRoot == null)
            {
                newScope = GeniesLifetimeScope.CreateRoot<GeniesRootLifetimeScope>(scopeName, builder);
            }
            else
            {
                newScope = lastRoot.CreateChild<GeniesRootLifetimeScope>(scopeName, builder);

                //Singleton lifetime scope is destroyed/re-created so we want to make sure
                //we don't parent to it to ensure its children don't get destroyed
                if (lastRoot is GeniesSingletonLifetimeScope)
                {
                    //Unparent from singleton scope
                    newScope.transform.SetParent(lastRoot.gameObject.transform.parent);
                }
            }

            _rootScopes.Add(newScope);
            await UniTask.WaitUntil(() => newScope.Container != null);
        }
    }
}
