namespace Genies.CameraSystem
{
    /// <summary>
    /// Interface for setting up new virtual camera classes.
    /// </summary>
    public interface ICameraType
    {
        /// <summary>
        /// Sets up components and dependencies for the camera
        /// </summary>
        public void ConfigureVirtualCamera();
        public void ToggleBehaviour(bool value);
    }
}
