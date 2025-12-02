using UnityEngine;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// A Unity component that represents an avatar overlay which can be attached to avatar bones.
    /// This component is used to add additional geometry or effects to avatars at runtime.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class AvatarOverlay : MonoBehaviour
#else
    public sealed class AvatarOverlay : MonoBehaviour
#endif
    {
        internal bool TryGetRoot(out Transform root)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                if (child.name != "Root")
                {
                    continue;
                }
                root = child;
                return true;
            }

            root = null;
            return false;
        }
    }
}
