using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting.Helpers;
using Debug = UnityEngine.Debug;

namespace Genies.Refs
{
    /// <summary>
    /// A single unique reference to a resource handle. We are using the <see cref="IGenerationalResourceReference"/> interface for being able to easily cast this class down to a non-generic reference.
    /// </summary>
    internal sealed class GenerationalResourceReference<TResource> : IGenerationalResourceReference<TResource>
    {
        [ThreadStatic] private static Stack<GenerationalResourceReference<TResource>> _pool;

        public ulong Generation { get; private set; }
        public bool IsAlive { get; private set; }
        public TResource Item => _handle.Resource;
        public Handle<TResource> Handle => _handle;

        Handle IGenerationalResourceReference.Handle => _handle;
        object IGenerationalResourceReference.Item => Item;

        private Handle<TResource> _handle;
        private StackTrace _creationStackTrace;

        private GenerationalResourceReference() { }
        public static GenerationalResourceReference<TResource> New(Handle<TResource> handle, StackTrace creationStackTrace = null)
        {
            if (!handle.IsAlive)
            {
                return null;
            }

            // get or create a reference instance
            _pool ??= new Stack<GenerationalResourceReference<TResource>>();
            var reference = _pool.Count > 0 ? _pool.Pop() : new GenerationalResourceReference<TResource>();

            // initialize the new generation
            ++reference.Generation;
            reference.IsAlive = true;
            reference._handle = handle;
            reference._creationStackTrace = creationStackTrace;
            handle.AddReference();

            return reference;
        }

        public IGenerationalResourceReference<TResource> New()
            => New(_handle);

        IGenerationalResourceReference IGenerationalResourceReference.New()
            => New(_handle);

        public void Dispose()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            _pool ??= new Stack<GenerationalResourceReference<TResource>>(); // you could dispose the ref in a different thread
            _pool.Push(this);
            _creationStackTrace = null;
            _handle.RemoveReference();
            _handle = default;
        }

        // we should always try to dispose references manually since we cannot fully rely on Unity's garbage collector
        ~GenerationalResourceReference()
        {
            GcDispose().Forget();
        }

        // only used when disposed by the garbage collector to make sure that we dispose on the Unity's main thread and don't send the item back to the pool
        private async UniTaskVoid GcDispose()
        {
            await UniTask.SwitchToMainThread();

            if (!IsAlive)
            {
                return;
            }

#if UNITY_EDITOR && GENIES_INTERNAL
            if (_creationStackTrace is null)
                Debug.LogWarning($"[Refs] A reference to {_handle.Resource} has been disposed by the garbage collector. The reference should have been disposed manually");
            else
                Debug.LogWarning($"[Refs] A reference to {_handle.Resource} has been disposed by the garbage collector. The reference should have been disposed manually.\nReference creation stack trace:\n{_creationStackTrace.GetCleanStackTraceWithFileLinks()}");
#endif

            IsAlive = false;
            _creationStackTrace = null;
            _handle.RemoveReference();
            _handle = default;
        }
    }
}
