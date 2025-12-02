using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Genies.CameraSystem
{
    public class SwipeDetectionEnabler : MonoBehaviour
    {
        public Action<Vector2> SwipeDetected;

        [SerializeField] private InputActionReference pressActionReference;
        [SerializeField] private InputActionReference currentScreenPosActionReference;
        [SerializeField] private float minimumDistance = 0.2f;

        [Range(0f, 1f)] [SerializeField] public float directionThreshold = 0.9f;

        private Vector2 _startPosition;

        // Start is called before the first frame update
        private void OnEnable()
        {
            pressActionReference.action.Enable();
            currentScreenPosActionReference.action.Enable();

            pressActionReference.action.performed += OnStartSwipe;
            pressActionReference.action.canceled += OnEndSwipe;
        }

        private void OnDisable()
        {
            pressActionReference.action.performed -= OnStartSwipe;
            pressActionReference.action.canceled -= OnEndSwipe;

            pressActionReference.action.Disable();
            currentScreenPosActionReference.action.Disable();
        }

        /// <summary>
        /// Start reading input when swipe has started
        /// </summary>
        /// <param name="ctx"></param>
        private void OnStartSwipe(InputAction.CallbackContext ctx)
        {
            _startPosition = currentScreenPosActionReference.action.ReadValue<Vector2>();
        }

        /// <summary>
        /// Detects direction when swipe has ended
        /// </summary>
        /// <param name="ctx"></param>
        private void OnEndSwipe(InputAction.CallbackContext ctx)
        {
            Vector2 currentPosition = currentScreenPosActionReference.action.ReadValue<Vector2>();

            if (Vector3.Distance(_startPosition, currentPosition) >= minimumDistance)
            {
                Vector2 direction = (currentPosition - _startPosition).normalized;
                SwipeDetected?.Invoke(direction);
            }
        }
    }
}
