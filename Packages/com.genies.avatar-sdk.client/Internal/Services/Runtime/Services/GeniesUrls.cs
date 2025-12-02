using System;
using Genies.Services.Configs;

namespace Genies.Utilities
{
    public static class GeniesUrls
    {
        public static string CloudfrontUrl
        {
            get
            {
                return GeniesApiConfigManager.TargetEnvironment switch
                {
                    BackendEnvironment.QA or BackendEnvironment.Dev => "https://d1osytbwwessyk.cloudfront.net",
                    BackendEnvironment.Prod => "https://d2qi2yjjvd8bhg.cloudfront.net",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}
