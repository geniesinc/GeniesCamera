using System;
using Genies.ABTesting;

namespace Genies.Services.Configs
{
    public class DefaultComposerApiPathResolver : IApiClientPathResolver
    {
        private const string MobileApiDynamicConfigName = "mobile-client-api-config";
        private const string MobileApiDynamicConfigProdPathKey = "prodBasePath";

        private readonly IABTestingService _abTestingService;

        public DefaultComposerApiPathResolver(IABTestingService abTestingService)
        {
            _abTestingService = abTestingService;
        }

        public string GetApiBaseUrl(BackendEnvironment environment)
        {
            //Check statsig for override
            if (_abTestingService != null && environment == BackendEnvironment.Prod)
            {
                var overridePath = _abTestingService.GetFromConfig<string>(MobileApiDynamicConfigName, MobileApiDynamicConfigProdPathKey, null);

                if (!string.IsNullOrEmpty(overridePath))
                {
                    return overridePath;
                }
            }

            switch (environment)
            {
                case BackendEnvironment.QA:
                    return "https://composer-api.qa.genies.com/v1";
                case BackendEnvironment.Prod:
                    return "https://composer-api.genies.com/v1";
                case BackendEnvironment.Dev:
                    return "https://composer-api.dev.genies.com/v1/";
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }
    }
}