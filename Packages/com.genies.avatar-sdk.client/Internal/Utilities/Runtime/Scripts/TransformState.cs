using UnityEngine;

namespace Genies.Utilities
{
    public readonly struct TransformState
    {
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;
            
        public TransformState(Transform transform)
        {
            Parent = transform.parent;
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
        }

        public void SetTo(Transform transform)
        {
            transform.SetParent(Parent, worldPositionStays: false);
            transform.localPosition = LocalPosition;
            transform.localRotation = LocalRotation;
            transform.localScale = LocalScale;
        }
    }
}
