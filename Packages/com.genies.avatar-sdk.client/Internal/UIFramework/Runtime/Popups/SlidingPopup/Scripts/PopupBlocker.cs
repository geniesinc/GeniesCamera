using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Genies.UIFramework
{
    public class PopupBlocker : MonoBehaviour, IPointerDownHandler
    {
        public event Action OnBlockerClicked;

        private bool _active = true;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_active)
            {
                return;
            }

            OnBlockerClicked?.Invoke();
        }

        /// <summary>
        /// Manage whether the user can click on the blocker or not.
        /// </summary>
        /// <param name="isActive"> If true, the blocker will receive input</param>
        public void SetBlockerActive(bool isActive)
        {
            _active = isActive;
        }
    }
}
