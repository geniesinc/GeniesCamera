using System;
using System.Collections.Generic;
using System.Linq;
using Genies.CrashReporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Diagnostics;
using VContainer.Unity;
using Genies.ServiceManagement.Events;

namespace Genies.ServiceManagement
{
    public static partial class ServiceManager
    {
        /// <summary>
        /// Provide a bool flag to known when the Services are initialized and ready to use
        /// Set to true after <see cref="InitializeAppAsync"/> is done.
        /// </summary>
        public static bool IsAppInitialized { get; private set; }

        /// <summary>
        /// Flag to prevent concurrent initialization calls.
        /// Set to true during <see cref="InitializeAppAsync"/> execution.
        /// </summary>
        private static bool IsAppInitializing { get; set; }

        /// <summary>
        /// The last registered <see cref="GeniesRootLifetimeScope"/>
        /// </summary>
        internal static GeniesLifetimeScope LastRootScope => _rootScopes is { Count: > 0 } ? _rootScopes.Last() : null;

        /// <summary>
        /// Containers registered/tracked using <see cref="RegisterContainer"/>
        /// </summary>
        private static readonly LinkedList<IServiceContainer> _containers = new LinkedList<IServiceContainer>();

        /// <summary>
        /// Root scopes created using <see cref="InitializeAppAsync"/>
        /// </summary>
        private static readonly List<GeniesLifetimeScope> _rootScopes = new List<GeniesLifetimeScope>();

        /// <summary>
        /// Callstack cache when <see cref="ResolveService{TService}"/> is called to ensure we don't visit the same
        /// container twice.
        /// </summary>
        private static readonly HashSet<IServiceContainer> _visitedContainers = new HashSet<IServiceContainer>();

        /// <summary>
        /// Mapping of <see cref="IServiceContainer"/>s to their owner <see cref="GameObject"/>s used
        /// for proximity based resolving.
        /// </summary>
        private static readonly Dictionary<GameObject, LinkedList<IServiceContainer>> _gameObjectToContainersMap = new Dictionary<GameObject, LinkedList<IServiceContainer>>();

        /// <summary>
        /// Mapping of <see cref="IServiceContainer"/>s to their owner <see cref="Scene"/>s used
        /// for proximity based resolving.
        /// </summary>
        private static readonly Dictionary<Scene, LinkedList<IServiceContainer>> _sceneToContainersMap = new Dictionary<Scene, LinkedList<IServiceContainer>>();

        /// <summary>
        /// The <see cref="IServiceContainer"/> for singleton services, we keep track of it and when a new
        /// <see cref="GeniesLifetimeScope"/> awakes we convert it to a <see cref="GeniesSingletonLifetimeScope"/> so that
        /// the <see cref="IContainerBuilder"/> knows about its dependencies.
        /// </summary>
        private static SingletonServiceContainer _singletonServicesContainer;

        /// <summary>
        /// Tracks when <see cref="_singletonServicesContainer"/> gets a new registration. Used to check if we need to rebuild the
        /// <see cref="GeniesSingletonLifetimeScope"/>
        /// </summary>
        private static bool _singletonContainerUpdated;

        /// <summary>
        /// Cached for resolved services
        /// </summary>
        private static readonly Dictionary<(Type serviceType, object context, bool isCollection), object> _resolvedServicesCache =
            new Dictionary<(Type serviceType, object context, bool isCollection), object>();

        /// <summary>
        /// Subscribe to service registration events.
        /// Use this to get notified when services are registered.
        /// </summary>
        public static IServiceRegisteredSubscriber ServiceRegistered => _singletonServicesContainer?.ServiceRegistered;

        static ServiceManager()
        {
            OnAwake();
        }

        /// <summary>
        /// Run pre-app load configuration/initialization
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnAwake()
        {
            //Enable diagnostics in dev environment
            if (VContainerSettings.Instance != null)
            {
#if UNITY_EDITOR || QA_BUILD
                VContainerSettings.Instance.EnableDiagnostics = true;
#else
                //Disable in production for performance
                VContainerSettings.Instance.EnableDiagnostics = false;
#endif
            }

            // Create singleton container
            _singletonServicesContainer ??= new SingletonServiceContainer();
        }

        /// <summary>
        /// When a lifetime scope has just been created, we want to rebuild our singleton scope to include
        /// any changes that were made using <see cref="RegisterService{T}"/> calls, the reason for that is
        /// our <see cref="SingletonServiceContainer"/> isn't part of scopes so for them to be able to resolve from
        /// it we will convert it to a scope and set it as the parent to our root scopes which will allow
        /// any new scopes to find and resolve dependencies from it.
        /// </summary>
        internal static void OnLifetimeScopeCreated(IObjectResolver container)
        {
            OnScopeCreated(container);
        }

        /// <summary>
        /// Dispose all <see cref="IServiceContainer"/>s and clear all
        /// data/instances created so far. Use when reloading the app.
        /// </summary>
        public static void Dispose()
        {
            // Dispose of all containers in the _containers list
            foreach (var container in _containers)
            {
                container?.Dispose();
            }

            // Clear collections
            _containers.Clear();
            _visitedContainers.Clear();
            _gameObjectToContainersMap.Clear();
            _sceneToContainersMap.Clear();

            _singletonServicesContainer?.Dispose();
            _resolvedServicesCache.Clear();

            DisposeRootScopes();
            IsAppInitialized = false;
            IsAppInitializing = false;
        }

