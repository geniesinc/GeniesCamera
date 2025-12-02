using Genies.ServiceManagement;
using VContainer;

namespace Genies.Closet
{
    /// <summary>
    /// Service installer for the Genies Closet package that configures dependency injection for closet services.
    /// This installer registers closet-related services with the VContainer dependency injection system.
    /// </summary>
    [AutoResolve]
    public class ClosetServiceInstaller : IGeniesInstaller
    {
        public int OperationOrder => DefaultInstallationGroups.PostCoreServices;

        /// <summary>
        /// Installs closet services into the dependency injection container.
        /// Registers the IClosetService interface with the ClosetService implementation as a singleton.
        /// </summary>
        /// <param name="builder">The container builder used to register services.</param>
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IClosetService, ClosetService>(Lifetime.Singleton);
        }
    }
}
