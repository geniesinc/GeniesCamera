using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.CrashReporting.Helpers;
using Debug = UnityEngine.Debug;

namespace Genies.Refs
{
    /// <summary>
    /// Handles a <see cref="IResource{TResource}"/> instance.
    /// </summary>
    internal sealed class GenerationalResourceHandle<T> : IGenerationalResourceHandle<T>
    {
        [ThreadStatic]
        private static Stack<GenerationalResourceHandle<T>> _pool;

        public ulong Generation { get; private set; }
        public bool IsAlive => _resource is not null;
        public T Resource => _resource is null ? default : _resource.Resource;
        public int ReferenceCount {get; private set; }

        public event Action<T> Releasing;

        object IGenerationalResourceHandle.Resource => Resource;

        private IResource<T> _resource;
        private StackTrace _creationStackTrace;

        private GenerationalResourceHandle() { }
        public static GenerationalResourceHandle<T> New(IResource<T> resource, StackTrace creationStackTrace = null)
        {
            if (resource is null)
            {
                return null;
            }

            // get or create a handle instance
            _pool ??= new Stack<GenerationalResourceHandle<T>>();
            var handle = _pool.Count > 0 ? _pool.Pop() : new GenerationalResourceHandle<T>();

            // initialize the new generation
            ++handle.Generation;
            handle.ReferenceCount = 0;
            handle.Releasing = null;
            handle._resource = resource;
            handle._creationStackTrace = creationStackTrace;

            // track this handle globally
            ReferencedResourcesTracker.TrackHandle(handle);

            return handle;
        }

        public void AddReference()
        {
            ++ReferenceCount;
        }

        public void RemoveReference()
        {
            if (--ReferenceCount <= 0)
            {
                Dispose();
            }
        }

        public Ref<T> NewReference()
        {
            return CreateRef.FromHandle(new Handle<T>(this));
        }

        Ref IGenerationalResourceHandle.NewReference()
        {
            return NewReference();
        }

        public void Dispose()
        {
            if (_resource is null)
            {
                return;
            }

            if (ReferenceCount > 0)
            {
#if GENIES_INTERNAL
                Debug.LogWarning($"[Refs] A resource handle to {_resource.Resource} is being disposed manually while still having {ReferenceCount} alive references. You should avoid disposing handles manually");
#endif
            }

            TryDisposeResource();
            _pool ??= new Stack<GenerationalResourceHandle<T>>();
            _pool.Push(this);
        }

        private void TryDisposeResource()
        {
            try
            {
                Releasing?.Invoke(_resource.Resource);
                _resource.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Refs] exception thrown when disposing resource: {_resource.Resource?.ToString()}\n{exception}");
            }
            finally
            {
                ReferenceCount = 0;
                Releasing = null;
                _resource = null;
                _creationStackTrace = null;
            }
        }

        // we should always try to dispose references manually since we cannot fully rely on Unity's garbage collector
        ~GenerationalResourceHandle()
        {
            GcRelease().Forget();
        }

        // only used when released by the garbage collector to make sure that we release on the Unity's main thread and don't send the item back to the pool
        private async UniTaskVoid GcRelease()
        {
            await UniTask.SwitchToMainThread();

            if (_resource is null)
            {
                return;
            }
#if GENIES_INTERNAL
            if (_creationStackTrace is null)
            {
                CrashReporter.LogWarning($"[Refs] A resource handle to {_resource.Resource} with {ReferenceCount} alive references has been released by the garbage collector. The handle should have been released by disposing all its references");
            }
            else
            {
                CrashReporter.LogWarning($"[Refs] A resource handle to {_resource.Resource} with {ReferenceCount} alive references has been released by the garbage collector. The handle should have been released by disposing all its references.\nHandle creation stack trace:\n{_creationStackTrace.GetCleanStackTraceWithFileLinks()}");
            }
#endif
            TryDisposeResource();
        }
    }
}
