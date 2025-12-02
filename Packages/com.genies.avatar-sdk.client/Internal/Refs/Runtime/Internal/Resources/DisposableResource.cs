using System;
using System.Collections.Generic;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a resource that implements the IDisposable interface.
    /// </summary>
    internal sealed class DisposableResource<T> : IResource<T>
        where T : IDisposable
    {
        [ThreadStatic]
        private static Stack<DisposableResource<T>> _pool;

        public T Resource { get; private set; }
        
        private bool _isDisposed;
        
        private DisposableResource() { }
        public static DisposableResource<T> New(T resource)
        {
            _pool ??= new Stack<DisposableResource<T>>();
            var instance = _pool.Count > 0 ? _pool.Pop() : new DisposableResource<T>();
            
            instance.Resource = resource;
            instance._isDisposed = false;
            
            return instance;
        }
        
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Resource?.Dispose();
            Resource = default;
            _isDisposed = true;
            _pool ??= new Stack<DisposableResource<T>>();
            _pool.Push(this);
        }

    }
}