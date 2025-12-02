namespace Genies.Animations
{
    /// <summary>
    /// Catalog of Virtual Camera (Cinemachine) within the Genies Animation package
    /// </summary>
    /// <seealso cref="GeniesVirtualCamera"/>
    /// <seealso cref="LookAnimationController"/>
    public enum AnimationVirtualCameraCatalog
    {
        /// <summary>
        /// Option to set the main camera to follow the animated Camera (with animation) under the avatar object
        /// </summary>
        AnimatedCamera = 0,

        /// <summary>
        /// Option to have the main camera focus on the full body
        /// </summary>
        FullBodyFocusCamera = 1,
    }
}