        /// <summary>
        /// Track a new <see cref="IServiceContainer"/> and cache its
        /// lookups to optimize resolving
        /// </summary>
        /// <param name="container"> container to track </param>
        internal static void RegisterContainer(IServiceContainer container)
        {
            if (container == null)
            {
                return;
            }

            // Check if the container already exists in the linked list
            if (!_containers.Contains(container))
            {
                if (container is GeniesSingletonLifetimeScope)
                {
                    // Always add GeniesSingletonLifetimeScope type containers last.
                    _containers.AddLast(container);
                }
                else if (container is GeniesRootLifetimeScope)
                {
                    // Add GeniesRootLifetimeScope right after the last GeniesSingletonLifetimeScope, if it exists. Otherwise, add last.
                    var lastSingleton = FindLastContainerOfType<GeniesSingletonLifetimeScope>();
                    if (lastSingleton != null)
                    {
                        _containers.AddAfter(lastSingleton, container);
                    }
                    else
                    {
                        _containers.AddLast(container);
                    }
                }
                else
                {
                    // For other types, add at the beginning (or change as per requirements).
                    _containers.AddFirst(container);
                }

                if (container.Owner != null)
                {
                    var scopeGo    = container.Owner;
                    var ownerScene = container.Owner.scene;

                    AddContainerToDictionary(_gameObjectToContainersMap, scopeGo,    container);
                    AddContainerToDictionary(_sceneToContainersMap,      ownerScene, container);
                    CrashReporter.LogInternal($"Registered Container {container.Owner.name} in {ownerScene.name}");
                }
                else
                {
                    CrashReporter.LogInternal($"Registered Container");
                }

                //Reset cache when a new container is added
                _resolvedServicesCache.Clear();
            }
            else
            {
                CrashReporter.LogInternal($"Container is already registered");
            }
        }

        /// <summary>
        /// Un-track the container.
        /// </summary>
        /// <param name="container"> Container to un-track </param>
        internal static void UnregisterContainer(IServiceContainer container)
        {
            RemoveContainerFromAllDictionaries(container);
            _containers.Remove(container);

            if (container is GeniesLifetimeScope asScope)
            {
                _rootScopes.Remove(asScope);
            }

            //Reset cache when a container is removed
            _resolvedServicesCache.Clear();

            CrashReporter.LogInternal($"Un-Registered Container {container.Owner.name}");
        }

        private static LinkedListNode<IServiceContainer> FindLastContainerOfType<T>()
        {
            var node = _containers.Last;
            while (node != null)
            {
                if (node.Value is T)
                {
                    return node;
                }

                node = node.Previous;
            }

            return null;
        }

        /// <summary>
        /// Only call when disposing root scopes
        /// </summary>
        private static void DisposeRootScopes()
        {
            foreach (var scope in _rootScopes)
            {
                scope.Dispose();
            }

            _rootScopes.Clear();
        }

        /// <summary>
        /// Helper to remove a container from a dictionary mapping.
        /// </summary>
        /// <param name="dictionary"> The dictionary to remove the container from </param>
        /// <param name="key"> The key to the tracked containers </param>
        /// <param name="container"> Container to remove </param>
        /// <typeparam name="TKey"> Key Type </typeparam>
        private static void RemoveContainerFromDictionary<TKey>(Dictionary<TKey, LinkedList<IServiceContainer>> dictionary, TKey key, IServiceContainer container)
        {
            if (dictionary.TryGetValue(key, out var linkedList))
            {
                linkedList.Remove(container);

                if (linkedList.Count == 0)
                {
                    dictionary.Remove(key);
                }
            }
        }

        /// <summary>
        /// Helper to add a container to a dictionary mapping.
        /// </summary>
        /// <param name="dictionary"> The dictionary to remove the container from </param>
        /// <param name="key"> The key to the tracked containers </param>
        /// <param name="container"> Container to remove </param>
        /// <typeparam name="TKey"> Key Type </typeparam>
        private static void AddContainerToDictionary<TKey>(Dictionary<TKey, LinkedList<IServiceContainer>> dictionary, TKey key, IServiceContainer container)
        {
            if (dictionary.TryGetValue(key, out var linkedList))
            {
                linkedList.AddFirst(container);
            }
            else
            {
                var newLinkedList = new LinkedList<IServiceContainer>();
                newLinkedList.AddFirst(container);
                dictionary.Add(key, newLinkedList);
            }
        }

        /// <summary>
        /// Helper to remove a container from tracked scenes/parent container mapping.
        /// </summary>
        /// <param name="container"> the container to remove </param>
        private static void RemoveContainerFromAllDictionaries(IServiceContainer container)
        {
            if (container == null || container.Owner == null)
            {
                return;
            }

            var rootParent = container.Owner.transform.root.gameObject;
            var ownerScene = container.Owner.scene;

            RemoveContainerFromDictionary(_gameObjectToContainersMap, rootParent, container);
            RemoveContainerFromDictionary(_sceneToContainersMap,      ownerScene, container);
        }
    }
}
