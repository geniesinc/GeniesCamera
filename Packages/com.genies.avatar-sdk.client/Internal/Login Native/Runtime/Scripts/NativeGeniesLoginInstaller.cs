using System;
using Cysharp.Threading.Tasks;
using Genies.NativeAPI;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Login.Native
{
    [AutoResolve]
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NativeGeniesLoginInstaller : IGeniesLoginInstaller, IGeniesInitializer
#else
    public class NativeGeniesLoginInstaller : IGeniesLoginInstaller, IGeniesInitializer
#endif
    {
        [SerializeField] private string _baseUrl = "https://api.genies.com";
        [SerializeField] private string _appName = "GeniesSdk";
        [SerializeField] private bool _allowSignUp = true;

        public string BaseUrl
        {
            get => _baseUrl;
            set => _baseUrl = value;
        }

        /// <summary>
        /// Can be anything. Used as key for keychain.
        /// </summary>
        public string AppName
        {
            get => _appName;
            set => _appName = value;
        }

        public bool AllowSignUp
        {
            get => _allowSignUp;
            set => _allowSignUp = value;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.Register<IGeniesLogin, GeniesNativeAPIAuth>(Lifetime.Singleton);
        }

        public async UniTask Initialize()
        {
            if (GeniesLoginSdk.IsInitialized is false)
            {
                await GeniesLoginSdk.InitializeAsync(BaseUrl, AppName, AllowSignUp);
            }
        }
    }
}
