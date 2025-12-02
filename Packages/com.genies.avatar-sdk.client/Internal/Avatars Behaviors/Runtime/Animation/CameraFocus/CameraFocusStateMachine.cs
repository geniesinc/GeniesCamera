using System.Collections.Generic;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Static state machine that manages camera focus states and determines which animation transitions are allowed based on the current camera focus.
    /// This class controls which idle animations can be triggered depending on whether the camera is focused on the complete avatar or specific categories.
    /// </summary>
    public static class CameraFocusStateMachine
    {
        static CameraFocusStateMachine()
        {
            _allowableAnimationTransitionsMapping.Add(
                CameraFocusState.CompleteAvatarFocus, new HashSet<string>()
                {
                    AnimationTransitionNameConstants.MaleTransitionToFloating,
                    AnimationTransitionNameConstants.FemaleTransitionToFloating,
                    AnimationTransitionNameConstants.UnifiedTransitionToFloating,
                    AnimationTransitionNameConstants.MaleTransitionToIdle2,
                    AnimationTransitionNameConstants.FemaleTransitionToIdle2,
                    AnimationTransitionNameConstants.UnifiedTransitionToIdle2,
                    AnimationTransitionNameConstants.UnifiedTransitionToIdle3
                });

            _allowableAnimationTransitionsMapping.Add(
                CameraFocusState.AvatarCategoryFocus, new HashSet<string>());
        }

        /// <summary>
        /// Defines the different camera focus states that determine which animation transitions are available.
        /// </summary>
        public enum CameraFocusState
        {
            /// <summary>
            /// Camera is focused on the complete avatar, allowing all animation transitions.
            /// </summary>
            CompleteAvatarFocus,

            /// <summary>
            /// Camera is focused on specific avatar categories (e.g., face, body parts), restricting available animations.
            /// </summary>
            AvatarCategoryFocus
        }

        private static CameraFocusState _currentCameraFocusState = CameraFocusState.CompleteAvatarFocus;

        private static Dictionary<CameraFocusState, HashSet<string>> _allowableAnimationTransitionsMapping =
            new Dictionary<CameraFocusState, HashSet<string>>();

        /// <summary>
        /// Changes the current camera focus state, which determines which animation transitions are allowed.
        /// </summary>
        /// <param name="cameraFocusState">The new camera focus state to set.</param>
        public static void ChangeState(CameraFocusState cameraFocusState)
        {
            _currentCameraFocusState = cameraFocusState;
        }

        /// <summary>
        /// Gets the array of animation transition names that are currently allowed based on the active camera focus state.
        /// </summary>
        /// <returns>An array of strings containing the names of allowable animation transitions.</returns>
        public static string[] GetAllowableAnimationTransitions()
        {
            HashSet<string> hashSet = _allowableAnimationTransitionsMapping[_currentCameraFocusState];
            string[] array = new string[hashSet.Count];
            _allowableAnimationTransitionsMapping[_currentCameraFocusState].CopyTo(array);
            return array;
        }

    }
}
