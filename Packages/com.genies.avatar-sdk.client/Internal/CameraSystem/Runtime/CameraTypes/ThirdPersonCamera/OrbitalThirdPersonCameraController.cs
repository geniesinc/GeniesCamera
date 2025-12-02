using System;
using System.Collections;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Genies.CameraSystem.Focusable;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Cinemachine Camera Type that rotates around a target Focusable object
    /// Allows for auto-rotation when no input is detected
    /// </summary>
    [RequireComponent(typeof(CinemachineFreeLook), typeof(CinemachineInputProvider))]
    public class OrbitalThirdPersonCameraController : MonoBehaviour, ICameraType
    {
        [Header("Anchors Container")]
        [SerializeField] private Transform anchorsContainer;

        [Header("Target Bounds")]
        [SerializeField] private CameraFocusPoint cameraFocusPoint;

        [Header("Initial Settings")]
        [SerializeField] private Vector3 initialPosition;
        [SerializeField] private Vector3 initialRotation;

        [Header("Field of View")]
        [SerializeField] private float fieldOfView;

        [Header("Auto Rotation Settings")]
        [SerializeField] private float autoRotationWaitTime;
        [SerializeField] private float autoRotationDuration;

        [Header("Orbital Rigs Settings")]
        [SerializeField] private float topRigHeight;
        [SerializeField] private float topRigRadius;
        [SerializeField] private float mediumRigHeight;
        [SerializeField] private float mediumRigRadius;
        [SerializeField] private float bottomRigHeight;
        [SerializeField] private float bottomRigRadius;

        private CinemachineFreeLook _freeLook;
        private CinemachineInputProvider _inputProvider;

        private float _currentValue;
        private float _timer;
        private UIAnimator _rotationAnimation;
        private Coroutine _rotationLoopCoroutine;
        [SerializeField] private bool isAutoRotationEnabled = true;
        private bool _isRotationLoopActive;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Sets the initial configurations of the Free Look Virtual Camera
        /// and the behaviour of the autorotation feature
        /// </summary>
        public void ConfigureVirtualCamera()
        {
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(initialRotation);

            // Get the Input Provider component.
            _inputProvider ??= GetComponent<CinemachineInputProvider>();
            _inputProvider.enabled = false;

            // Get the Free Look component.
            _freeLook ??= GetComponent<CinemachineFreeLook>();

            _freeLook.m_Lens.FieldOfView = fieldOfView;

            _freeLook.m_Orbits[0].m_Height = topRigHeight;
            _freeLook.m_Orbits[0].m_Radius = topRigRadius;

            _freeLook.m_Orbits[1].m_Height = mediumRigHeight;
            _freeLook.m_Orbits[1].m_Radius = mediumRigRadius;

            _freeLook.m_Orbits[2].m_Height = bottomRigHeight;
            _freeLook.m_Orbits[2].m_Radius = bottomRigRadius;
        }

        /// <summary>
        /// Toggles the behaviour of the camera
        /// </summary>
        /// <param name="value">Toggles the behaviour on/off</param>
        public void ToggleBehaviour(bool value)
        {
            _inputProvider.enabled = value;

            if (value && isAutoRotationEnabled)
            {
                _inputProvider.XYAxis.action.performed += StopAutoRotationAnimation;
                _currentValue = _freeLook.m_XAxis.Value;
                AutoRotationCheck().Forget();
            }
            else
            {
                _inputProvider.XYAxis.action.performed -= StopAutoRotationAnimation;
                StopAutoRotationCheck();
            }
        }

        /// <summary>
        /// Enable the auto-rotation behaviour and check when
        /// no input is being detected
        /// </summary>
        private async UniTaskVoid AutoRotationCheck()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            await UniTask.WaitUntil(() => _freeLook.m_Follow != null && _freeLook.m_LookAt != null, cancellationToken: token);

            while (!token.IsCancellationRequested)
            {
                if (Math.Abs(_currentValue - _freeLook.m_XAxis.Value) > 0.01f)
                {
                    _currentValue = _freeLook.m_XAxis.Value;
                    _timer = 0f;
                }
                else
                {
                    _timer += Time.deltaTime;

                    if (_timer >= autoRotationWaitTime)
                    {
                        StartAutoRotation();
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
            }
        }

        /// <summary>
        /// Disables the auto-rotation behaviour
        /// </summary>
        private void StopAutoRotationCheck()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            // Set flag to stop rotation loop
            _isRotationLoopActive = false;
            StopAutoRotationAnimation(default);
        }

        /// <summary>
        /// Sets the Follow field on the Virtual Camera
        /// </summary>
        public void SetFollow()
        {
            var follow = new GameObject("Follow Anchor " + name);
            follow.transform.parent = anchorsContainer;
            follow.transform.position = cameraFocusPoint.GetBounds().center;
            _freeLook.m_Follow = follow.transform;
        }

        /// <summary>
        /// Sets the Look At field on the Virtual Camera
        /// </summary>
        public void SetLookAt()
        {
            var lookAt = new GameObject("Look At Anchor " + name);
            lookAt.transform.parent = anchorsContainer;
            Bounds bounds = cameraFocusPoint.GetBounds();
            lookAt.transform.position = bounds.center;
            lookAt.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
            _freeLook.m_LookAt = lookAt.transform;
        }

        /// <summary>
        /// Begins auto-rotation feature when no input is detected after a waiting time.
        /// </summary>
        private void StartAutoRotation()
        {
            if (_rotationLoopCoroutine != null)
            {
                return;
            }

            _isRotationLoopActive = true;
            _rotationLoopCoroutine = StartCoroutine(RotationLoop());
        }

        private IEnumerator RotationLoop()
        {
            while (_isRotationLoopActive)
            {
                float startValue = _freeLook.m_XAxis.Value;
                float endValue = startValue + 360f;

                _rotationAnimation = AnimateVirtual.Float(startValue, endValue, autoRotationDuration,
                    x => _freeLook.m_XAxis.Value = x);

                yield return _rotationAnimation.WaitForCompletion();

                if (!_isRotationLoopActive)
                {
                    break;
                }
            }

            // Clean up when loop exits
            _rotationLoopCoroutine = null;
        }

        /// <summary>
        /// Stops auto-rotation when input is detected.
        /// </summary>
        /// <param name="context"></param>
        private void StopAutoRotationAnimation(InputAction.CallbackContext context)
        {
            _isRotationLoopActive = false;

            // Stop the coroutine if it's still running
            if (_rotationLoopCoroutine != null)
            {
                StopCoroutine(_rotationLoopCoroutine);
                _rotationLoopCoroutine = null;
            }

            // Terminate any active rotation animation
            if (_rotationAnimation != null)
            {
                _rotationAnimation.Terminate();
                _rotationAnimation = null;
            }
        }

        private void OnDestroy()
        {
            if (isAutoRotationEnabled)
            {
                _inputProvider.XYAxis.action.performed -= StopAutoRotationAnimation;
                StopAutoRotationCheck();
            }
        }
    }
}
