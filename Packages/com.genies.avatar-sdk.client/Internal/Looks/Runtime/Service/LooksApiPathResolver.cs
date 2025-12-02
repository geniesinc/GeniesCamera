using System;
using Genies.Services.Configs;

namespace Genies.Looks.Service
{
    /// <summary>
    /// API path resolver for looks services that provides the appropriate base URLs for different backend environments.
    /// This class implements <see cref="IApiClientPathResolver"/> to resolve Genies API endpoints for looks-related operations.
    /// </summary>
    public class LooksApiPathResolver : IApiClientPathResolver
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
