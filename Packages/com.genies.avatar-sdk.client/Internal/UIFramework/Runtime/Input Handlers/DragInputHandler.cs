using UnityEngine;
using UnityEngine.EventSystems;

namespace Genies.UI
{
    public class DragInputHandler : UIBehaviour,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, UnityEngine.EventSystems.IScrollHandler
    {
        public DragInputHandler MasterHandler;
        
        public int PointerId { get; private set; }
        public bool PointerPressed { get; private set; } // whether or not the pointer was pressed down (on this object) during this frame
        public bool PointerDown { get; private set; } // wether or not the pointer is currently pressed down on this object
        public bool PointerReleased { get; private set; } // whether or not the pointer was pressed up (on this object) during this frame
        public Vector2 PointerPos { get; private set; }
        public Vector2 PointerDownPos { get; private set; } // pointer position when it was pressed down
        public Vector2 DragDelta { get; private set; } // the current drag delta at this frame
        public Vector2 ScrollDelta { get; private set; } // scroll delta at this frame from scroll input (like the mouse wheel)

        public RectTransform RectTransform => _rectTransform ??= GetComponent<RectTransform>();

        private RectTransform _rectTransform;
        private bool _registeredDown;

        public virtual void OnPointerDown(int pointerId, Vector2 position)
        {
            if (PointerDown)
            {
                return;
            }

            PointerId = pointerId;
            PointerPos = RectTransform.InverseTransformPoint(position);
            PointerDownPos = PointerPos;
            PointerPressed = true;
            PointerDown = true;
            _registeredDown = true;
        }

        public virtual void OnPointerUp(int pointerId, Vector2 position)
        {
            if (pointerId != PointerId)
            {
                return;
            }

            PointerPos = RectTransform.InverseTransformPoint(position);
            PointerReleased = true;
            PointerDown = false;
            _registeredDown = false;
        }

        public virtual void OnBeginDrag(int pointerId, Vector2 position, Vector2 delta)
        {
            if (!_registeredDown)
            {
                PointerId = pointerId;
                PointerDownPos = RectTransform.InverseTransformPoint(position);
                PointerPressed = true;
                PointerDown = true;
            }
            else if (pointerId != PointerId)
            {
                return;
            }

            PointerPos = RectTransform.InverseTransformPoint(position);
            DragDelta = RectTransform.InverseTransformVector(delta);
        }

        public virtual void OnDrag(int pointerId, Vector2 position, Vector2 delta)
        {
            if (pointerId != PointerId)
            {
                return;
            }

            PointerPos = RectTransform.InverseTransformPoint(position);
            DragDelta = RectTransform.InverseTransformVector(delta);
        }

        public virtual void OnEndDrag(int pointerId, Vector2 position)
        {
            if (pointerId != PointerId)
            {
                return;
            }

            PointerPos = RectTransform.InverseTransformPoint(position);
            PointerReleased = true;
            PointerDown = false;
            _registeredDown = false;
        }

        public virtual void OnScroll(Vector2 scrollDelta)
        {
            ScrollDelta = scrollDelta;
        }

        protected virtual void LateUpdate()
        {
            PointerPressed = false;
            PointerReleased = false;
            DragDelta = Vector2.zero;
            ScrollDelta = Vector2.zero;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (MasterHandler is null)
            {
                OnPointerDown(eventData.pointerId, eventData.position);
            }
            else
            {
                MasterHandler.OnPointerDown(this, eventData);
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (MasterHandler is null)
            {
                OnPointerUp(eventData.pointerId, eventData.position);
            }
            else
            {
                MasterHandler.OnPointerUp(this, eventData);
            }
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (MasterHandler is null)
            {
                OnBeginDrag(eventData.pointerId, eventData.position, eventData.delta);
            }
            else
            {
                MasterHandler.OnBeginDrag(this, eventData);
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (MasterHandler is null)
            {
                OnDrag(eventData.pointerId, eventData.position, eventData.delta);
            }
            else
            {
                MasterHandler.OnDrag(this, eventData);
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (MasterHandler is null)
            {
                OnEndDrag(eventData.pointerId, eventData.position);
            }
            else
            {
                MasterHandler.OnEndDrag(this, eventData);
            }
        }

        public virtual void OnScroll(PointerEventData eventData)
        {
            if (MasterHandler is null)
            {
                OnScroll(eventData.scrollDelta);
            }
            else
            {
                MasterHandler.OnScroll(this, eventData);
            }
        }
        
        public virtual void OnPointerDown(DragInputHandler slaveHandler, PointerEventData eventData)
            => OnPointerDown(eventData.pointerId, eventData.position);

        public virtual void OnPointerUp(DragInputHandler slaveHandler, PointerEventData eventData)
            => OnPointerUp(eventData.pointerId, eventData.position);

        public virtual void OnBeginDrag(DragInputHandler slaveHandler, PointerEventData eventData)
            => OnBeginDrag(eventData.pointerId, eventData.position, eventData.delta);

        public virtual void OnDrag(DragInputHandler slaveHandler, PointerEventData eventData)
            => OnDrag(eventData.pointerId, eventData.position, eventData.delta);

        public virtual void OnEndDrag(DragInputHandler slaveHandler, PointerEventData eventData)
            => OnEndDrag(eventData.pointerId, eventData.position);

        public virtual void OnScroll(DragInputHandler slaveHandler, PointerEventData eventData)
            => OnScroll(eventData.scrollDelta);
    }
}
