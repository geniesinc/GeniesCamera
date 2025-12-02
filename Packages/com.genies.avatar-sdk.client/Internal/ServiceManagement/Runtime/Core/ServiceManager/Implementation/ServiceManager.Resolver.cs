using Genies.CrashReporting;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// This part of the <see cref="ServiceManager"/> handles resolving services. When resolving a service the logic is as follows:
    ///
    /// - If the context object is a Transform or Component or GameObject we will do a proximity search and find the closest <see cref="IServiceContainer"/> to
    ///   that context and try to resolve from it. Proximity search does hierarchy lookup first and if it fails tries a scene wide lookup.
    /// - If service wasn't resolve yet, we will use an Order based lookup where the last registered container in <see cref="_containers"/> will
    ///   be checked first and we will try to resolve from all its <see cref="IServiceContainer.ParentContainer"/>s recursively
    /// - If Proximity-based and Order-based lookups fail, we will then do a singleton lookup in the <see cref="SingletonServiceContainer"/>
    /// - If all lookups fail we will return null and log an exception
    /// </summary>
    public static partial class ServiceManager
    {
        public static LazyService<T> GetLazy<T>(object context)
        {
            return new LazyService<T>(() => GetWithContext<T>(context));
        }

        public static LazyService<IReadOnlyCollection<T>> GetLazyCollection<T>(object context)
        {
            return new LazyService<IReadOnlyCollection<T>>(() => GetCollection<T>(context));
        }

        public static T Get<T>()
        {
            return (T)Get(typeof(T));
        }

        public static T GetWithContext<T>(object context)
        {
            return (T)GetWithContext(typeof(T), context);
        }

        public static IReadOnlyCollection<T> GetCollection<T>(object context)
        {
            return GetCollection(typeof(T), context) as IReadOnlyCollection<T>;
        }

        public static object GetWithContext(Type serviceType, object context)
        {
            var cacheKey = (serviceType, context, false);

            // Return from cache if available
            if (_resolvedServicesCache.TryGetValue(cacheKey, out var cachedService))
            {
                if(cachedService != null)
                {
                    return cachedService;
                }

                _resolvedServicesCache.Remove(cacheKey);
            }

            //If not cached, resolve then cache
            var resolvedService = ResolveService(context, (container) => container.Get(serviceType), serviceType);
            _resolvedServicesCache[cacheKey] = resolvedService;

            return resolvedService;
        }

        public static object Get(Type serviceType)
        {
            return GetWithContext(serviceType, null);
        }

        public static IReadOnlyCollection<object> GetCollection(Type serviceType, object context)
        {
            // Create a unique cache key for collections
            var collectionCacheKey = (serviceType, context, true);

            if (_resolvedServicesCache.TryGetValue(collectionCacheKey, out var cachedObj)
                && cachedObj is IReadOnlyCollection<object> cachedCollection)
            {
                // Verify if all items in cachedCollection are still valid
                var allItemsValid = true;

                foreach (var item in cachedCollection)
                {
                    if (item != null)
                    {
                        continue;
                    }

                    allItemsValid = false;
                    break;
                }

                if (allItemsValid)
                {
                    return cachedCollection;
                }
            }

            // If we reach here, we either didn't have the collection cached, or it was invalidated
            var actualCollection = ResolveService(context, (container) => container.GetCollection(serviceType), serviceType);

            // Cache the entire collection and each item within
            _resolvedServicesCache[collectionCacheKey] = actualCollection;
            return actualCollection;
        }

        private static TService ResolveService<TService>(object context, Func<IServiceContainer, TService> resolver, Type serviceType)
        {
            _visitedContainers.Clear();

            if (context is IServiceContainer serviceContainer)
            {
                _visitedContainers.Add(serviceContainer);
            }

            Transform requesterTransform = null;
            var requesterScene = new Scene();

            if (context != null)
            {
                if (context is Transform transform)
                {
                    requesterTransform = transform;
                    requesterScene = transform.gameObject.scene;
                }
                else if (context is GameObject gameObject)
                {
                    requesterTransform = gameObject.transform;
                    requesterScene = gameObject.scene;
                }
                else if (context is MonoBehaviour monoBehaviour)
                {
                    requesterTransform = monoBehaviour.transform;
                    requesterScene = monoBehaviour.gameObject.scene;
                }
            }

            // Proximity-based resolution here
            if (requesterTransform != null)
            {
                var requesterGo = requesterTransform.gameObject;

                var resolvedService = FindServiceByProximity(resolver, requesterGo, requesterScene);
                if (resolvedService != null)
                {
                    return resolvedService;
                }
            }

            // Fallback to order-based method
            foreach (var container in _containers)
            {
                // Skip the container that matches the context to avoid a stack overflow.
                if (container == context)
                {
                    continue;
                }

                var resolvedService = GetFromContainerHierarchy(resolver, container, _visitedContainers);
                if (resolvedService != null)
                {
                    return resolvedService;
                }
            }

            //Fallback to singletons
            if (_singletonServicesContainer != null && context != _singletonServicesContainer)
            {
                var resolvedService = resolver(_singletonServicesContainer);
                if (resolvedService != null)
                {
                    return resolvedService;
                }
            }

            // Debugging information when resolution fails
            var contextInfo = context != null ? context.GetType().Name : "null";
            var message = $"Service of type {serviceType.Name} could not be resolved. Context: {contextInfo}";

#if !GENIES_SDK
            CrashReporter.LogWarning(message);
#endif
            return default;
        }

        private static TService FindServiceByProximity<TService>(Func<IServiceContainer, TService> resolver, GameObject requesterGameObject, Scene requesterScene)
        {
            LinkedList<IServiceContainer> containerLinkedList;
            GameObject currentGameObject = requesterGameObject;

            //Scene of the requester doesn't have any containers.
            if (!_sceneToContainersMap.TryGetValue(requesterScene, out containerLinkedList) || containerLinkedList?.Count == 0)
            {
                return default;
            }

            while (currentGameObject != null)
            {
                if (_gameObjectToContainersMap.TryGetValue(currentGameObject, out containerLinkedList))
                {
                    foreach (var container in containerLinkedList)
                    {
                        var resolvedService = GetFromContainerHierarchy(resolver, container, _visitedContainers);
                        if (resolvedService != null)
                        {
                            return resolvedService;
                        }
                    }
                }


                // Move up the hierarchy to check the parent game object's containers
                var parentTransform = currentGameObject.transform.parent;
                currentGameObject = parentTransform != null ? parentTransform.gameObject : null;
            }

            // If not found, search siblings recursively.
            TService siblingService = RecursiveSiblingSearch(resolver, requesterGameObject);
            if (siblingService != null)
            {
                return siblingService;
            }

            if (_sceneToContainersMap.TryGetValue(requesterScene, out containerLinkedList))
            {
                foreach (var container in containerLinkedList)
                {
                    var resolvedService = GetFromContainerHierarchy(resolver, container, _visitedContainers);
                    if (resolvedService != null)
                    {
                        return resolvedService;
                    }
                }
            }

            return default;
        }

        private static TService RecursiveSiblingSearch<TService>(Func<IServiceContainer, TService> resolver, GameObject currentObject)
        {
            Transform parentTransform = currentObject.transform.parent;
            if (parentTransform == null)
            {
                return default;
            }

            // Check siblings
            foreach (Transform sibling in parentTransform)
            {
                if (sibling == currentObject.transform)
                {
                    continue; // skip the requester itself
                }

                if (_gameObjectToContainersMap.TryGetValue(sibling.gameObject, out var linkedList))
                {
                    foreach (var container in linkedList)
                    {
                        var resolvedService = resolver(container);
                        if (resolvedService != null)
                        {
                            return resolvedService;
                        }
                    }
                }
            }

            // If not found among siblings, try with the parent's siblings
            return RecursiveSiblingSearch(resolver, parentTransform.gameObject);
        }

        private static TService GetFromContainerHierarchy<TService>(
            Func<IServiceContainer, TService> resolver,
            IServiceContainer container,
            HashSet<IServiceContainer> visitedContainers
        )
        {
            while (container != null)
            {
                if (visitedContainers.Contains(container))
                {
                    return default;
                }

                visitedContainers.Add(container);

                try
                {
                    return resolver(container);
                }
                catch (VContainerException e)
                {
                    if (!e.Message.Contains("No such registration of type"))
                    {
                        CrashReporter.LogHandledException(e);
                    }
                }


                container = container.ParentContainer;
            }

            return default;
        }
    }
}
