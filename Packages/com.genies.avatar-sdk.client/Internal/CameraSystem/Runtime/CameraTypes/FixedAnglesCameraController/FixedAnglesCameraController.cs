using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Cinemachine Camera Type that allows user to blend between different fixed angles
    /// </summary>
    [RequireComponent(typeof(CinemachineMixingCamera))]
    public class FixedAnglesCameraController : MonoBehaviour, ICameraType
    {
        [Header("Virtual Camera Child")]
        [SerializeField] private CinemachineVirtualCamera virtualCameraPrefab;

        [Header("Camera Settings")]
        [SerializeField] private float fieldOfView;

        [SerializeField] private float nearClipPlane;
        [SerializeField] private float farClipPlane;

        [SerializeField] private bool isTransitionImmediate;

        [Header("Fixed Camera Angles")]
        [SerializeField] private List<FixedCameraAngle> cameraAngles;

        [SerializeField] private SwipeDetectionEnabler swipeDetectionEnabler;

        private int _currentActiveCameraIndex;
        private CinemachineMixingCamera _mixingCamera;

        private Vector2 _startPosition;
        private bool _isSwiping;

        private const float _endActiveCameraWeight = 0f;
        private const float _endTargetCameraWeight = 1f;

        private const float _immediateDuration = 0f;
        private const float _defaultDuration = 2f;

        private void Awake()
        {
            CheckUI();
            swipeDetectionEnabler.SwipeDetected += SwitchCameraByDirection;
        }

        /// <summary>
        /// Configures the Mixing Camera and creates the required child virtual cameras
        /// </summary>
        public void ConfigureVirtualCamera()
        {
            _mixingCamera ??= GetComponent<CinemachineMixingCamera>();

            if (_mixingCamera.transform.childCount == 0)
            {
                // Creates a child virtual camera based on the amount of angles in the list
                for (int i = 0; i < cameraAngles.Count; i++)
                {
                    CinemachineVirtualCamera virtualCamera = Instantiate(virtualCameraPrefab, _mixingCamera.transform, true);
                    virtualCamera.transform.position = cameraAngles[i].position;
                    virtualCamera.transform.rotation = Quaternion.Euler(cameraAngles[i].direction);

                    virtualCamera.m_Lens.FieldOfView = fieldOfView;
                    virtualCamera.m_Lens.NearClipPlane = nearClipPlane;
                    virtualCamera.m_Lens.FarClipPlane = farClipPlane;

                    _mixingCamera.SetWeight(i, 0f);
                }

                // Set the first child camera as active
                _mixingCamera.SetWeight(_currentActiveCameraIndex, 1f);
            }
        }

        /// <summary>
        /// Toggles the behaviour of the camera
        /// </summary>
        /// <param name="value">Toggles the behaviour on/off</param>
        public void ToggleBehaviour(bool value)
        {
            swipeDetectionEnabler.enabled = value;
        }

        /// <summary>
        /// Detects the direction and switches camera based on the swipe
        /// </summary>
        /// <param name="direction"></param>
        private void SwitchCameraByDirection(Vector2 direction)
        {
            if (_isSwiping)
            {
                return;
            }

            if (Vector2.Dot(Vector2.left, direction) > swipeDetectionEnabler.directionThreshold ||
                Vector2.Dot(Vector2.right, direction) > swipeDetectionEnabler.directionThreshold)
            {
                var nextIndex = _currentActiveCameraIndex;

                // Swipe to the left detected
                if (direction.x < 0f)
                {
                    nextIndex--;

                    if (nextIndex < 0)
                    {
                        nextIndex = cameraAngles.Count - 1;
                    }

                    _isSwiping = true;
                }
                // Swipe to the right detected
                else if (direction.x > 0f)
                {
                    nextIndex++;

                    if (nextIndex > cameraAngles.Count - 1)
                    {
                        nextIndex = 0;
                    }

                    _isSwiping = true;
                }

                // Trigger transition if swipe is detected
                if (_isSwiping)
                {
                    var duration = isTransitionImmediate ? _immediateDuration : _defaultDuration;

                    TransitionToNextCamera(nextIndex, duration).Forget();
                }
            }
        }

        /// <summary>
        /// Triggers a transition to a target camera over a specific time duration
        /// </summary>
        /// <param name="targetCameraIndex">The index of the camera to transition to</param>
        /// <param name="transitionDuration">The duration of the transition</param>
        private async UniTask TransitionToNextCamera(int targetCameraIndex, float transitionDuration)
        {
            var startTime = Time.time;
            var endTime = startTime + transitionDuration;

            var initialActiveCameraWeight = _mixingCamera.GetWeight(_currentActiveCameraIndex);
            var initialTargetCameraWeight = _mixingCamera.GetWeight(targetCameraIndex);

            while (Time.time < endTime)
            {
                var t = (Time.time - startTime) / transitionDuration;

                _mixingCamera.SetWeight(_currentActiveCameraIndex, Mathf.Lerp(initialActiveCameraWeight, _endActiveCameraWeight, t));
                _mixingCamera.SetWeight(targetCameraIndex, Mathf.Lerp(initialTargetCameraWeight, _endTargetCameraWeight, t));

                await UniTask.Yield();
            }

            _mixingCamera.SetWeight(_currentActiveCameraIndex, _endActiveCameraWeight);
            _mixingCamera.SetWeight(targetCameraIndex, _endTargetCameraWeight);

            _currentActiveCameraIndex = targetCameraIndex;

            _isSwiping = false;
        }

        private void CheckUI()
        {
            Debug.Assert(virtualCameraPrefab != null, "virtualCameraPrefab is not set");
            Debug.Assert(cameraAngles != null, "cameraAngles is not set");
        }

        private void OnDestroy()
        {
            swipeDetectionEnabler.SwipeDetected -= SwitchCameraByDirection;
        }
    }
}
