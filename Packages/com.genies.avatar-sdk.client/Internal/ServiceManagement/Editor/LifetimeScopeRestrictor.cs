using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Genies.ServiceManagement.Editor
{
    /// <summary>
    /// The framework doens't allow using <see cref="LifetimeScope"/> as is, this
    /// editor will remove the component if it was <see cref="GeniesLifetimeScope"/> or any of its
    /// derivatives
    /// </summary>
    [CustomEditor(typeof(LifetimeScope), true)] 
    public class LifetimeScopeRestrictor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            var component = (LifetimeScope)target;

            if(component != null && !Application.isPlaying)
            {
                DestroyImmediate(component);
                throw new System.Exception($"You can't Add LifetimeScope or any of its derivatives in Editor. " +
                                           $"\n Use ServiceManager.InitializeCustomScopeAsync to install scopes into scenes or GameObjects");
            }
        }
    }
}
