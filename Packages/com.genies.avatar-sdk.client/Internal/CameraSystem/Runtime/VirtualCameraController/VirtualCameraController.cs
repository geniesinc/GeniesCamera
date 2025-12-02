using System;
using System.Collections.Generic;
using Cinemachine;
using Genies.CameraSystem.Focusable;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Provides methods to activate or deactivate cameras based on a list of enum (catalogs).
    /// </summary>
    /// <remarks> This class depends on the enum (Camera Catalog) to initialize.
    /// You can create as many controllers as you have catalogs to create different use cases of the camera.</remarks>
    [Serializable]
    public class VirtualCameraController<TEnum> where TEnum : struct, Enum
    {
        [Header("Camera References")]
        [Tooltip("Reference to the camera object to use by this controller")]
        public Camera CinemachineCamera;

        [Header("Virtual Cameras Settings")]
        [Tooltip("Transition settings for this virtual camera controller")]
        [SerializeField] private CinemachineBlenderSettings _blenderSettings;

        [Tooltip("List of cameras to use by this controller. " +
            "Make sure this list represents the catalog of the controller when setting up the controller in the Scene.")]
        [SerializeField] private List<GeniesVirtualCamera> _geniesVirtualCameras;

        private CinemachineBrain _brain;

        private ICameraType _activeCameraController;
        private CinemachineVirtualCameraBase _activeVCam;

        private const int _mainPriority = 20;
        private const int _minPriority = 0;

        private float _originalCamFocalLength;
        private Vector2 _originalCamSensorSize;

        private Dictionary<TEnum, int> _enumToIndexMap = new Dictionary<TEnum, int>();

        /// <summary>
        /// Initializes this virtual camera controller and its cameras.
        /// If using multiple Virtual Camera Controllers, make sure to use this method
        /// to override any previous controller.
        /// </summary>
        public void Initialize()
        {
            // Initialize the dictionary to store enum values to avoid casting
            InitializeEnumToIndexMap();

            // Get the CinemachineBrain from the camera object (assign to main camera or create camera if null)
            if (CinemachineCamera == null)
            {
                CinemachineCamera = Camera.main;
            }

            if (CinemachineCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                CinemachineCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                CinemachineCamera.tag = "MainCamera";
            }
            
            _brain = CinemachineCamera.GetComponent<CinemachineBrain>() == null ? CinemachineCamera.gameObject.AddComponent<CinemachineBrain>() : CinemachineCamera.GetComponent<CinemachineBrain>();

            // Set the transition
            if (_blenderSettings != null)
            {
                _brain.m_CustomBlends = _blenderSettings;
            }

            _originalCamFocalLength = CinemachineCamera.focalLength;
            _originalCamSensorSize = CinemachineCamera.sensorSize;

            // Set up virtual cameras
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                geniesVirtualCamera.CameraTypeScript.ConfigureVirtualCamera();
            }
        }

        private void InitializeEnumToIndexMap()
        {
            // Store enum values to the dictionary
            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));
            for (int i = 0; i < values.Length; i++)
            {
                _enumToIndexMap[values[i]] = i;
            }
        }

        /// <summary>
        /// Sets a high priority to a Genies Virtual Camera if it's found inside the
        /// Genies Virtual Camera list. Makes sure the priorities of the other cameras
        /// are set to zero.
        /// </summary>
        /// <remarks>Use the catalog as the parameter directly, it avoids casting between index and camera catalog</remarks>
        /// <param name="virtualCameraIndex">The index of the Genies Virtual Camera to set as active</param>
        public void ActivateVirtualCamera(TEnum virtualCameraIndex)
        {
            if (_enumToIndexMap.TryGetValue(virtualCameraIndex, out var index))
            {
                GeniesVirtualCamera virtualCamera = _geniesVirtualCameras[index];

                if (_activeVCam == null)
                {
                    _activeVCam = virtualCamera.VirtualCamera;
                }

                else if (_activeVCam == virtualCamera.VirtualCamera)
                {
                    return;
                }

                _activeVCam = virtualCamera.VirtualCamera;
                _activeCameraController = virtualCamera.CameraTypeScript;

                foreach (GeniesVirtualCamera geniesVirtualCamera in _geniesVirtualCameras)
                {
                    geniesVirtualCamera.CameraTypeScript.ToggleBehaviour(false);
                    geniesVirtualCamera.VirtualCamera.Priority = _minPriority;
                }

                _activeCameraController.ConfigureVirtualCamera();
                _activeCameraController.ToggleBehaviour(true);
                _activeVCam.Priority = _mainPriority;
            }
        }

        public void DeactivateAllVirtualCameras()
        {
            foreach (GeniesVirtualCamera geniesVirtualCamera in _geniesVirtualCameras)
            {
                geniesVirtualCamera.CameraTypeScript.ToggleBehaviour(false);
                geniesVirtualCamera.VirtualCamera.Priority = _minPriority;
            }

            _activeVCam = null;
            _activeCameraController = null;
        }

        /// <summary>
        /// Resets the cinemachine camera to its original focal length and sensor size to avoid broken transitions.
        /// </summary>
        public void ResetCinemachineCamera()
        {
            CinemachineCamera.focalLength = _originalCamFocalLength;
            CinemachineCamera.sensorSize = _originalCamSensorSize;
        }

    #region Focus Cameras Methods

        /// <summary>
        /// Sets the fullscreen mode to all focus cameras.
        /// Makes sure the Focus Camera Controllers focus on the object as if it were in a
        /// fullscreen mode.
        /// </summary>
        /// <param name="isFullScreen">The bool value to set the full screen mode of the Focus Cameras</param>
        public void SetFullScreenModeInFocusCameras(bool isFullScreen)
        {
            foreach (GeniesVirtualCamera geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetFullScreen(isFullScreen);
                }
            }
        }

        /// <summary>
        /// Updates the viewport object of all focus cameras.
        /// This is needed for all focus cameras to behave correctly.
        /// </summary>
        /// <param name="newViewport">The new viewport to set on the cameras</param>
        public void UpdateViewportInFocusCameras(RectTransform newViewport)
        {
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetTargetViewport(newViewport);
                }
            }
        }

        /// <summary>
        /// Sets a focusable object for the focus camera to look at.
        /// </summary>
        /// <param name="focusCameraIndex">catalog (which camera) to use as the focus camera</param>
        /// <param name="focusable">the focusable to have the camera focus on</param>
        public void SetFocusableInFocusCamera(TEnum focusCameraIndex, IFocusable focusable)
        {
            if (_enumToIndexMap.TryGetValue(focusCameraIndex, out var index))
            {
                GeniesVirtualCamera geniesVirtualCamera = _geniesVirtualCameras[index];

                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetTargetFocusable(focusable);
                }
            }
        }

        public void SetFocusModeInFocusCamera(TEnum focusCameraIndex, FocusCameraMode targetFocusMode)
        {
            if (_enumToIndexMap.TryGetValue(focusCameraIndex, out var index))
            {
                GeniesVirtualCamera geniesVirtualCamera = _geniesVirtualCameras[index];

                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.Handler.ChangeFocusMode(targetFocusMode);
                }
            }
        }

    #endregion

    #region Animated Cameras Methods
        /// <summary>
        /// Sets the animated camera to follow onto all animated cameras.
        /// </summary>
        /// <param name="newAnimatedCamera">The target animated camera to follow</param>
        public void SetAnimatedCamera(Camera newAnimatedCamera)
        {
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera.CameraType == VirtualCameraType.AnimatedCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as AnimatedCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetAnimatedCamera(newAnimatedCamera);
                }
            }
        }

    #endregion

    #region Third Person Cameras Methods

        /// <summary>
        /// Sets the Follow & Look At fields on the Orbital Third Person Cameras
        /// </summary>
        public void SetFollowAndLookAtOnOrbitalThirdPersonCameras()
        {
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera.CameraTypeScript is OrbitalThirdPersonCameraController cameraScript)
                {
                    cameraScript.SetFollow();
                    cameraScript.SetLookAt();
                }
            }
        }

    #endregion
    }
}
