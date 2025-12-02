using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Utilities.Internal;
using Genies.Services.Configs;

namespace Genies.Addressables
{
    public class CustomBackendEnvInitializer : Initializer
    {
        [SerializeField] private BackendEnvironment backendEnvironment = BackendEnvironment.Prod;

        protected override string _InitializationSuccessMessage => $"Loaded Custom Backend Environment {backendEnvironment}";

        protected override UniTask InitializeAsync()
        {
            GeniesApiConfigManager.SetApiConfig(new GeniesApiConfig
            {
                TargetEnv = backendEnvironment
            }, true);

            return UniTask.CompletedTask;
        }
    }
}
