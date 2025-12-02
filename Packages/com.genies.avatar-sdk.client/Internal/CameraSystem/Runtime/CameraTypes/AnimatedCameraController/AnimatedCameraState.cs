using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Holds the current state of the main camera then follows a proxy camera.
    /// Updates itself through events.
    /// </summary>
    public class AnimatedCameraState : MonoBehaviour
    {
        public bool FollowingProxy {get; private set;}
        private const string DefaultSceneId = "Default";

        public void OnSceneSwitched(string sceneID)
        {
            FollowingProxy = sceneID != DefaultSceneId;
        }
    }
}
