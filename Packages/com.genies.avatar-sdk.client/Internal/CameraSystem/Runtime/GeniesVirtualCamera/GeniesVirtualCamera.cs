using System;
using Cinemachine;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Class to handle the type of camera to be configured within the
    /// Virtual Camera Controller.
    /// Allows for the configuration of whatever type of Virtual Camera component, as
    /// Cinemachine has several of these.
    /// </summary>
    [Serializable]
    public class GeniesVirtualCamera
    {
        [SerializeField] private VirtualCameraType _cameraType;
        [SerializeField] private CinemachineVirtualCameraBase _virtualCamera;

        private ICameraType _cameraTypeScript;

        public VirtualCameraType CameraType => _cameraType;

        public ICameraType CameraTypeScript
        {
            get
            {
                return _cameraTypeScript ??= _virtualCamera.transform.gameObject.GetComponent<ICameraType>();
            }
        }
        public CinemachineVirtualCameraBase VirtualCamera => _virtualCamera;
    }

    public enum VirtualCameraType
    {
        NONE = 0,
        AnimatedCamera = 1,
        FocusCamera = 2,
        OrbitalThirdPersonCamera = 3,
        FixedAnglesCamera = 4,
        FlyByWireCamera = 5,
    }
}
