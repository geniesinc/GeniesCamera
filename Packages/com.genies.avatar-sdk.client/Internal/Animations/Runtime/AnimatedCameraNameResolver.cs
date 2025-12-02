using UnityEngine;

namespace Genies.Animations
{
    /// <summary>
    /// Resolves and sets the name of animated cameras used in looks animations.
    /// This MonoBehaviour component automatically assigns the correct camera name when enabled, ensuring proper camera identification for animation systems.
    /// </summary>
    public class AnimatedCameraNameResolver : MonoBehaviour
    {
        private const string CurrentCameraName = "animatedCamera";

        private void OnEnable()
        {
            name = GetCameraName();
        }

        private string GetCameraName()
        {
            return CurrentCameraName;
        }
    }
}
