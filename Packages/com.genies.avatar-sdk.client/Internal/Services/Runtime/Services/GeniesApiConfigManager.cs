using System;
using System.Collections.Generic;
using Genies.Login.Native;
using UnityEngine;
using SDKClient = Genies.SDKServices.Client.Configuration;
using GeniesApiClient = Genies.Services.Client.Configuration;

namespace Genies.Services.Configs
{
    [Serializable]
    public class GeniesApiConfig
    {
        public BackendEnvironment TargetEnv;
    }

    public static class GeniesApiConfigManager
    {
        private static readonly Dictionary<BackendEnvironment, string> EnvLookup = new()
        {
            { BackendEnvironment.Dev, "https://api.dev.genies.com" },
            { BackendEnvironment.QA, "https://api.qa.genies.com"},
            { BackendEnvironment.Prod, "https://api.genies.com" },
        };

        private static GeniesApiConfig _currentConfig;
        public static BackendEnvironment TargetEnvironment => _currentConfig?.TargetEnv ?? BackendEnvironment.Prod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GeniesLoginSdk.UserLoggedIn -= GeniesLoginSdkOnUserLoggedIn;
            GeniesLoginSdk.UserLoggedIn += GeniesLoginSdkOnUserLoggedIn;

            GeniesLoginSdk.OnTokenRefresh -= GeniesLoginSdkOnTokensRefresh;
            GeniesLoginSdk.OnTokenRefresh += GeniesLoginSdkOnTokensRefresh;
        }

        private static void GeniesLoginSdkOnUserLoggedIn()
        {
            SDKClient sdkClient = SDKClient.Default;
            sdkClient.AccessToken = GeniesLoginSdk.AuthAccessToken;

            GeniesApiClient geniesApiClient = GeniesApiClient.Default;
            geniesApiClient.AccessToken = GeniesLoginSdk.AuthAccessToken;
        }

        private static void GeniesLoginSdkOnTokensRefresh(string args)
        {
            SDKClient sdkClient = SDKClient.Default;
            sdkClient.AccessToken = GeniesLoginSdk.AuthAccessToken;

            GeniesApiClient geniesApiClient = GeniesApiClient.Default;
            geniesApiClient.AccessToken = GeniesLoginSdk.AuthAccessToken;
        }

        public static void SetApiConfig(GeniesApiConfig config, bool overwriteCurrent = true)
        {
            if (_currentConfig != null && !overwriteCurrent)
            {
                return;
            }

            _currentConfig = config;

            SDKClient sdkClient = SDKClient.Default;
            sdkClient.BasePath = ResolveApiPath(TargetEnvironment);

            GeniesApiClient geniesApiClient = GeniesApiClient.Default;
            geniesApiClient.BasePath = ResolveApiPath(TargetEnvironment);
        }

        public static string GetApiPath()
        {
            return ResolveApiPath(TargetEnvironment);
        }

        public static string ResolveApiPath(BackendEnvironment env)
        {
            if (EnvLookup.TryGetValue(env, out var value))
            {
                return value;
            }
            throw new ArgumentOutOfRangeException(nameof(env), env, null);
        }
    }
}
