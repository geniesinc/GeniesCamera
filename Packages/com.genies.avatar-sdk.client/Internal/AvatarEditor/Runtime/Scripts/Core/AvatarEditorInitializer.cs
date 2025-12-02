using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using UnityEngine;
using Genies.Avatars.Context;
using Genies.Avatars.Sdk;
using Genies.Avatars.Services.Flair;
using Genies.ServiceManagement;
using Genies.Ugc.CustomHair;
using Genies.CrashReporting;
using Genies.Services.Model;
using Genies.Ugc;
using Genies.CloudSave;
using Genies.DataRepositoryFramework;
using Genies.Inventory;
using Genies.Services.Configs;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Main initializer for the Avatar Editor that coordinates all avatar and editor loading.
    /// Can be customized by setting installer properties before calling InitializeAsync.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarEditorInitializer
#else
    public class AvatarEditorInitializer
#endif
    {
        /// <summary>
        /// The shared instance used by AvatarEditorSDK. Can be customized before initialization.
        /// </summary>
        public static AvatarEditorInitializer Instance { get; set; } = new AvatarEditorInitializer();

        public Action InitializationFinished;
        private UniTaskCompletionSource<bool> _initializationTask;
        private readonly object _initializationLock = new();
        private bool _isEditingLocal;
        public bool IsEditingLocal
        {
            get => _isEditingLocal;
            set => _isEditingLocal = value;
        }
        public bool Initialized
        {
            get
            {
                return _initializationTask != null &&
                       _initializationTask.Task.Status == UniTaskStatus.Succeeded &&
                       _initializationTask.Task.GetAwaiter().GetResult(); // Returns bool status
            }
        }

        public AssetServiceInstaller AssetServiceInstaller { get; set; }
        public CustomHairColorServiceInstaller CustomHairColorServiceInstaller { get; set; }
        public FlairCustomColorPresetServiceInstaller FlairCustomColorPresetServiceInstaller { get; set; }
        public AvatarEditorSdkInstaller AvatarEditorSdkInstaller { get; set; }

        private bool LoadResourceAssets(out Shader hairShader)
        {
            // Load hair shader from resources
            hairShader = Resources.Load<Shader>("MegaHair_P");
            if (hairShader == null)
            {
                CrashReporter.LogError("[AvatarEditorInitializer] Failed to load hair shader from Resources/MegaHair_P");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialization to use the avatar editor
        /// </summary>
        public async UniTask<bool> InitializeAsync()
        {
#if GENIES_INTERNAL && GENIES_DEV
            return await InitializeAsync(BackendEnvironment.Dev);
#else
            return await InitializeAsync(GeniesApiConfigManager.TargetEnvironment);
#endif
        }

        public async UniTask<bool> InitializeAsync(BackendEnvironment targetEnvironment)
        {
            if (_initializationTask != null)
            {
                return await _initializationTask.Task;
            }

            lock (_initializationLock)
            {
                if (_initializationTask == null)
                {
                    _initializationTask = new UniTaskCompletionSource<bool>();
                }
            }

            try
            {
                CrashReporter.Log("[AvatarEditorInitializer] Starting avatar editor initialization...");

                // Load resources
                bool resourcesLoaded = LoadResourceAssets(out Shader hairShader);
                if (!resourcesLoaded)
                {
                    _initializationTask.TrySetResult(false);
                    return false;
                }

                if (!ServiceManager.IsAppInitialized)
                {
                    var apiConfig = new GeniesApiConfig()
                    {
                        TargetEnv = targetEnvironment,
                    };

                    await ServiceManager.InitializeAppAsync(
                        customInstallers: RegisterAdditionalServices(hairShader, apiConfig),
                        disableAutoResolve: true);

                    await AvatarsContextProvider.GetOrCreateDefaultInstance();
                }
                else
                {
                    // Will go through changes with new inventory
                    new MetadataServicesRegister(hairShader).Register();

                    if (AvatarEditorSdkInstaller == null)
                    {
                        AvatarEditorSdkInstaller = new AvatarEditorSdkInstaller();
                    }

                    AvatarEditorSdkInstaller.Register();
                }

                CrashReporter.Log("[AvatarEditorInitializer] Avatar editor initialization complete!");

                InitializationFinished?.Invoke();
                _initializationTask.TrySetResult(true);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[AvatarEditorInitializer] Initialization failed: {ex}");
                _initializationTask.TrySetException(ex);
            }

            return await _initializationTask.Task;
        }

        private List<IGeniesInstaller> RegisterAdditionalServices(Shader hairShader, GeniesApiConfig apiConfig)
        {
            var services = new GeniesInstallersSetup(apiConfig).ConstructInstallersList();
            var additionalServices = new List<IGeniesInstaller>()
            {
                AssetServiceInstaller ?? new AssetServiceInstaller(),
                CustomHairColorServiceInstaller ?? new CustomHairColorServiceInstaller(hairShader),
                FlairCustomColorPresetServiceInstaller ?? new FlairCustomColorPresetServiceInstaller(),
                AvatarEditorSdkInstaller ?? new AvatarEditorSdkInstaller()
            };

            services.AddRange(additionalServices);
            return services;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class MetadataServicesRegister
#else
    public class MetadataServicesRegister
#endif
    {
        private Shader _hairShader;

        public MetadataServicesRegister(Shader hairShader)
        {
            _hairShader = hairShader;
        }

        public void Register()
        {
            // Register addressable assets service
            var assetsService = new AddressableAssetsService();
            assetsService.RegisterSelf().As<IAssetsService>();

            // Create avatars context
            AvatarsContextProvider.GetOrCreateDefaultInstance().Forget();

            // Custom hair color data repository
            var hairColorDataRepository = new CloudFeatureSaveService<CustomHairColorData>
            (
                GameFeature.GameFeatureTypeEnum.UgcCustomHair,
                new CustomHairCloudSaveJsonSerializer(),
                (data, id) => data.Id = id,
                data => data.Id
            );
            hairColorDataRepository.RegisterSelf().As<IDataRepository<CustomHairColorData>>();

            // Hair color service
            var hairColorService = new HairColorService(
                _hairShader,
                assetsService,
                hairColorDataRepository,
                ServiceManager.Get<IDefaultInventoryService>());

            hairColorService.RegisterSelf().As<HairColorService>();

            // Flair color service
            var flairColorDataRepository = new CloudFeatureSaveService<FlairColorPreset>
            (
                GameFeature.GameFeatureTypeEnum.CustomFlairColors,
                new FlairColorPresetCloudSaveJsonSerializer(),
                (data, id) => data.Id = id,
                data => data.Id
            );
            flairColorDataRepository.RegisterSelf().As<IDataRepository<FlairColorPreset>>();

            var flairColorService = new FlairCustomColorPresetService(flairColorDataRepository);
            flairColorService.RegisterSelf().As<IFlairCustomColorPresetService>();

        }
    }
}
