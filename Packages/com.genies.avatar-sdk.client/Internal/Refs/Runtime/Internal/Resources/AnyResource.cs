using System;
using System.Collections.Generic;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a resource that is disposed through a custom callback.
    /// </summary>
    internal sealed class AnyResource<T> : IResource<T>
    {
        [ThreadStatic]
        private static Stack<AnyResource<T>> _pool;
        
        public T Resource { get; private set; }
        
        private Action<T> _disposeCallback;
        private bool _isDisposed;
        
        private AnyResource() { }
        public static AnyResource<T> New(T resource, Action<T> disposeCallback = null)
        {
            _pool ??= new Stack<AnyResource<T>>();
            var instance = _pool.Count > 0 ? _pool.Pop() : new AnyResource<T>();
            
            instance.Resource = resource;
            instance._isDisposed = false;
            instance._disposeCallback = disposeCallback;
            
            return instance;
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _disposeCallback?.Invoke(Resource);
            _disposeCallback = null;
            Resource = default;
            _isDisposed = true;
            _pool ??= new Stack<AnyResource<T>>();
            _pool.Push(this);
        }

    }
}