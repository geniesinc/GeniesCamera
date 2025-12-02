using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Closet;
using Genies.CloudSave;
using Genies.DiskCaching;
using Genies.Login.Native;
using Genies.S3Service;
using Genies.ServiceManagement;
using Genies.Services.Api;
using Genies.Services.Model;
using Genies.Ugc;
using Genies.Ugc.CustomHair;
using Genies.Ugc.CustomPattern;
using Genies.Ugc.CustomSkin;
using Genies.Utilities;
using Genies.Utilities.Internal;
using Genies.Wearables;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Initializes the <see cref="DefaultAvatarsContext"/>.<see cref="DefaultAvatarsContext.Instance"/>. If the default instance
    /// is already initialized nothing will be done.
    /// <br/><br/>
    /// User login is optional, if the <see cref="AccountServiceProvider"/>.<see cref="AccountServiceProvider.Instance"/>
    /// is not initialized then any features tied to user accounts will not be available (UGC wearables, custom patterns...).
    /// </summary>
    public sealed class AvatarsContextInitializer : Initializer
    {
        protected override string _InitializationSuccessMessage => "Created a default AvatarsContext";

        [SerializeField, Tooltip("If true, this initializer will require a content catalog to be initialized. Set it to false if only testing builtin content for faster loading times")]
        private bool useAddressablesContent = true;
        [SerializeField, Tooltip("If true, assets on Resource folders will also be included as content for avatars")]
        private bool useResourcesContent = true;
        [SerializeField, Tooltip("If true, asset bundles added to the AssetBundlesAssetsService static API will also be included as content for avatars")]
        private bool useAssetBundles = false;
        [SerializeField, Tooltip("Optional. If specified, the assets declared here will also be included as content for avatars")]
        private BuiltinAssetsAsset builtinAssets;
#if UNITY_EDITOR
        [SerializeField, Tooltip("(Editor only) If true, assets on the project will be included (they can be referenced by GUID or asset path)")]
        private bool useAssetDatabase = false;
#endif

        [SerializeField, Tooltip("If specified, a RS Refitting Service implementation will used instead of the legacy one")]
        private ReferenceShapesLoaderAsset referenceShapesLoader;

        private GeniesAppStateManager _AppStateManager => this.GetService<GeniesAppStateManager>();

        protected override async UniTask InitializeAsync()
        {
            // skip if the default instance is already initialized
            if (DefaultAvatarsContext.Instance is not null)
            {
                return;
            }

            //check if the users already logged and finish the onboarding before loading the dependencies
            var didCompleteLogin = GeniesLoginSdk.IsUserSignedIn();

            var initializeNonLogged = !didCompleteLogin;
            if (initializeNonLogged)
            {
                // create a context without user specific features (will not load any custom patterns, UGC wearables, etc...)
                IAssetsService assetsService = InitializeAssetsService();
                IRefittingService refittingService = referenceShapesLoader ? new RsRefittingService(referenceShapesLoader) : null;
                DefaultAvatarsContext.Instance = await AvatarsContextFactory.CreateContextAsync(assetsService: assetsService, refittingService: refittingService);
            }
            else
            {
                var userId = await GeniesLoginSdk.GetUserIdAsync();
                // if there is an initialized account service then create a context with user specific features
                await InitializeLoggedInAvatarsContextAsync(userId);
            }
        }

        private IAssetsService InitializeAssetsService()
        {
            var services = new List<IAssetsService>(4);

            if (useAddressablesContent)
            {
                services.Add(new AddressableAssetsService());
            }
            else if (useResourcesContent) // apparently Addressables already loads from Resources paths too, so we only need this if not including addressables
            {
                services.Add(new ResourceAssetsService());
            }

            if (useAssetBundles)
            {
                services.Add(AssetBundlesAssetsService.Service);
            }

            if (builtinAssets)
            {
                services.Add(new BuiltinAssetsService(builtinAssets.BuiltinAssets));
            }

#if UNITY_EDITOR
            if (useAssetDatabase)
                services.Add(new AssetDatabaseAssetsService());
#endif

            // if only one of the sources is selected then return its specific implementation directly
            if (services.Count == 1)
            {
                return services[0];
            }

            return new GroupedAssetsService(services);
        }

        private async UniTask InitializeLoggedInAvatarsContextAsync(string userId)
        {
            // core services
            IAssetsService assetsService = InitializeAssetsService();
            var s3Service = new GeniesS3Service(
                new ImageApi(),
                async () => await GeniesLoginSdk.GetUserIdAsync(),
                DiskCacheOptions.Default
            );

            // ugc wearable definition service
            var closetService = this.GetService<IClosetService>();
            var wearableService = new WearableService(userId, closetService);
            var ugcWearableDefinitionService = new UgcWearableDefinitionService(wearableService);

            // custom hair colors
            var customHairDataRepository = new CloudFeatureSaveService<CustomHairColorData>(
                GameFeature.GameFeatureTypeEnum.UgcCustomHair,
                new CustomHairCloudSaveJsonSerializer(),
                (data, id) => data.Id = id,
                data => data.Id
            );
            var materialLoader = new MaterialLoaderWithUgcHairColors(assetsService, customHairDataRepository);

            // custom skin colors
            var skinColorDataRepository = new CloudFeatureSaveService<SkinColorData>(
                GameFeature.GameFeatureTypeEnum.UgcCustomSkin,
                new CustomSkinCloudSaveJsonSerializer(),
                (data, id) => data.Id = id,
                data => data.Id
            );
            var skinColorLoader = new SkinColorLoaderWithUgcColors(assetsService, skinColorDataRepository);

            // ugc projected textures
            var projectedTextureService = new ProjectedTextureRemoteLoaderService(s3Service, new ImageLoader());
            var projectedTexturesProvider = new ProjectedTexturesProvider(projectedTextureService);

            // custom patterns
            var customPatternsDataRepository = new CloudFeatureSaveService<Pattern>(
                GameFeature.GameFeatureTypeEnum.UgcCustomPatterns,
                new PatternCloudSaveJsonSerializer(),
                (data, id) => data.TextureId = id,
                data => data.TextureId
            );
            var customPatternService = new CustomPatternRemoteLoaderService(s3Service, customPatternsDataRepository, new ImageLoader());
            var nonCustomPatternsProvider = new LabeledAssetsProvider<Texture2D>(assetsService, new[] { "ugcwpattern" }, MergingMode.Intersection);
            var ugcPatternProvider = new UgcPatternsProvider(customPatternService, nonCustomPatternsProvider);

            // refitting service
            IRefittingService refittingService = referenceShapesLoader ? new RsRefittingService(referenceShapesLoader) : null;

            DefaultAvatarsContext.Instance = await AvatarsContextFactory.CreateContextAsync(
                assetsService: assetsService,
                ugcWearableDefinitionService: ugcWearableDefinitionService,
                materialLoader: materialLoader,
                skinColorLoader: skinColorLoader,
                projectedTexturesProvider: projectedTexturesProvider,
                ugcPatternsProvider: ugcPatternProvider,
                refittingService: refittingService
            );
        }
    }
}
