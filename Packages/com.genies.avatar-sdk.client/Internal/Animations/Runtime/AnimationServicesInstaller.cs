using Genies.ServiceManagement;
using VContainer;

namespace Genies.Animations
{
    /// <summary>
    /// Service installer for the Genies Animations package that configures dependency injection for animation services.
    /// This installer registers animation-related services with the VContainer dependency injection system.
    /// </summary>
    [AutoResolve]
    public class AnimationServicesInstaller : IGeniesInstaller
    {
        /// <summary>
        /// Installs animation services into the dependency injection container.
        /// Registers the IAnimationLoader interface with its default AddressablesAnimationLoader implementation as a singleton.
        /// </summary>
        /// <param name="builder">The container builder used to register services.</param>
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAnimationLoader, AddressablesAnimationLoader>(Lifetime.Singleton);
        }
    }
}
