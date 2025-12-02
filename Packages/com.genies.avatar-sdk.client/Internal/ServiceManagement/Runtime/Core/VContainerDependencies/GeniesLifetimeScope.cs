using Genies.CrashReporting;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Customizes the <see cref="LifetimeScope"/> and implements <see cref="IServiceContainer"/>
    /// the <see cref="ServiceManager"/> needs to track lifetime scopes to properly resolve,
    /// the <see cref="IServiceContainer"/> encapsulates different containers and also will help aid refactoring
    /// in the future as it abstracts resolving to not depend on a specific framework.
    ///
    /// this scope does the following:
    ///
    /// - Notify the <see cref="ServiceManager"/> when its created
    /// - Register itself as an <see cref="IServiceContainer"/> for the service manager to lookup
    /// - Respect the initialization of <see cref="ServiceManager"/> before installing its dependencies
    /// - Hides implementations of <see cref="LifetimeScope"/> as we don't support using it as is.
    /// </summary>
    internal class GeniesLifetimeScope : LifetimeScope, IServiceContainer
    {
        GameObject IServiceContainer.Owner => _overrideOwner ? _overrideOwner : gameObject;

        IServiceContainer IServiceContainer.ParentContainer
        {
            get => Parent as IServiceContainer;
        }

        private GameObject _overrideOwner;

        protected sealed override async void Awake()
        {
            await InitializeAndBuild();
        }
        
        internal async UniTask InitializeAndBuild()
        {
            autoRun = false;
            
            try
            {
                //Always set root scope as parent if no parent was set.
                if (!typeof(GeniesRootLifetimeScope).IsAssignableFrom(GetType()))
                {
                    //Wait until root scope is built first
                    if (ServiceManager.LastRootScope != null)
                    {
                        await UniTask.WaitUntil(() => ServiceManager.LastRootScope.Container != null);
                    }
                    
                    //Try to set parent here first using VContainer typical lookup
                    base.Awake();

                    if (Parent == null)
                    {
                        parentReference.Object = ServiceManager.LastRootScope;
                    }
                }
                else
                {
                    base.Awake();
                }

                //Explicit build
                Build();
                
                //Let service manager prepare singleton container
                //for injection into this scope.
                ServiceManager.OnLifetimeScopeCreated(Container);

                await AwaitBuildAndRegister();
                OnBuilt();
            }
            catch (VContainerException e)
            {
                CrashReporter.LogHandledException(e);
            }
        }

        private void SetOverrideOwner(GameObject overrideOwner)
        {
            if (overrideOwner == null)
            {
                return;
            }

            var scopeOwnerLifetime = overrideOwner.GetComponent<ScopeOwnerLifetimeEventHandler>() ?? overrideOwner.AddComponent<ScopeOwnerLifetimeEventHandler>();
            scopeOwnerLifetime.OnDestroyed += Dispose;
            _overrideOwner = overrideOwner;
        }

        protected virtual void OnBuilt()
        {
        }

        protected sealed override void Configure(IContainerBuilder builder)
        {
            Install(builder);
            base.Configure(builder);
        }

        protected virtual void Install(IContainerBuilder builder)
        {
        }

        private async UniTask AwaitBuildAndRegister()
        {
            if (Container == null)
            {
                await UniTask.WaitUntil(() => Container != null);
            }

            ServiceManager.RegisterContainer(this);
        }

        protected sealed override void OnDestroy()
        {
            ServiceManager.UnregisterContainer(this);
            base.OnDestroy();
        }

        public new GeniesLifetimeScope CreateChild(IInstaller installer = null)
        {
            return CreateChild<GeniesLifetimeScope>(installer);
        }

        public new TScope CreateChild<TScope>(Action<IContainerBuilder> installation) where TScope : GeniesLifetimeScope
        {
            return CreateChild<TScope>($"GeniesLifetimeScope (Child) - {Guid.NewGuid().ToString()}", new ActionInstaller(installation));
        }

        public new TScope CreateChild<TScope>(IInstaller installer) where TScope : GeniesLifetimeScope
        {
            return CreateChild<TScope>($"GeniesLifetimeScope (Child) - {Guid.NewGuid().ToString()}", installer);
        }

        public TScope CreateChild<TScope>(string scopeName, Action<IContainerBuilder> installation, GameObject overrideOwner = null) where TScope : GeniesLifetimeScope
        {
            return CreateChild<TScope>(scopeName, new ActionInstaller(installation), overrideOwner);
        }

        public TScope CreateChild<TScope>(string scopeName, IInstaller installer = null, GameObject overrideOwner = null)
            where TScope : GeniesLifetimeScope
        {
            //Enqueue Install dependencies
            using var extraInstallation = Enqueue(installer);

            var childGameObject = new GameObject(scopeName);
            childGameObject.SetActive(false);
            if (IsRoot)
            {
                DontDestroyOnLoad(childGameObject);
            }
            else
            {
                childGameObject.transform.SetParent(transform, false);
            }

            var child = childGameObject.AddComponent<TScope>();
            child.parentReference.Object = this;
            child.SetOverrideOwner(overrideOwner);

            childGameObject.SetActive(true);
            return child;
        }

        
        // We need to return a scope object that would sit in memory instead of in a scene?
        /*
        private static TScope Create<TScope>(string scopeName, bool isRoot = false, IInstaller installer = null,
            GameObject overrideOwner = null)
            where TScope : GeniesLifetimeScope
        {

        }
*/
        private static TScope Create<TScope>(string scopeName, bool isRoot = false, IInstaller installer = null, GameObject overrideOwner = null)
            where TScope : GeniesLifetimeScope
        {
            //Enqueue Install dependencies
            using var extraInstallation = Enqueue(installer);

            var gameObject = new GameObject(scopeName);
            gameObject.SetActive(false);

            if (isRoot)
            {
                DontDestroyOnLoad(gameObject);
            }

            var newScope = gameObject.AddComponent<TScope>();
            newScope.IsRoot = isRoot;
            newScope.SetOverrideOwner(overrideOwner);
            gameObject.SetActive(true);

            return newScope;
        }

        public static TScope CreateRoot<TScope>(string scopeName, Action<IContainerBuilder> configuration, GameObject overrideOwner = null)
            where TScope : GeniesLifetimeScope =>
            Create<TScope>(scopeName, isRoot: true, new ActionInstaller(configuration), overrideOwner);

        public static TScope Create<TScope>(string scopeName, Action<IContainerBuilder> configuration, GameObject overrideOwner = null)
            where TScope : GeniesLifetimeScope =>
            Create<TScope>(scopeName, false, new ActionInstaller(configuration), overrideOwner);

        public new static GeniesLifetimeScope Create(Action<IContainerBuilder> configuration) =>
            Create<GeniesLifetimeScope>("", false, new ActionInstaller(configuration));

        T IServiceContainer.Get<T>()
        {
            return (T)GetService(typeof(T));
        }

        IReadOnlyCollection<T> IServiceContainer.GetCollection<T>()
        {
            return new List<T>((IEnumerable<T>)GetCollectionOfType(typeof(T)));
        }

        public object Get(Type serviceType)
        {
            return GetService(serviceType);
        }

        public IReadOnlyCollection<object> GetCollection(Type serviceType)
        {
            return GetCollectionOfType(serviceType);
        }

        private object GetService(Type serviceType)
        {
            if (this.Container == null)
            {
                Build();
            }

            return this.Container!.Resolve(serviceType);
        }

        private IReadOnlyCollection<object> GetCollectionOfType(Type serviceType)
        {
            if (this.Container == null)
            {
                Build();
            }

            var types = TypeAnalyzer.FindImplementations(serviceType);

            var instances = new List<object>();
            foreach (var type in types)
            {
                try
                {
                    var objectResolver = this.Container;
                    if (objectResolver != null)
                    {
                        var instance = objectResolver.Resolve(type);
                        if (instance != null)
                        {
                            instances.Add(instance);
                        }
                    }
                }
                catch
                {
                    // Handle exception
                }
            }

            return instances.AsReadOnly();
        }
    }
}
