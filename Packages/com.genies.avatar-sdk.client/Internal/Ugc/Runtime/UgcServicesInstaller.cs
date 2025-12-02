using System.Collections;
using Genies.Assets.Services;
using Genies.ServiceManagement;
using Genies.Models;
using VContainer;

namespace Genies.Ugc
{
    /// <summary>
    /// Service installer for the Genies UGC package that configures dependency injection for UGC-related services.
    /// This installer registers UGC services with the VContainer dependency injection system,
    /// including asset providers for element containers and other core UGC functionality.
    /// </summary>
    [AutoResolve]
    public class UgcServicesInstaller : IGeniesInstaller
    {
        /// <summary>
        /// Installs UGC services into the dependency injection container.
        /// Registers the IAssetsProvider for ElementContainer with appropriate labeling and merging configuration.
        /// </summary>
        /// <param name="builder">The container builder used to register services.</param>
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAssetsProvider<ElementContainer>, LabeledAssetsProvider<ElementContainer>>(Lifetime.Singleton)
                   .WithParameter<IEnumerable>(new[] { "elementcontainer" })
                   .WithParameter(MergingMode.Intersection);

        }
    }
}
