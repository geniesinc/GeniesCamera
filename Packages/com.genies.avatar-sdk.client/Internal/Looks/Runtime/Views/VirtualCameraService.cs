using Genies.Animations;
using Genies.Avatars.Behaviors;
using Genies.CameraSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Looks
{
    /// <summary>
    /// GameObject that holds the Virtual Camera Controllers that are used throughout the project.
    /// This service manages different types of virtual cameras including general cameras and animation-specific cameras.
    /// It provides centralized access to camera controllers for avatar and animation rendering scenarios.
    /// </summary>
    public class VirtualCameraService : MonoBehaviour
    {
        [FormerlySerializedAs("virtualCameraCameraController")]
        [Header("Virtual Camera Controllers References")]
        [SerializeField]
        private VirtualCameraController<GeniesVirtualCameraCatalog> _virtualCameraCameraController;
        
        /// <summary>
        /// Gets the general virtual camera controller used for standard avatar viewing scenarios.
        /// This controller uses the GeniesVirtualCameraCatalog for camera configurations.
        /// </summary>
        public VirtualCameraController<GeniesVirtualCameraCatalog> CameraController => _virtualCameraCameraController;

        [FormerlySerializedAs("animationCameraVirtualCameraController")]
        [SerializeField]
        private VirtualCameraController<AnimationVirtualCameraCatalog> _animationCameraVirtualCameraController;
        
        /// <summary>
        /// Gets the animation-specific virtual camera controller used for animation playback scenarios.
        /// This controller uses the AnimationVirtualCameraCatalog for animation-focused camera configurations.
        /// </summary>
        public VirtualCameraController<AnimationVirtualCameraCatalog> AnimationCameraController => _animationCameraVirtualCameraController;

        private void Awake()
        {
            _virtualCameraCameraController.Initialize();
            _animationCameraVirtualCameraController.Initialize();
        }
    }
}
