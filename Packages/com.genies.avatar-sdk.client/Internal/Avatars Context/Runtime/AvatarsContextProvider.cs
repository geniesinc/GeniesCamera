using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Closet;
using Genies.CloudSave;
using Genies.Addressables;
using Genies.DiskCaching;
using Genies.Login.Native;
using Genies.S3Service;
using Genies.Services.Api;
using Genies.Services.Model;
using Genies.Ugc;
using Genies.Ugc.CustomHair;
using Genies.Ugc.CustomPattern;
using Genies.Ugc.CustomSkin;
using Genies.Utilities;
using Genies.Wearables;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public class AvatarsContextProvider
    {
        public static async UniTask<AvatarsContext> GetOrCreateDefaultInstance()
        {
            // skip if the default instance is already initialized
            if (DefaultAvatarsContext.Instance is not null)
            {
                return DefaultAvatarsContext.Instance;
            }

            IAssetsService assetsService = new AddressableAssetsService();
            
            return await CreateDefaultInstance(assetsService);
        }
        
        public static async UniTask<AvatarsContext> GetOrCreateDefaultInstance(IAssetsService assetsService)
        {
            // skip if the default instance is already initialized
            if (DefaultAvatarsContext.Instance is not null)
            {
                return DefaultAvatarsContext.Instance;
            }

            return await CreateDefaultInstance(assetsService);
        }

        private static async UniTask<AvatarsContext> CreateDefaultInstance(IAssetsService assetsService)
        {
            // skip if the default instance is already initialized
            if (DefaultAvatarsContext.Instance is not null)
            {
                return DefaultAvatarsContext.Instance;
            }

            // Get Addressables Dependency
            await AddressablesCatalogProvider.InitializeAddressablesAndCatalogsAsync();

            var initializeNonLogged = !GeniesLoginSdk.IsUserSignedIn();
            if (initializeNonLogged)
            {
                // create a context without user specific features (will not load any custom patterns, UGC wearables, etc...)
                IRefittingService refittingService = null;
                DefaultAvatarsContext.Instance = await AvatarsContextFactory.CreateContextAsync(assetsService: assetsService, refittingService: refittingService);
            }
            else
            {
                // if there is an initialized account service then create a context with user specific features
                await InitializeDefaultLoggedInAvatarsContextAsync();
            }

            return DefaultAvatarsContext.Instance;
        }

        private static async UniTask InitializeDefaultLoggedInAvatarsContextAsync()
        {
            var userId = await GeniesLoginSdk.GetUserIdAsync();
            // core services
            IAssetsService assetsService = new AddressableAssetsService();
            var s3Service = new GeniesS3Service(
                new ImageApi(),
                async () => await GeniesLoginSdk.GetUserIdAsync(),
                DiskCacheOptions.Default
            );

            // ugc wearable definition service
            var closetService = new ClosetService();
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
            IRefittingService refittingService = null;

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
