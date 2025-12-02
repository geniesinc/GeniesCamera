using Genies.CameraSystem.Focusable;
using UnityEngine;

namespace Genies.CameraSystem
{
    public class FocusableComponent : MonoBehaviour, IFocusable
    {
        public bool IsBoundsCalculated { get; private set; }
        public float ViewAngle;
        public bool GetBoundsFromChildRenderers = true;
        public Bounds Bounds;
        public Vector3 TargetViewDirection { get; set; }

        private float _prevAngle;

        private void Start()
        {
            var center = GetBounds().center;
            TargetViewDirection = new Vector3(0, center.y, 1);
        }

        protected void Update()
        {
            UpdateTargetViewDirection();
        }

        private void UpdateTargetViewDirection()
        {
            if (_prevAngle != ViewAngle)
            {
                var center = GetBounds().center;
                TargetViewDirection = Quaternion.Euler(0, ViewAngle, 0) * new Vector3(0, center.y, 1);
                ViewAngle = ViewAngle >= 360 ? 0 : ViewAngle;
                ViewAngle = ViewAngle < 0 ? 360 : ViewAngle;
                _prevAngle = ViewAngle;
            }
        }

        public Bounds GetBounds()
        {
            if (!IsBoundsCalculated && GetBoundsFromChildRenderers)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (Renderer r in renderers)
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                    IsBoundsCalculated = true;
                    Bounds = bounds;
                }
            }
            else if (!GetBoundsFromChildRenderers)
            {
                var prevExtents = Bounds.extents;
                Bounds = Bounds.center != transform.position ? new Bounds(transform.position, Bounds.extents) : Bounds;
                Bounds.extents = prevExtents;
                _prevAngle = 0;
            }

            return Bounds;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            var center = GetBounds().center;
            Vector3 direction = center - TargetViewDirection;
            Vector3 right = Vector3.Cross(direction.normalized, Vector3.up);
            Vector3 up = Vector3.Cross(right.normalized, direction.normalized);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, TargetViewDirection);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(center, up);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Bounds.center, Bounds.extents);
            UpdateTargetViewDirection();
        }
#endif
    }
}
