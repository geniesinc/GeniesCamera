using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.CameraSystem
{
    public enum FocusCameraMode
    {
        Default = 0,
        ZoomIn = 1,
        ZoomOut = 2
    }

    public class FocusCameraModeHandler : MonoBehaviour
    {
        public FocusCameraMode focusCameraMode = FocusCameraMode.Default;
        private FocusCameraMode _previousFocusCameraMode;

        private CinemachineMixingCamera _mixingCamera;
        private CinemachineVirtualCamera _zoomInVirtualCamera;
        private CinemachineVirtualCamera _zoomOutVirtualCamera;

        private float _maxWeight = 1f;
        private float _minWeight = 0f;
        private float _defaultWeight = 0.5f;

        public float transitionTime = 1f;

        private void Awake()
        {
            _previousFocusCameraMode = focusCameraMode;

            _mixingCamera ??= GetComponent<CinemachineMixingCamera>();

            _zoomInVirtualCamera ??= _mixingCamera.ChildCameras[0] as CinemachineVirtualCamera;
            _zoomOutVirtualCamera ??= _mixingCamera.ChildCameras[1] as CinemachineVirtualCamera;
        }

        private void OnValidate()
        {
            ChangeFocusMode(focusCameraMode);
        }

        public void ChangeFocusMode(FocusCameraMode targetFocusMode)
        {
            if (targetFocusMode == _previousFocusCameraMode)
            {
                return;
            }

            TransitionToFocusCameraMode(targetFocusMode, transitionTime).Forget();
            _previousFocusCameraMode = targetFocusMode;
        }

        /// <summary>
        /// Triggers a transition over a specific duration to blend into
        /// another focus camera mode (Zoom In, Zoom Out or Default)
        /// </summary>
        /// <param name="targetFocusMode">The target focus mode to blend into</param>
        /// <param name="transitionDuration">The duration that the transition should last</param>
        /// <returns></returns>
        private async UniTask TransitionToFocusCameraMode(FocusCameraMode targetFocusMode, float transitionDuration = 1f)
        {
            var startTime = Time.time;
            var endTime = startTime + transitionDuration;

            // Get the current weight of the virtual cameras
            var startWeightZoomIn = _mixingCamera.GetWeight(_zoomInVirtualCamera);
            var startWeightZoomOut = _mixingCamera.GetWeight(_zoomOutVirtualCamera);

            // Get the final weight to apply to the virtual cameras, depending on the selected focus mode
            var endWeightZoomIn = targetFocusMode == FocusCameraMode.Default ? _defaultWeight : (targetFocusMode == FocusCameraMode.ZoomIn ? _maxWeight : _minWeight);
            var endWeightZoomOut = targetFocusMode == FocusCameraMode.Default ? _defaultWeight : (targetFocusMode == FocusCameraMode.ZoomIn ? _minWeight : _maxWeight);

            // Wait for the transition to end
            while (Time.time < endTime)
            {
                var t = (Time.time - startTime) / transitionDuration;

                _mixingCamera.SetWeight(_zoomInVirtualCamera, Mathf.Lerp(startWeightZoomIn, endWeightZoomIn, t));
                _mixingCamera.SetWeight(_zoomOutVirtualCamera, Mathf.Lerp(startWeightZoomOut, endWeightZoomOut, t));

                await UniTask.Yield();
            }

            // Set the final weights to make sure the values are correct
            _mixingCamera.SetWeight(_zoomInVirtualCamera, endWeightZoomIn);
            _mixingCamera.SetWeight(_zoomOutVirtualCamera, endWeightZoomOut);
        }
    }
}
