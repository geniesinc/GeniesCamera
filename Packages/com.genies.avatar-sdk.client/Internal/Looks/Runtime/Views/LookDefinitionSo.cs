using Genies.Looks.Models;
using UnityEngine;

namespace Genies.Looks.Core.Data
{
    /// <summary>
    /// ScriptableObject that stores look definition data for use in Unity Editor and runtime scenarios.
    /// This asset allows designers and developers to configure look data through the Unity Inspector
    /// and provides a convenient way to create and manage look configurations.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "Looks/LooksDefinitionSo", menuName = "LooksDefinitionSo", order = 0)]
#endif
    public class LookDefinitionSo : ScriptableObject
    {
        /// <summary>
        /// The avatar definition JSON string that defines the avatar's appearance.
        /// This field can be configured in the Unity Inspector.
        /// </summary>
        public string _avatarDefinition;

        /// <summary>
        /// The animation ID associated with this look for animation playback.
        /// This field can be configured in the Unity Inspector.
        /// </summary>
        public string _animationId;

        /// <summary>
        /// The scene ID where this look should be displayed or was captured.
        /// This field can be configured in the Unity Inspector.
        /// </summary>
        public string _sceneId;
        private LookData _lookData;

        /// <summary>
        /// Creates and returns a LookData instance populated with the values configured in this ScriptableObject.
        /// This method provides a convenient way to convert ScriptableObject data to runtime look data.
        /// </summary>
        /// <returns>A LookData struct containing the avatar definition, animation ID, and scene ID.</returns>
        public LookData GetLook()
        {
            _lookData.AvatarDefinition = _avatarDefinition;
            _lookData.AnimationId = _animationId;
            _lookData.SceneId = _sceneId;

            return _lookData;
        }
    }
}
