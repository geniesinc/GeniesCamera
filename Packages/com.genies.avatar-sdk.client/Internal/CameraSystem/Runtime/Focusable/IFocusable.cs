using UnityEngine;

namespace Genies.CameraSystem.Focusable
{
    public interface IFocusable
    {
        public Vector3 TargetViewDirection { get;}
        public Bounds GetBounds();
    }
}
