using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Genies.UIFramework {
    [RequireComponent(typeof(RectTransform))]
    public class InteractionController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("References")]
        public RectTransform RectTransform;
        public GameObject Controllable;

        [Header("Options")]
        public Vector2 InertiaSettleTimeRange;
        public Vector2 DragReleaseAngularVelocityInterpolationRange;
        public AnimationCurve DragVelocityCurve;
        public float DegreesPerPixel = 1f;
        public float MaxAngularVelocity = 30f;

        public bool IsEnabled { get; private set; } = true;

        protected float _angularVelocity = 0f;
        protected bool _dragging = false;
        protected float _dragReleaseTime = 0;
        protected float _dragReleaseAngularVelocity = 0;

        public UnityEvent PointerClick { get; private set; } = new UnityEvent();

        protected void OnValidate() {
            if (RectTransform == null)
            {
                RectTransform = GetComponent<RectTransform>();
            }
        }

        public void SetEnabled(bool enable)
        {
            SetEnabledWithoutResetting(enable);
            if (!IsEnabled)
            {
                Reset();
            }
        }

        /// <summary>
        /// Disables the interaction controls
        /// without resetting the <see cref="Controllable"/>'s rotation.
        /// </summary>
        /// <param name="enable">whether to set it enable or disable</param>
        public void SetEnabledWithoutResetting(bool enable)
        {
            if (enable == IsEnabled)
            {
                return;
            }

            IsEnabled = enable;
        }

        public void Reset() {
            _dragging = false;
            _angularVelocity = 0f;
            _dragReleaseTime = 0f;
            _dragReleaseAngularVelocity = 0;
            if (Controllable)
            {
                Controllable.transform.rotation = Quaternion.identity;
            }
        }

        public void SmoothReset() {
            _dragging = false;
            if (Controllable)
            {
                // Smooth spring rotation back to identity
                Controllable.transform.SpringRotation(Quaternion.identity, SpringPhysics.Presets.Smooth).OnCompletedOneShot(Reset);
            }
        }

        public void OnBeginDrag(PointerEventData eventData) {
        }

        public void OnDrag(PointerEventData eventData) {
            if (!IsEnabled)
            {
                return;
            }

            var delta = eventData.delta;
            var rotationAngle = delta.x * DegreesPerPixel;

            if (Controllable)
            {
                Controllable.transform.Rotate(Vector3.down, rotationAngle);
            }

            _angularVelocity = rotationAngle / Time.deltaTime;
            _angularVelocity = Mathf.Clamp(_angularVelocity, -MaxAngularVelocity, MaxAngularVelocity);
        }

        public void OnEndDrag(PointerEventData eventData) {

        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!IsEnabled)
            {
                return;
            }

            _dragging = true;
            _angularVelocity = 0;
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (!IsEnabled)
            {
                return;
            }

            _dragging = false;
            _dragReleaseTime = Time.time;
            _dragReleaseAngularVelocity = _angularVelocity;
        }

        protected void Update() {
            if (!IsEnabled || !Controllable)
            {
                return;
            }

            if (Mathf.Abs(_angularVelocity) > 0 && _dragging == false) {
                float normalizedReleaseVelocity = Mathf.InverseLerp(DragReleaseAngularVelocityInterpolationRange.x, DragReleaseAngularVelocityInterpolationRange.y, Mathf.Abs(_dragReleaseAngularVelocity));
                float dragTime = Mathf.Lerp(InertiaSettleTimeRange.x, InertiaSettleTimeRange.y, normalizedReleaseVelocity);
                float timeSinceDrag = Time.time - _dragReleaseTime;
                float normalizedTime = Mathf.InverseLerp(0, dragTime, timeSinceDrag);

                _angularVelocity = _dragReleaseAngularVelocity * DragVelocityCurve.Evaluate(normalizedTime);
                Controllable.transform.Rotate(Vector3.down, _angularVelocity * Time.deltaTime);
            }
        }

        public void SetInteractionZone(Vector2 anchorMin, Vector2 anchorMax) {
            RectTransform.anchorMin = anchorMin;
            RectTransform.anchorMax = anchorMax;
            RectTransform.offsetMax = Vector2.zero;
            RectTransform.offsetMin = Vector2.zero;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (!eventData.dragging) {
                PointerClick.Invoke();
            }
        }
    }
}
