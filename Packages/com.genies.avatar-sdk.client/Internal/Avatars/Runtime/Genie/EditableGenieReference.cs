using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Component that acts as a reference between its GameObject and an <see cref="IEditableGenie"/> instance.
    /// </summary>
    public sealed class EditableGenieReference : MonoBehaviour
    {
        public IEditableGenie Genie { get; private set; }
        
        private bool _disposeOnDestroy;

        public static EditableGenieReference Create(IEditableGenie genie, GameObject target, bool disposeOnDestroy = false)
        {
            if (genie is null || !target)
            {
                return null;
            }

            var reference = target.GetComponent<EditableGenieReference>();
            if (!reference)
            {
                reference = target.AddComponent<EditableGenieReference>();
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
