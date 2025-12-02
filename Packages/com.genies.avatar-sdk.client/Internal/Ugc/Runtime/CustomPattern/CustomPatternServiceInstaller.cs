using Genies.CloudSave;
using Genies.ServiceManagement;
using Genies.Services.Model;
using VContainer;

namespace Genies.Ugc.CustomPattern
{
    /// <summary>
    /// Service installer for the custom pattern functionality that configures dependency injection for pattern-related services.
    /// This installer registers pattern services including cloud save functionality and pattern loading services
    /// with the VContainer dependency injection system.
    /// </summary>
    [AutoResolve]
    public class CustomPatternServiceInstaller : IGeniesInstaller
    {
        /// <summary>
        /// Installs custom pattern services into the dependency injection container.
        /// Configures pattern cloud save services and pattern loader services.
        /// </summary>
        /// <param name="builder">The container builder used to register services.</param>
        public void Install(IContainerBuilder builder)
        {
            RegisterPatternsService(builder);
        }

        /// <summary>
        /// Registers pattern-related services including cloud save and pattern loading functionality.
        /// Sets up the cloud feature save service for patterns with proper serialization and ID handling.
        /// </summary>
        /// <param name="builder">The container builder used to register services.</param>
        private static void RegisterPatternsService(IContainerBuilder builder)
        {
            //Patterns
            builder.Register
                    (
                     _ =>
                     {
                         return new CloudFeatureSaveService<Pattern>
                             (
                              GameFeature.GameFeatureTypeEnum.UgcCustomPatterns,
                              new PatternCloudSaveJsonSerializer(),
                              (data, id) => data.TextureId = id,
                              data => data.TextureId
                             );
                     },
                     Lifetime.Singleton
                    )
                   .As<ICloudFeatureSaveService<Pattern>>();

            builder.Register<ICustomPatternService, CustomPatternRemoteLoaderService>(Lifetime.Singleton);
        }
    }
}
