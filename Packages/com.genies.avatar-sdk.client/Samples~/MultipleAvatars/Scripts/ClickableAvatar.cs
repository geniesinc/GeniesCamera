using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Genies.Sdk;
using Cysharp.Threading.Tasks;

namespace Genies.Sdk.Samples.MultipleAvatars
{
    /// <summary>
    /// Component that makes an avatar clickable to open the Avatar Editor.
    /// Should be added to spawned avatar GameObjects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ClickableAvatar : MonoBehaviour
    {
        private ManagedAvatarComponent _managedAvatarComponent;
        private bool _debugMode = false;

        private void Awake()
        {
            // Auto-find ManagedAvatarComponent if not assigned
            if (_managedAvatarComponent == null)
            {
                _managedAvatarComponent = GetComponent<ManagedAvatarComponent>();
            }
        }

        private void Update()
        {
            // Only process input when Avatar Editor is closed
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                return;
            }

            HandlePointerInput();
        }

        #region Input Handling

        private void HandlePointerInput()
        {
            // Skip if no pointer input device (e.g., Mouse or Touchscreen)
            if (Pointer.current == null)
            {
                return;
            }

            // Only process on pointer press this frame
            if (!Pointer.current.press.wasPressedThisFrame)
            {
                return;
            }

            // Skip clicks over UI
            if (IsPointerOverUI())
            {
                if (_debugMode)
                {
                    Debug.Log("Pointer over UI — ignoring avatar click.");
                }

                return;
            }

            Vector2 pointerPos = Pointer.current.position.ReadValue();
            ProcessPointer(pointerPos);
        }

        private void ProcessPointer(Vector2 screenPosition)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning("No MainCamera found for click raycast.");
                }

                return;
            }

            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    OnClick().Forget();
                }
                else if (_debugMode)
                {
                    Debug.Log($"Raycast hit {hit.collider.gameObject.name}, not this avatar ({gameObject.name}).");
                }
            }
            else if (_debugMode)
            {
                Debug.Log("Raycast did not hit any object.");
            }
        }

        /// <summary>
        /// Checks if pointer is over any UI element.
        /// </summary>
        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            // Use device-specific ID for consistent simulator behavior
            int deviceId = Pointer.current.device.deviceId;
            return EventSystem.current.IsPointerOverGameObject(deviceId);
        }

        #endregion

        #region Avatar Editor Integration

        private async UniTaskVoid OnClick()
        {
            if (_debugMode)
            {
                Debug.Log($"Avatar clicked: {gameObject.name}");
            }

            // Prevent switching avatars if the editor is already open
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                if (_debugMode)
                {
                    Debug.Log($"Avatar Editor already open — ignoring click on {gameObject.name}.");
                }

                return;
            }

            // Wait until the end of the frame to ensure camera finishes input updates
            await UniTask.WaitForEndOfFrame(this);

            await OpenAvatarEditor();
        }

        /// <summary>
        /// Opens the Avatar Editor with this avatar.
        /// </summary>
        public async UniTask OpenAvatarEditor()
        {
            try
            {
                await AvatarSdk.OpenAvatarEditorAsync(_managedAvatarComponent.ManagedAvatar);

                if (_debugMode)
                {
                    Debug.Log($"Avatar Editor opened successfully for {gameObject.name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open Avatar Editor for {gameObject.name}: {ex.Message}");
            }
        }

        #endregion
    }
}
