using System;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Generic implementation of <see cref="IGenieSnapshot"/> that receives the resources as a disposable instance.
    /// </summary>
    public sealed class GenieSnapshot : MonoBehaviour, IGenieSnapshot
    {
        public string Species { get; private set; }
        public GameObject Root { get; private set; }
        public bool IsDisposed { get; private set; }

        public event Action Disposed;

        private bool _initialized;

        public void Initialize(string species, GameObject root, Action onDisposedCallback = null)
        {
            if (_initialized || IsDisposed)
            {
                onDisposedCallback?.Invoke();
                Debug.LogError($"[{nameof(GenieSnapshot)}] already initialized");
                return;
            }

            _initialized = true;
            Species = species;
            Root = root;
            IsDisposed = false;
            
            if (onDisposedCallback is not null)
            {
                Disposed += onDisposedCallback;
            }
        }

        public void Dispose()
        {
            if (!_initialized || IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            
            if (gameObject)
            {
                Destroy(gameObject);
            }

            Disposed?.Invoke();
        }

        private void OnDestroy()
        {
            // make sure all resources are released if the snapshot component is destroyed
            Dispose();
        }
    }
}
