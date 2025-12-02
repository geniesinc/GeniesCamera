using Genies.ServiceManagement;
using VContainer;

namespace Genies.Looks.Service
{
    /// <summary>
    /// Service installer for the Genies Looks package that configures dependency injection for looks-related services.
    /// This installer registers looks services with the VContainer dependency injection system.
    /// </summary>
    [AutoResolve]
    public class LooksServicesInstaller : IGeniesInstaller
    {
        /// <summary>
        /// Installs looks services into the dependency injection container.
        /// Registers the ILooksService interface with the LooksService implementation as a singleton with local flag set to false.
        /// </summary>
        /// <param name="builder">The container builder used to register services.</param>
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ILooksService, LooksService>(Lifetime.Singleton).WithParameter(false);
        }
    }
}
