using System;
using System.Collections.Generic;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a resource that depends on other resources.
    /// </summary>
    internal sealed class DependentResource<T> : IResource<T>
    {
        [ThreadStatic]
        private static Stack<DependentResource<T>> _pool;

        public T Resource => _resourceRef.Item;

        private Ref<T> _resourceRef;
        private readonly List<Ref> _dependencies = new List<Ref>();
        private bool _isDisposed;

        private DependentResource() { }

        public static DependentResource<T> New(Ref<T> reference, params Ref[] dependencies)
            => New(reference, dependencies as IEnumerable<Ref>);

        public static DependentResource<T> New(Ref<T> reference, IEnumerable<Ref> dependencies)
        {
            _pool ??= new Stack<DependentResource<T>>();
            var instance = _pool.Count > 0 ? _pool.Pop() : new DependentResource<T>();

            instance._resourceRef = reference;
            instance._dependencies.Clear();
            instance._dependencies.AddRange(dependencies);
            instance._isDisposed = false;

            return instance;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (Ref dependency in _dependencies)
            {
                dependency.Dispose();
            }

            _dependencies.Clear();
            _resourceRef.Dispose();
            _resourceRef = default;
            _isDisposed = true;
            _pool ??= new Stack<DependentResource<T>>();
            _pool.Push(this);
        }
    }
}
