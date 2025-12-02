using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Login;
using Genies.Login.Native;
using Genies.ServiceManagement;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class UserLookDefinitionSource : IAvatarDefinitionSource
    {
        private static LookApi _lookApi;

        private readonly bool _getDrafts;

        public UserLookDefinitionSource(bool getDrafts)
        {
            _getDrafts = getDrafts;
        }

        public async UniTask<string> GetDefinitionAsync()
        {
            string userId = await GeniesLoginSdk.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"[{nameof(UserLookDefinitionSource)}] there is no logged in user");
                return null;
            }

            InitializeLookApi();

            // try to get all user looks from the api
            List<Look> looks = await GetAllUserLooksAsync(userId);
            if (looks is null)
            {
                return null;
            }

            Look look = null;
            var requiredStatus = _getDrafts ? Look.StatusEnum.Draft : Look.StatusEnum.Published;

            for (int i = 0; i < looks.Count; ++i)
            {
                if (looks[i] is null || looks[i].Status != requiredStatus)
                {
                    continue;
                }

                if (look is null || look.Created < looks[i].Created)
                {
                    look = looks[i];
                }
            }

            if (look is not null)
            {
                return look.AvatarDefinition;
            }

            Debug.LogError($"[{nameof(UserLookDefinitionSource)}] the user has no published looks");
            return null;
        }

        private static async UniTask<List<Look>> GetAllUserLooksAsync(string userId)
        {
            var looks = new List<Look>();

            try
            {
                var page = new LookListPagination();

                do
                {
                    page = await _lookApi.GetLooksByAsync(userId, page.NextCursor);
                    if (page?.Looks is not null)
                    {
                        looks.AddRange(page.Looks);
                    }
                } while (page is not null && !string.IsNullOrEmpty(page.NextCursor));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UserLookDefinitionSource)}] something went wrong while fetching user looks.\n{exception}");
                return null;
            }

            return looks;
        }

        private static void InitializeLookApi()
        {
            if (_lookApi is not null)
            {
                return;
            }

            //Looks api requires a specific configuration as opposed to previous composer apis. The path and authorization are both different.
            IApiClientPathResolver apiPathResolver = new LooksApiPathResolver();
            var config = new Configuration
            {
                BasePath = apiPathResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment),
                AccessToken = GeniesLoginSdk.AuthAccessToken,
            };

            _lookApi = new LookApi(config);
        }

        private sealed class LooksApiPathResolver : IApiClientPathResolver
        {
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
}
