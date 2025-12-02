using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Utility for fast and easy access to a collection of animator parameters. I implemented this since Unity doesn't
    /// provide a proper way to check if a parameter exists within an Animator.
    /// </summary>
    public sealed class AnimatorParameters : IReadOnlyList<AnimatorControllerParameter>
    {
        /// <summary>
        /// The hash code that represents current parameters with their configuration. It is updated when
        /// <see cref="Refresh"/> is called.
        /// </summary>
        public int Hash { get; private set; }
        public int Count => _parameters.Count;
        
        public AnimatorControllerParameter this[int index] => _parameters[index];
        
        /// <summary>
        /// The animator that this <see cref="AnimatorParameters"/> instance represents. <see cref="Refresh"/> needs to
        /// be called if the animator parameters were updated (i.e.: if the animator controller changed).
        /// </summary>
        public readonly Animator Animator;
        
        private readonly List<AnimatorControllerParameter>            _parameters;
        private readonly Dictionary<int, AnimatorControllerParameter> _parametersByHash;
        
        public AnimatorParameters(Animator animator, bool refresh = true)
        {
            Animator = animator;
            _parameters = new List<AnimatorControllerParameter>();
            _parametersByHash = new Dictionary<int, AnimatorControllerParameter>();
            
            if (refresh)
            {
                Refresh();
            }
        }
        
        /// <summary>
        /// Refreshes all parameters from the animator that this instance is tied to.
        /// </summary>
        public void Refresh()
        {
            AnimatorControllerParameter lastParameter = null;
            try
            {
                _parameters.Clear();
                _parametersByHash.Clear();
                
                // Check if animator is properly initialized before accessing parameters
                if (Animator == null)
                {
                    Debug.LogWarning($"Animator is null, cannot refresh parameters");
                    return;
                }
                
                if (!Animator.isActiveAndEnabled)
                {
                    Debug.LogWarning($"Animator is not active or enabled, skipping parameter refresh");
                    return;
                }
                
                if (Animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"Animator has no RuntimeAnimatorController, cannot refresh parameters");
                    return;
                }
                
                AnimatorControllerParameter[] parameters;
                try
                {
                    parameters = Animator.parameters;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception accessing Animator.parameters: {ex.Message}");
                    return;
                }
                
                if (parameters == null || parameters.Length == 0)
                {
                    Debug.LogWarning($"Animator has no parameters to refresh");
                    return;
                }
                
                _parameters.AddRange(parameters);

                foreach (AnimatorControllerParameter parameter in _parameters)
                {
                    lastParameter = parameter;
                    _parametersByHash.Add(parameter.nameHash, parameter);
                }
            }
            catch (Exception)
            {
                Debug.LogError($"Error refreshing parameters. Last parameter: {lastParameter?.name ?? "unknown"}");
            }
        }

        /// <summary>
        /// Hashes all parameters with their current configurations. If two different <see cref="AnimatorParameters"/>
        /// instances return the same hash from this method it means that they both have the same collection of
        /// parameters with the same default values. The order of the parameters within the list returned by the
        /// animator will affect the hash, so make sure that your animator controllers have the same order if you need
        /// to compare them.
        /// </summary>
        public int HashParameters()
        {
            int hash = HashCode.Combine(0);
            foreach (AnimatorControllerParameter parameter in _parameters)
            {
                // hash the parameter without using its GetHashCode method as that one only hashes the name
                int parameterHash = HashCode.Combine(parameter.name, parameter.type, parameter.defaultBool, parameter.defaultFloat, parameter.defaultInt);
                hash = HashCode.Combine(hash, parameterHash);
            }
    
            return hash;
        }
        
        public bool TryGet(int id, out AnimatorControllerParameter parameter)
            => _parametersByHash.TryGetValue(id, out parameter);
        
        public bool TryGet(string name, out AnimatorControllerParameter parameter)
            => _parametersByHash.TryGetValue(Animator.StringToHash(name), out parameter);
        
        public bool Contains(int id)
            => _parametersByHash.ContainsKey(id);
        
        public bool Contains(string name)
            => _parametersByHash.ContainsKey(Animator.StringToHash(name));

        public IEnumerator<AnimatorControllerParameter> GetEnumerator()
            => ((IEnumerable<AnimatorControllerParameter>)_parameters).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _parameters.GetEnumerator();
    }
}
