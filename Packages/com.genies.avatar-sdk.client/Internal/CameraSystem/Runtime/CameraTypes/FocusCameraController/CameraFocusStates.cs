using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Scriptable object that stores a list of camera's positions and rotations as Poses
    /// and the name of the target focus objects.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "CameraFocusStates", menuName = "CameraSystem/CameraFocusStates", order = 1)]
#endif
    [Serializable]
    public class CameraFocusStates : ScriptableObject
    {
        [SerializeField] private List<string> _focusTargets = new List<string>();
        [SerializeField] private List<Pose> _cameraFocusPoses = new List<Pose>();

        public List<string> Keys => _focusTargets;
        public List<Pose> Poses => _cameraFocusPoses;

        public bool ContainsKey(string key) => _focusTargets.Contains(key);
        public int GetKeyIndex(string key) => _focusTargets.IndexOf(key);

        /// <summary>
        /// Updates a target camera focus state with a new Pose.
        /// Replaces old Pose.
        /// </summary>
        /// <param name="focusTarget">The target camera focus state name to modify</param>
        /// <param name="newPose">The new Pose to save</param>
        public void UpdateCameraPoseWithFocusTarget(string focusTarget, Pose newPose)
        {
            if (ContainsKey(focusTarget))
            {
                var index = GetKeyIndex(focusTarget);
                if (index < _cameraFocusPoses.Count)
                {
                    _cameraFocusPoses[index] = newPose;
                }
            }
        }

        /// <summary>
        /// Adds a new Pose object and a string value inside the lists.
        /// </summary>
        /// <param name="focusTarget">The name of the camera focus state to add</param>
        /// <param name="cameraPose">The Pose of the camera focus state to add</param>
        public void Add(string focusTarget, Pose cameraPose)
        {
            _focusTargets.Add(focusTarget);
            _cameraFocusPoses.Add(cameraPose);
        }

        /// <summary>
        /// Tries to get the target camera focus state's pose
        /// </summary>
        /// <param name="targetName">The name of the camera focus state</param>
        /// <param name="cameraPose">The Pose of the camera focus state</param>
        /// <returns>Returns a boolean value if the target is found</returns>
        public bool TryGetCameraFocusPose(string targetName, out Pose cameraPose)
        {
            cameraPose = Pose.identity;
            if (!ContainsKey(targetName))
            {
                return false;
            }

            cameraPose = _cameraFocusPoses[GetKeyIndex(targetName)];

            return true;
        }
    }
}
