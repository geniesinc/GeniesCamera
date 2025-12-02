using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Toolbox.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Genies.Utilities.Internal
{
    /// <summary>
    /// Utility component for the creation of async initializers that can be initialized as a dependency tree.
    /// </summary>
    public class Initializer : MonoBehaviour
    {
        [SerializeField]
        private bool autoInitialize = true;

        /// <summary>
        /// To use this _IsInitialized property must be overriden
        /// </summary>
        [SerializeField]
        private bool initializeOnce;

        [ReorderableList(ListStyle.Lined, "Dependency", Foldable = false)]
        [SerializeField]
        private List<Initializer> dependencies;

        /// <summary>
        /// Override this to log a message after successful initialization that also specifies the time it took.
        /// </summary>
        protected virtual string _InitializationSuccessMessage => null;
        protected virtual bool _IsInitialized => false;

        // state
        private readonly UniTaskCompletionSource<bool> _initialization = new();
        private readonly HashSet<Type> _requiredDependencyTypes = new();
        private bool _startedInitialization = false;
        private bool _didAwake = false;

        public bool AutoInitialize
        {
            get => autoInitialize;
            set => autoInitialize = value;
        }

        public bool StartedInitialization => _startedInitialization;

        protected virtual void OnAwake(){}
        private void Awake()
        {
            if (_didAwake)
            {
                return;
            }

            _didAwake = true;

            if(_IsInitialized && initializeOnce)
            {
                return;
            }

            OnAwake();

            if (autoInitialize)
            {
                ResolvedInitializeAsync().Forget();
            }
        }

        /// <summary>
        /// Expose a way to control the initialization when the Initializer is not autoInitialize type
        /// </summary>
        public async UniTask LateInitialize()
        {
            if (autoInitialize || _startedInitialization)
            {
                Debug.LogError($"[{GetType().Name}] wrong initialization");
                return;
            }

            await ResolvedInitializeAsync();
        }

        /// <summary>
        /// Expose a way to control the initialization directly
        /// </summary>
        public async UniTask BeginInitialize()
        {
            if (_startedInitialization)
            {
                Debug.LogWarning("You tried to initalize the " + GetType() + " which was already initalized");
                return;
            }

            await ResolvedInitializeAsync();
        }

        /// <summary>
        /// Waits until initialization has finished. It will wait even if the initialization didn't start.
        /// Handy if you want to wait for initialization but don't want to trigger it for yourself.
        /// </summary>
        public UniTask WaitUntilInitializedAsync()
        {
            return _initialization.Task;
        }

        /// <summary>
        /// Invoked before initializing dependencies. This is the right place for declaring required dependencies (see <see cref="Require{T}"/>)
        /// or running independent fire and forget logic.
        /// </summary>
        protected virtual void PreInitialize() { }

        /// <summary>
        /// Invoked after initializing dependencies.
        /// </summary>
        protected virtual UniTask InitializeAsync()
            => UniTask.CompletedTask;

        /// <summary>
        /// Registers the given <see cref="Initializer"/> type as a required dependency. Initialization will fail if the required
        /// dependencies are not provided.
        /// </summary>
        /// <typeparam name="T">The <see cref="Initializer"/> type that is required</typeparam>
        protected void Require<T>()
            where T : Initializer
        {
            _requiredDependencyTypes.Add(typeof(T));
        }

        /// <summary>
        /// Clears the currently required dependencies.
        /// </summary>
        protected void ClearRequiredDependencies()
        {
            _requiredDependencyTypes.Clear();
        }

        private async UniTask<bool> ResolvedInitializeAsync()
        {
            if (!_didAwake)
            {
                Awake();
            }

            // initialize only once
            if (_startedInitialization)
            {
                return await _initialization.Task;
            }

            _startedInitialization = true;

            try
            {
                // custom child class pre initialization
                PreInitialize();

                // check that all required dependencies
                if (!HasRequiredDependencies())
                {
                    throw new Exception($"[{GetType().Name}] missing required dependencies");
                }

                // old way of initializing dependencies, now done in order all within GeniesPartyInitializer, this is not used
                bool[] results = await UniTask.WhenAll(dependencies.Select(dependency => dependency.ResolvedInitializeAsync()));

                // only initialize if all dependencies initialized successfully
                if (results.Any(result => !result))
                {
                    LogInitializationSkip();
                    _initialization.TrySetResult(false);
                    return false;
                }

                // custom child class initialization (count the time it takes and log the initialization message after)
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                await InitializeAsync();
                LogInitializationMessage(stopwatch);
            }
            catch (Exception exception)
            {
                LogInitializationException(exception);
                _initialization.TrySetResult(false);
                return false;
            }

            _initialization.TrySetResult(true);
            return true;
        }

        // checks if the initializer has the required dependencies and logs errors for those missing
        private bool HasRequiredDependencies()
        {
            var dependencyTypes = new HashSet<Type>(dependencies.Select(dependency => dependency.GetType()));
            bool hasRequiredDependencies = true;

            foreach (Type requiredType in _requiredDependencyTypes)
            {
                if (dependencyTypes.Contains(requiredType))
                {
                    continue;
                }

                Debug.LogError($"[{GetType().Name}] missing required dependency: {requiredType.Name}");
                hasRequiredDependencies = false;
            }

            return hasRequiredDependencies;
        }

        private void LogInitializationMessage(Stopwatch stopwatch)
        {
            string message = _InitializationSuccessMessage;
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            stopwatch.Stop();

#if UNITY_EDITOR
            Debug.Log($"<color=green>[INIT SUCCESS]</color> <color=cyan>Took {stopwatch.Elapsed.TotalSeconds:0.00} seconds:</color> {message}");
#else
            Debug.Log($"[INIT SUCCESS] Took {stopwatch.Elapsed.TotalSeconds:0.00} seconds: {message}");
#endif
        }

        private void LogInitializationSkip()
        {
#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[INIT SKIP]</color> {GetType().Name}: one ore more dependencies failed to initialize.");
#else
            Debug.Log($"[INIT SKIP] {GetType().Name}: one ore more dependencies failed to initialize.");
#endif
        }

        private void LogInitializationException(Exception exception)
        {
#if UNITY_EDITOR
            Debug.LogError($"<color=red>[INIT ERROR]</color> {GetType().Name} initialization exception: {exception}");
#else
            Debug.LogError($"[INIT ERROR] {GetType().Name} initialization exception: {exception}");
#endif
        }
    }
}
