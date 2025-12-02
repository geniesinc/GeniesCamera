using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Avatars.Sdk.Editor
{
    /// <summary>
    /// Configuration asset that holds a collection of default animator controller configurations.
    /// This ScriptableObject is used to store and manage default settings for avatar animator controllers.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName="AnimControllerDefaultsConfig.asset", menuName="Genies/Anim Controller Defaults/AnimControllerDefaultsConfig asset")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AnimControllerDefaultsConfig : ScriptableObject
#else
    public class AnimControllerDefaultsConfig : ScriptableObject
#endif
    {
        /// <summary>
        /// Array of default animator controller configuration assets to be applied to avatar controllers.
        /// </summary>
        [FormerlySerializedAs("defaults")] public AnimControllerDefaultAsset[] Defaults;
    }
}
