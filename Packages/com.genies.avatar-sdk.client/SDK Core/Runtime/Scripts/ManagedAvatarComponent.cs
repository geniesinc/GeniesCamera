using UnityEngine;

namespace Genies.Sdk
{
    /// <summary>
    /// MonoBehaviour component that provides a bridge between Unity's GameObject system and the ManagedAvatar wrapper.
    /// This component is automatically attached to the avatar's root GameObject and maintains a bidirectional reference
    /// with its corresponding ManagedAvatar instance.
    /// </summary>
    public class ManagedAvatarComponent : MonoBehaviour
    {
        /// <summary>
        /// Reference to the ManagedAvatar wrapper instance that owns this component.
        /// </summary>
        public ManagedAvatar ManagedAvatar { get; internal set; }

        /// <summary>
        /// Gets whether this component is currently disposing or has been disposed.
        /// </summary>
        public bool IsDisposing { get; private set; }

        private void OnDestroy()
        {
            // Prevent re-entrant calls and ensure disposal happens only once
            if (IsDisposing)
            {
                return;
            }

            IsDisposing = true;

            // Let ManagedAvatar handle its own disposal (it's idempotent)
            ManagedAvatar?.Dispose();
            ManagedAvatar = null;
        }
    }
}

