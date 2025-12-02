using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a resource loaded from the Addressables API.
    /// </summary>
    internal sealed class AddressableResource<T> : IResource<T>
    {
        [ThreadStatic]
        private static Stack<AddressableResource<T>> _pool;
        
        public T Resource => _operationHandle.IsValid() ? _operationHandle.Result : default;
        
        private AsyncOperationHandle<T> _operationHandle;
        
        private AddressableResource() { }
        public static AddressableResource<T> New(AsyncOperationHandle<T> operationHandle)
        {
            _pool ??= new Stack<AddressableResource<T>>();
            var instance = _pool.Count > 0 ? _pool.Pop() : new AddressableResource<T>();
            
            instance._operationHandle = operationHandle;
            
            return instance;
        }

        public void Dispose()
        {
            if (!_operationHandle.IsValid())
            {
                return;
            }

            Addressables.Release(_operationHandle);
            _operationHandle = default;
            _pool ??= new Stack<AddressableResource<T>>();
            _pool.Push(this);
        }
    }
}
