using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Genies.UI
{
    public class PointerEventDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action OnDragEnded;
        public Action OnDragStarted;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            OnDragStarted?.Invoke();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            OnDragEnded?.Invoke();
        }
    }
}
