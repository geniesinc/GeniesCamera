using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Cinemachine Camera Type that follows a target animated camera.
    /// It follows the animation of the camera, configuring to match the physical camera's settings.
    /// </summary>
    [RequireComponent(typeof(AnimatedCameraState), typeof(CinemachineVirtualCamera))]
    public class AnimatedCameraController : MonoBehaviour, ICameraType
    {
        [Tooltip("The target camera to follow")]
        private Camera _animatedCamera;

        //internal
        private AnimatedCameraState _animatedCameraState;
        private CinemachineVirtualCamera _vCam;

        private bool _followedLastFrame;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Sets the initial configuration to the Virtual Camera component
        /// for proper copying of the proxy Camera
        /// </summary>
        public void ConfigureVirtualCamera()
        {
            _vCam ??= GetComponent<CinemachineVirtualCamera>();

            _vCam.AddCinemachineComponent<CinemachineHardLockToTarget>();
            _vCam.AddCinemachineComponent<CinemachineSameAsFollowTarget>();

            _followedLastFrame = false;

            _animatedCameraState = GetComponent<AnimatedCameraState>();
        }

        /// <summary>
        /// Toggles the behaviour of the camera
        /// </summary>
        /// <param name="value">Toggles the behaviour on/off</param>
        public void ToggleBehaviour(bool value)
        {
            if (value)
            {
                BeginFollowing().Forget();
            }
            else
            {
                StopFollowing();
            }
        }

        private async UniTaskVoid BeginFollowing()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            // Return if behaviour hasn't been activated
            await UniTask.WaitUntil(() => _animatedCamera != null, cancellationToken: token);

            if(_animatedCameraState.FollowingProxy)
            {
                // First frame following
                if (!_followedLastFrame)
                {
                    // Updates the virtual camera's settings to match the physical camera's settings when following the proxy
                    // This is necessary to replicate how the camera is configured in the original animation
                    _vCam.m_Lens.ModeOverride = LensSettings.OverrideModes.Physical;
                    _vCam.m_Lens.SensorSize = new Vector2(25f, 25f);
                    _vCam.m_Lens.GateFit = Camera.GateFitMode.Overscan;
                }

                // Match the virtual camera's field of view to the proxy camera's field of view
                _vCam.m_Lens.FieldOfView = UpdateVirtualCameraFOV(_vCam, _animatedCamera);

                // Flag
                _followedLastFrame = true;
            }
            else
            {
                // If is not following anything in the first frame
                if (_followedLastFrame)
                {
                    // Resets them when not following the object
                    _vCam.m_Lens.ModeOverride = LensSettings.OverrideModes.None;
                }

                // Flag
                _followedLastFrame = false;
            }

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
        }

        private void StopFollowing()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void SetAnimatedCamera(Camera newAnimatedCamera)
        {
            _animatedCamera = newAnimatedCamera == null ? null : newAnimatedCamera;
            _vCam.m_Follow = newAnimatedCamera == null ? null : newAnimatedCamera.transform;
        }

        /// <summary>
        /// Calculates the FOV value of a camera based on the sensor size and the focal length of the lens.
        /// Using the equation for field of view, this method returns the FOV value as degrees back to the
        /// Virtual Camera.
        /// </summary>
        /// <param name="vCam">The Virtual Camera to configure</param>
        /// <param name="proxyCam">The Proxy Camera to get the focal length from</param>
        /// <returns>The FOV value of the virtual camera</returns>
        private float UpdateVirtualCameraFOV(CinemachineVirtualCamera vCam, Camera proxyCam)
        {
            var sensorHeight = vCam.m_Lens.SensorSize.y;
            var focalLength = proxyCam.focalLength;

            return 2f * Mathf.Atan(sensorHeight / (2 * focalLength)) * Mathf.Rad2Deg;
        }

        private void OnDestroy()
        {
            StopFollowing();
        }
    }
}
