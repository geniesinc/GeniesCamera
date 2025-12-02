using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Genies.CameraSystem;
using UnityEngine;

namespace Genies.CameraSystem
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class FlyByWireCameraController : MonoBehaviour, ICameraType
    {
        [Header("Controls")]
        [SerializeField] private MoveLookInputEnabler moveLookInputEnabler;

        [Header("Virtual Camera Settings")]
        [SerializeField] private float fieldOfView = 40f;

        [Header("Initial Camera Settings")]
        [SerializeField] private Vector3 initialPosition;
        [SerializeField] private Vector3 initialRotation;

        [Header("Min/Max Movement Limits")]
        [SerializeField] private float minX;
        [SerializeField] private float maxX;
        [SerializeField] private float minY;
        [SerializeField] private float maxY;
        [SerializeField] private float minZ;
        [SerializeField] private float maxZ;

        [Header("Camera Movement")]
        [SerializeField] private float cameraSpeed;
        [SerializeField] private float rotationSpeed;

        [Header("Target Canvas for Controls")]
        [SerializeField] private RectTransform canvasRT;

        [Header("Controls References")]
        [SerializeField] private RectTransform controlsPrefab;

        private RectTransform _controlsInstance;

        private CinemachineVirtualCamera _virtualCamera;

        private CancellationTokenSource _cancellationTokenSource;

        public void ConfigureVirtualCamera()
        {
            _virtualCamera ??= GetComponent<CinemachineVirtualCamera>();

            _virtualCamera.m_Lens.FieldOfView = fieldOfView;

            _virtualCamera.transform.position = initialPosition;
            _virtualCamera.transform.rotation = Quaternion.Euler(initialRotation);
        }

        public void ToggleBehaviour(bool value)
        {
            moveLookInputEnabler.enabled = value;

            if (value)
            {
                _controlsInstance ??= Instantiate(controlsPrefab, canvasRT);


                StartControls().Forget();
            }
            else
            {
                StopMovement();
            }
        }

        private async UniTaskVoid StartControls()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            // Tracks the vertical rotation angle
            float pitch = initialRotation.x;
            // Tracks the horizontal rotation angle
            float yaw = initialRotation.y;

            while (!token.IsCancellationRequested)
            {
                var moveVector = moveLookInputEnabler.GetMoveVector();
                var lookVector = moveLookInputEnabler.GetLookVector();

                if (moveVector != Vector2.zero)
                {
                    Vector3 forwardMovement = transform.forward * moveVector.y;
                    Vector3 rightMovement = transform.right * moveVector.x;

                    Vector3 movement = (forwardMovement + rightMovement).normalized * cameraSpeed * Time.deltaTime;

                    transform.position += movement;

                    var clampedPos = transform.position;

                    // Clamp to min and max
                    clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
                    clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);
                    clampedPos.z = Mathf.Clamp(clampedPos.z, minZ, maxZ);

                    transform.position = clampedPos;
                }

                if (lookVector != Vector2.zero)
                {
                    yaw += lookVector.x * rotationSpeed * Time.deltaTime;
                    pitch -= lookVector.y * rotationSpeed * Time.deltaTime;

                    pitch = Mathf.Clamp(pitch, -80f, 80f);

                    transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        /// <summary>
        /// Disables the auto-rotation behaviour
        /// </summary>
        private void StopMovement()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (_controlsInstance)
            {
                Destroy(_controlsInstance.gameObject);
                _controlsInstance = null;
            }
        }

        private void OnDestroy()
        {
            StopMovement();
        }
    }
}
