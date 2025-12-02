using UnityEngine;

namespace Genies.UI.Animations
{
    /// <summary>
    /// MonoBehaviour component that hosts coroutines for animation operations.
    /// Automatically added to GameObjects that need animating.
    /// </summary>
    public class AnimationHost : MonoBehaviour
    {
        private void OnDestroy()
        {
            // Clean up any active animations when the host is destroyed
            GeniesUIAnimation.TerminateAnimations(this);
        }
    }
}

