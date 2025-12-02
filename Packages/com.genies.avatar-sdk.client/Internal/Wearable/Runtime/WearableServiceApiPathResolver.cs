using System;
using Genies.Services.Configs;

namespace Genies.Wearables
{
    /// <summary>
    /// API path resolver for the wearable service that provides environment-specific base URLs.
    /// Implements IApiClientPathResolver to support different backend environments (QA, Production, Development).
    /// </summary>
    public class WearableServiceApiPathResolver : IApiClientPathResolver
    {
        /// <summary>
        /// Gets the API base URL for the specified backend environment.
        /// </summary>
        /// <param name="environment">The target backend environment (QA, Prod, Dev).</param>
        /// <returns>The base URL string for the specified environment.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported environment is specified.</exception>
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
