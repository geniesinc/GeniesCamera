using UnityEngine;

namespace Genies.CameraSystem.Focusable
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "CameraFocusPoint", menuName = "Genies/CameraFocusPoint")]
#endif
    public class CameraFocusPoint : ScriptableObject, IFocusable
    {
        public Bounds Bounds;
        public Vector3 ViewDirection;
        public Vector3 TargetViewDirection => ViewDirection;
        public Bounds GetBounds() => Bounds;
    }
}
