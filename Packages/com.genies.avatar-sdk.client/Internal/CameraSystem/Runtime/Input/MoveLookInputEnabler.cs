using UnityEngine;
using UnityEngine.InputSystem;

namespace Genies.CameraSystem
{
    public class MoveLookInputEnabler : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveInputActionReference;
        [SerializeField] private InputActionReference lookInputActionReference;

        private void OnEnable()
        {
            moveInputActionReference.action.Enable();
            lookInputActionReference.action.Enable();
        }

        private void OnDisable()
        {
            moveInputActionReference.action.Disable();
            lookInputActionReference.action.Disable();
        }

        public Vector2 GetMoveVector()
        {
            return moveInputActionReference.action.ReadValue<Vector2>();
        }

        public Vector2 GetLookVector()
        {
            return lookInputActionReference.action.ReadValue<Vector2>();
        }
    }
}
