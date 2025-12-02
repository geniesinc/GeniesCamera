using UnityEngine;
using UnityEditor.Animations;
using UnityEngine.Serialization;

namespace Genies.Avatars.Sdk.Editor
{
    /// <summary>
    /// Interface for objects that can apply default configurations to AnimatorController instances.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAnimControllerDefault
#else
    public interface IAnimControllerDefault
#endif
    {
        /// <summary>
        /// Applies default configuration settings to the specified AnimatorController.
        /// </summary>
        /// <param name="controller">The AnimatorController to apply defaults to.</param>
        void ApplyToTargetController(AnimatorController controller);
    }

    /// <summary>
    /// Base class for ScriptableObject assets that define default AnimatorController configurations.
    /// Derived classes implement specific types of default configurations (e.g., face parameters, grab layers).
    /// </summary>
    [System.Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class AnimControllerDefaultAsset : ScriptableObject, IAnimControllerDefault
#else
    public abstract class AnimControllerDefaultAsset : ScriptableObject, IAnimControllerDefault
#endif
    {
        /// <summary>
        /// Reference AnimatorController that serves as the source of default configuration data.
        /// </summary>
        [FormerlySerializedAs("refController")] public AnimatorController RefController;

        /// <summary>
        /// Applies the default configuration from the reference controller to the target controller.
        /// This method must be implemented by derived classes to define specific application logic.
        /// </summary>
        /// <param name="controller">The target AnimatorController to apply defaults to.</param>
        public abstract void ApplyToTargetController(AnimatorController controller);
    }
}
