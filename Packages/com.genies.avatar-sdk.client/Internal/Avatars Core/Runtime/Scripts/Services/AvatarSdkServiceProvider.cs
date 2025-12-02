using Genies.ServiceManagement;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Provides access to the Avatar SDK service instance and manages service initialization.
    /// This static class serves as the main entry point for accessing Avatar SDK functionality.
    /// </summary>
    internal static class AvatarSdkServiceProvider
    {
        /// <summary>
        /// Gets the currently registered Avatar SDK service instance.
        /// </summary>
        /// <returns>The Avatar SDK service instance, or null if not initialized.</returns>
        public static IGeniesAvatarSdkService Instance => ServiceManager.Get<IGeniesAvatarSdkService>();
    }
}
