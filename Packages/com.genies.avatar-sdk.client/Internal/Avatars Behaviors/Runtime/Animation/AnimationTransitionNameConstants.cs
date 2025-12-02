

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Contains constant string values for animation transition names used across the avatar behavior system.
    /// These constants provide standardized names for different idle animation transitions based on gender and avatar type.
    /// </summary>
    public static class AnimationTransitionNameConstants
    {
        [System.Obsolete("Use 'MaleTransitionToFloating' instead.")]
        public const string MALE_TRANSITION_TO_FLOATING = "male-idle-alt-1";
        [System.Obsolete("Use 'FemaleTransitionToFloating' instead.")]
        public const string FEMALE_TRANSITION_TO_FLOATING = "female-idle-alt-1";
        [System.Obsolete("Use 'UnifiedTransitionToFloating' instead.")]
        public const string UNIFIED_TRANSITION_TO_FLOATING = "unified-idle-alt-1";
        [System.Obsolete("Use 'MaleTransitionToIdle2' instead.")]
        public const string MALE_TRANSITION_TO_IDLE2 = "male-idle-alt-2";
        [System.Obsolete("Use 'FemaleTransitionToIdle2' instead.")]
        public const string FEMALE_TRANSITION_TO_IDLE2 = "female-idle-alt-2";
        [System.Obsolete("Use 'UnifiedTransitionToIdle2' instead.")]
        public const string UNIFIED_TRANSITION_TO_IDLE2 = "unified-idle-alt-2";
        [System.Obsolete("Use 'UnifiedTransitionToIdle3' instead.")]
        public const string UNIFIED_TRANSITION_TO_IDLE3 = "unified-idle-alt-3";

        /// <summary>
        /// Animation transition name for male avatars transitioning to floating idle animation.
        /// </summary>
        public const string MaleTransitionToFloating = "male-idle-alt-1";

        /// <summary>
        /// Animation transition name for female avatars transitioning to floating idle animation.
        /// </summary>
        public const string FemaleTransitionToFloating = "female-idle-alt-1";

        /// <summary>
        /// Animation transition name for unified avatars transitioning to floating idle animation.
        /// </summary>
        public const string UnifiedTransitionToFloating = "unified-idle-alt-1";

        /// <summary>
        /// Animation transition name for male avatars transitioning to second idle variation.
        /// </summary>
        public const string MaleTransitionToIdle2 = "male-idle-alt-2";

        /// <summary>
        /// Animation transition name for female avatars transitioning to second idle variation.
        /// </summary>
        public const string FemaleTransitionToIdle2 = "female-idle-alt-2";

        /// <summary>
        /// Animation transition name for unified avatars transitioning to second idle variation.
        /// </summary>
        public const string UnifiedTransitionToIdle2 = "unified-idle-alt-2";

        /// <summary>
        /// Animation transition name for unified avatars transitioning to third idle variation.
        /// </summary>
        public const string UnifiedTransitionToIdle3 = "unified-idle-alt-3";

    }
}

