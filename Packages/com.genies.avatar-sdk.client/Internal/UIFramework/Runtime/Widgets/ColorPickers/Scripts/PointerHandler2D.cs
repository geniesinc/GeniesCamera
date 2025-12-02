using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Handles pointer input events and provides interfaces for its normalized position value within the target rect.
    /// </summary>
    [RequireComponent(typeof(MaskableGraphic))]
    public class PointerHandler2D : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private RectTransform targetRectTransform;

        [System.Serializable]
        public class PointerValueChangedEvent : UnityEvent<Vector2> { }

        /// <summary>
        /// Event when the 2D pointer position changes. Invokes with Vector2 values normalized to 0-1.
        /// </summary>
        [SerializeField]
        public PointerValueChangedEvent onValueChanged = new PointerValueChangedEvent();

        private Vector2 _normalizedPosition;

        public Vector2 NormalizedPosition
        {
            get => _normalizedPosition;
            private set
            {
                if (_normalizedPosition != value)
                {
                    _normalizedPosition = value;
                    onValueChanged.Invoke(_normalizedPosition);
                }
            }
        }

        private void Start()
        {
            if (targetRectTransform == null)
            {
                targetRectTransform = GetComponent<RectTransform>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateNormalizedPosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateNormalizedPosition(eventData.position);
        }

        private void UpdateNormalizedPosition(Vector2 pointerPosition)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRectTransform, pointerPosition, null, out Vector2 localPoint))
            {
                Vector2 normalizedPosition = Rect.PointToNormalized(targetRectTransform.rect, localPoint);
                NormalizedPosition = normalizedPosition;
            }
        }
    }
}
