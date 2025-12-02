using System;
using System.Collections.Generic;
using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Handles getting/adding/removing <see cref="GenieComponent"/>s from a <see cref="IGenie"/> instance.
    /// </summary>
    public sealed class GenieComponentManager
    {
        public IReadOnlyList<GenieComponent> All => _components;
        public int Count => _components.Count;
        
        public event Action<GenieComponent> ComponentAdded;
        public event Action<GenieComponent> ComponentRemoved;

        // dependencies
        private readonly IGenie _genie;
        
        // state
        private readonly List<GenieComponent> _components;
        
        // helpers
        private readonly List<GenieComponent> _tmpComponents;

        public GenieComponentManager(IGenie genie)
        {
            _genie = genie;
            
            _components = new List<GenieComponent>();
            
            _tmpComponents = new List<GenieComponent>();
        }

        public bool TryGet<T>(out T result)
            where T : GenieComponent
        {
            foreach (GenieComponent component in _components)
            {
                if (component is not T tComponent)
                {
                    continue;
                }

                result = tComponent;
                return true;
            }
            
            result = default;
            return false;
        }

        public List<T> GetAll<T>()
            where T : GenieComponent
        {
            var results = new List<T>(_components.Count);
            GetAll<T>(results);
            return results;
        }

        public void GetAll<T>(ICollection<T> results)
            where T : GenieComponent
        {
            if (results is null)
            {
                return;
            }

            foreach (GenieComponent component in _components)
            {
                if (component is T tComponent)
                {
                    results.Add(tComponent);
                }
            }
        }
        
        public bool Add<T>(bool notify = true)
            where T : GenieComponent, new()
        {
            return Add(new T(), notify);
        }

        public bool Add(GenieComponent component, bool notify = true)
        {
            if (component is null)
            {
                return false;
            }

            if (component.Genie is not null)
            {
                Debug.LogError($"[{nameof(GenieComponentManager)}] cannot add component {component.Name} ({component.GetType().Name}) because it is already added to another genie: {component.Genie.Root?.name}");
                return false;
            }

            if (component.Genie == _genie)
            {
                Debug.LogWarning($"[{nameof(GenieComponentManager)}] component {component.Name} ({component.GetType().Name}) is already added to this genie: {_genie.Root?.name}");
                return false;
            }

            try
            {
                if (!component.TryAdd(_genie, notify))
                {
                    Debug.LogWarning($"[{nameof(GenieComponentManager)}] component {component.Name} ({component.GetType().Name}) will not be added because it failed to initialize");
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(GenieComponentManager)}] exception thrown when adding the component: {component.Name} ({component.GetType().Name})\n{exception}");
                return false;
            }
            
            _components.Add(component);
            
            if (notify)
            {
                ComponentAdded?.Invoke(component);
            }

            return true;
        }

        public void Remove(GenieComponent component, bool notify = true)
        {
            if (component is null || component.Genie != _genie)
            {
                return;
            }

            int index = _components.IndexOf(component);
            if (index < 0 || index >= _components.Count)
            {
                return;
            }

            _components.RemoveAt(index);
            SafeRemove(component, notify);
            
            if (notify)
            {
                ComponentRemoved?.Invoke(component);
            }
        }

        /// <summary>
        /// Will remove all components of the given type.
        /// </summary>
        public void Remove<T>(bool notify = true)
            where T : GenieComponent
        {
            // gather all components that match the type in the tmp components list
            _tmpComponents.Clear();
            foreach (GenieComponent component in _components)
            {
                if (component is T)
                {
                    _tmpComponents.Add(component);
                }
            }
            
            // remove matched components
            foreach (GenieComponent component in _tmpComponents)
            {
                Remove(component);
            }

            _tmpComponents.Clear();
        }
        
        public bool Add<T>(IEnumerable<T> components, bool notify = true)
            where T : GenieComponent
        {
            if (components is null)
            {
                return true;
            }

            bool allWereAdded = true;
            foreach (T component in components)
            {
                if(component is not null && !Add(component, notify))
                {
                    allWereAdded = false;
                }
            }
            
            return allWereAdded;
        }
        
        public void Remove<T>(IEnumerable<T> components, bool notify = true)
            where T : GenieComponent
        {
            foreach (T component in components)
            {
                Remove(component, notify);
            }
        }

        public void RemoveAll(bool notify = true)
        {
            // gather all components in the tmp list
            _tmpComponents.Clear();
            _tmpComponents.AddRange(_components);
            
            // remove matched components
            foreach (GenieComponent component in _tmpComponents)
            {
                Remove(component, notify);
            }

            _tmpComponents.Clear();
        }

        public List<JToken> SerializeAll()
        {
            var tokens = new List<JToken>(_components.Count);
            SerializeAll(tokens);
            return tokens;
        }

        public void SerializeAll(ICollection<JToken> results)
        {
            foreach (GenieComponent component in _components)
            {
                if (!component.ShouldSkipSerialization && SerializerAs<GenieComponent>.TrySerialize(component, out JToken token))
                {
                    results.Add(token);
                }
            }
        }
        
        public void DeserializeAndAdd(JToken token)
        {
            if (SerializerAs<GenieComponent>.TryDeserialize(token, out GenieComponent component))
            {
                Add(component);
            }
        }

        public void DeserializeAndAdd(IEnumerable<JToken> tokens)
        {
            foreach (JToken token in tokens)
            {
                DeserializeAndAdd(token);
            }
        }
        
        private static void SafeRemove(GenieComponent component, bool notify)
        {
            try
            {
                component.Remove(notify);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(GenieComponentManager)}] exception thrown when removing the component: {component.Name} ({component.GetType().Name})\n{exception}");
            }
        }
    }
}