using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a resource derived from UnityEngine.Object.
    /// </summary>
    internal sealed class UnityResource<T> : IResource<T>
        where T : Object
    {
        [ThreadStatic]
        private static Stack<UnityResource<T>> _pool;

        public T Resource { get; private set; }

        private bool _isUnityResource;
        private bool _isDisposed;
        
        private UnityResource() { }
        public static UnityResource<T> New(T asset, bool isUnityResource = false)
        {
            _pool ??= new Stack<UnityResource<T>>();
            var instance = _pool.Count > 0 ? _pool.Pop() : new UnityResource<T>();
            
            instance.Resource = asset;
            instance._isUnityResource = isUnityResource;
            instance._isDisposed = false;
            
            return instance;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // if the resource is disposed in a background thread then this makes sure we destroy/release it on the main Unity thread
            DisposeOnMainThread(Resource, _isUnityResource).Forget();
            
            _isDisposed = true;
            Resource = null;
            _pool ??= new Stack<UnityResource<T>>();
            _pool.Push(this);
        }
        
        private static async UniTaskVoid DisposeOnMainThread(T asset, bool isUnityResource)
        {
            if (!PlayerLoopHelper.IsMainThread)
            {
                await UniTask.SwitchToMainThread();
            }

            if (!asset)
            {
                return;
            }

            if (isUnityResource)
            {
                Resources.UnloadAsset(asset);
            }
            else
            {
                DestroyAsset(asset);
            }
        }
        
        private static void DestroyAsset(T asset)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Object.Destroy(asset);
            else
                Object.DestroyImmediate(asset);
#else
                Object.Destroy(asset);
#endif
        }
    }
}
