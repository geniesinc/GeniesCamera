using System;
using UnityEngine;

namespace Genies.ServiceManagement
{
    [DisallowMultipleComponent]
    internal class ScopeOwnerLifetimeEventHandler : MonoBehaviour
    {
        public event Action OnDestroyed;
        
        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
}
