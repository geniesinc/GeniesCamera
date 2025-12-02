using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Component that acts as a reference between its GameObject and an <see cref="IGenie"/> instance. It also comes with a custom editor
    /// that provides handy testing functionality.
    /// </summary>
    public sealed class GenieReference : MonoBehaviour
    {
        public IGenie Genie { get; private set; }
        
        private bool _disposeOnDestroy;

        public static GenieReference Create(IGenie genie, GameObject target, bool disposeOnDestroy = false)
        {
            if (genie is null || !target)
            {
                return null;
            }

            var reference = target.GetComponent<GenieReference>();
            if (!reference)
            {
                reference = target.AddComponent<GenieReference>();
            }

            reference.Genie = genie;
            reference._disposeOnDestroy = disposeOnDestroy;
            
            return reference;
        }
        
        private void OnDestroy()
        {
            if (_disposeOnDestroy)
            {
                Genie?.Dispose();
            }

            Genie = null;
        }
    }
}
