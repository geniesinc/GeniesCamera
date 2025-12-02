using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    /// <summary>
    /// Can "convert" any GameObject into a runtime prefab. Which means that it behaves as similar as possible to prefabs
    /// (not shown in the scene hierarchy, not unloading when loading other scenes, not visible, etc). In reality this
    /// is not exactly the same as prefabs which are set to a null scene, but Unity won't allow us to create such
    /// GameObject instances at runtime.
    /// <br/><br/>
    /// Once you have created a runtime prefab you should no longer directly manipulate the source GameObject.
    /// </summary>
    public sealed class RuntimePrefab<T> : IDisposable
        where T : Object
    {
        private readonly T _prefab;
        private readonly bool _active;

        public RuntimePrefab(T prefab)
        {
            if (!prefab)
            {
                throw new Exception("Cannot create a runtime prefab from a null or destroyed Object");
            }

            var gameObject = prefab as GameObject;
            if (!gameObject)
            {
                if (prefab is not Component component)
                {
                    throw new Exception("You can only create runtime prefabs for GameObjects or component types");
                }

                gameObject = component.gameObject;
            }
            
            _prefab = prefab;
            _active = gameObject.activeSelf;
            
            // deactivate, set hideFlags so it doesn't show in hierarchy and mark the object o not be destroyed when loading new scenes
            gameObject.SetActive(false);
            gameObject.hideFlags |= HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(gameObject);
        }

        public T Instantiate()
        {
            T instance = Object.Instantiate(_prefab);
            RestoreActiveState(instance);
            return instance;
        }
        
        public T Instantiate(Transform parent)
        {
            T instance = Object.Instantiate(_prefab, parent);
            RestoreActiveState(instance);
            return instance;
        }
        
        public T Instantiate(Transform parent, bool worldPositionStays)
        {
            T instance = Object.Instantiate(_prefab, parent, worldPositionStays);
            RestoreActiveState(instance);
            return instance;
        }
        
        public T Instantiate(Vector3 position, Quaternion rotation)
        {
            T instance = Object.Instantiate(_prefab, position, rotation);
            RestoreActiveState(instance);
            return instance;
        }
        
        public T Instantiate(Vector3 position, Quaternion rotation, Transform parent)
        {
            T instance = Object.Instantiate(_prefab, position, rotation, parent);
            RestoreActiveState(instance);
            return instance;
        }

        public void Destroy()
        {
            if (_prefab)
            {
                Object.Destroy(_prefab);
            }
        }
        
        public void DestroyImmediate()
        {
            if (_prefab)
            {
                Object.DestroyImmediate(_prefab);
            }
        }
        
        public void Dispose()
        {
            if (!_prefab)
            {
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy();
            else
                DestroyImmediate();
#else
            Destroy();
#endif
        }

        private void RestoreActiveState(T instance)
        {
            if (instance is GameObject gameObject)
            {
                gameObject.SetActive(_active);
            }
            else if (instance is Component component)
            {
                component.gameObject.SetActive(_active);
            }
        }
    }
}
