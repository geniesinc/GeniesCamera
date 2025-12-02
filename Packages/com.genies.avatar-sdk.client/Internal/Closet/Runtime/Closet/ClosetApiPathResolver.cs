using System;
using Genies.Services.Configs;

namespace Genies.Closet
{
    /// <summary>
    /// API path resolver for closet services that provides the appropriate base URLs for different backend environments.
    /// This class implements <see cref="IApiClientPathResolver"/> to resolve Genies API endpoints for closet-related operations.
    /// </summary>
    public class ClosetApiPathResolver : IApiClientPathResolver
    {
        /// <summary>
        /// Gets the appropriate API base URL for the specified backend environment.
        /// </summary>
        /// <param name="environment">The target backend environment.</param>
        /// <returns>The base URL string for the specified environment.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported environment is provided.</exception>
        public string GetApiBaseUrl(BackendEnvironment environment)
        {
            switch (environment)
            {
                case BackendEnvironment.QA:
                    return "https://api.qa.genies.com";
                case BackendEnvironment.Prod:
                    return "https://api.genies.com";
                case BackendEnvironment.Dev:
                    return "https://api.dev.genies.com";
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }
    }
}
