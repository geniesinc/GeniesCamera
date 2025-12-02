using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.DiskCaching;
using Genies.S3Service;
using Genies.Components.ShaderlessTools;
using Genies.Services.Api;
using Genies.Ugc;
using Genies.Ugc.CustomPattern;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public static class AvatarsContextFactory
    {
        private const string _ugcOutfitAssetBuilderPath = "UgcOutfitAssetBuilder";

        /// <summary>
        /// Creates and returns a <see cref="AvatarsContext"/> instance that uses our Addressables content build
        /// based on our DataModels package. All parameters are optional and are meant to provide extra customization
        /// options (like the ability to load user created UGC wearables).
        /// </summary>
        /// <param name="assetsService">Assets service to be used to load the data model assets. This parameter
        ///     is optional. If not defined, an <see cref="AddressableAssetsService"/> will be used.</param>
        /// <param name="speciesLoader"></param>
        /// <param name="materialLoader"></param>
        /// <param name="skinColorLoader"></param>
        /// <param name="tattooLoader"></param>
        /// <param name="makeupLoader"></param>
        /// <param name="makeupColorLoader"></param>
        /// <param name="decoratedSkinMaterialBaker"></param>
        /// <param name="refittingService"></param>
        /// <param name="ugcTemplateDataService"></param>
        /// <param name="ugcMaterialsProvider">Assets provider for the UGC material textures. This parameter
        ///     is optional. If not defined, the default one with materials from the content build will be used.</param>
        /// <param name="ugcPatternsProvider">Assets provider for the UGC pattern textures. This parameter
        ///     is optional. If not defined, the default one with patterns from the content build will be used.</param>
        /// <param name="projectedTexturesProvider">Projected textures from s3. Optional. If not defined no
        /// projected textures will be loaded.</param>
        /// <param name="ugcWearableDefinitionService">Service to fetch UGC wearable definitions by ID.
        ///     This parameter is optional. If not defined, the loaded avatars won't support UGC outfit assets.</param>
        /// <param name="staticWearableLoader"></param>
        /// <param name="shaderlessService">Service used to load shaderless material data from shaderless assets</param>
        /// <returns></returns>
        public static UniTask<Avatars.AvatarsContext> CreateContextAsync(
            IAssetsService assetsService = null,
            IShaderlessAssetService shaderlessService = null,
            IAssetLoader<SpeciesAsset> speciesLoader = null,
            ISlottedAssetLoader<MaterialAsset> materialLoader = null,
            IAssetLoader<ColorAsset> skinColorLoader = null,
            IAssetLoader<Texture2DAsset> tattooLoader = null,
            IAssetLoader<Texture2DAsset> makeupLoader = null,
            IAssetLoader<MakeupColorAsset> makeupColorLoader = null,
            ISlottedAssetLoader<FlairAsset> flairLoader = null,
            IRefittingService refittingService = null,
            IUgcTemplateDataService ugcTemplateDataService = null,
            IAssetsProvider<Texture2D> ugcMaterialsProvider = null,
            IAssetsProvider<Texture2D> ugcPatternsProvider = null,
            IAssetsProvider<Texture2D> projectedTexturesProvider = null,
            IUgcWearableDefinitionService ugcWearableDefinitionService = null,
            IAssetLoader<StaticWearableAsset> staticWearableLoader = null,
            IAssetLoader<GearAsset> gearAssetLoader = null,
            IAssetLoader<GearElementAsset> gearElementAssetLoader = null,
            IAssetLoader<BlendShapeAsset> blendShapeLoader = null,
            IAssetLoader<BlendShapePresetAsset> blendShapePresetLoader = null,
            ISubSpeciesLoader subSpeciesLoader = null)
        {
            // initialize default dependencies that were not provided in parameters
            assetsService ??= new AddressableAssetsService();
            ugcMaterialsProvider ??= new LabeledAssetsProvider<Texture2D>(assetsService, new[] { "ugcwmaterial" }, MergingMode.Intersection);

            if (ugcPatternsProvider is null)
            {
                var s3Service = new GeniesS3Service(
                    new ImageApi(),
                    () => UniTask.FromResult(string.Empty),
                    DiskCacheOptions.Default
                );
                var customPatternService = new CustomPatternRemoteLoaderService(s3Service, null, new ImageLoader());
                var nonCustomPatternsProvider = new LabeledAssetsProvider<Texture2D>(assetsService, new[] { "ugcwpattern" }, MergingMode.Intersection);
                ugcPatternsProvider = new UgcPatternsProvider(customPatternService, nonCustomPatternsProvider);
            }

            if (projectedTexturesProvider is null)
            {
                var s3Service = new GeniesS3Service(
                    new ImageApi(),
                    () => UniTask.FromResult(string.Empty),
                    DiskCacheOptions.Default
                );
                projectedTexturesProvider = new ProjectedTexturesProvider(new ProjectedTextureRemoteLoaderService(s3Service, new ImageLoader()));
            }

            // gear outfit asset loader
            gearAssetLoader ??= new GearAssetLoader(assetsService);
            gearElementAssetLoader ??= new GearElementAssetLoader(assetsService);
            var gearSkinOutfitAssetLoader = new GearOutfitAssetLoader(gearAssetLoader, gearElementAssetLoader);

            // static outfit asset loader
            staticWearableLoader ??= new StaticWearableLoader(assetsService);
            var staticOutfitAssetLoader = new StaticOutfitAssetLoader(staticWearableLoader);

            // ugc-default outfit asset loader
            ugcTemplateDataService ??= new UgcTemplateDataService(assetsService);
            var ugcTemplateLoader = new UgcTemplateLoader(assetsService, ugcTemplateDataService);
            var ugcElementLoader = new UgcElementLoader(assetsService, ugcTemplateDataService);
            var megaMaterialBuilder = new MegaMaterialBuilder(ugcElementLoader, ugcMaterialsProvider, ugcPatternsProvider, projectedTexturesProvider);
            var ugcOutfitAssetBuilder = Resources.Load<UgcOutfitAssetBuilder>(_ugcOutfitAssetBuilderPath);
            ugcOutfitAssetBuilder.Initialize(ugcTemplateLoader, ugcElementLoader, megaMaterialBuilder);
            var ugcDefaultOutfitAssetLoader = new UgcDefaultOutfitAssetLoader(ugcOutfitAssetBuilder, ugcTemplateLoader);

            // ugc outfit asset loader
            ugcWearableDefinitionService ??= new EmbeddedUgcWearableDefinitionService();
            var ugcOutfitAssetLoader = new UgcOutfitAssetLoader(ugcOutfitAssetBuilder, ugcWearableDefinitionService);

            // allow to use the refitting service sandbox
            if (RefittingServiceSandbox.Instance)
            {
                RefittingServiceSandbox.Instance.SetLegacyService(new RefittingService(new UtilityVectorService(assetsService)));
                refittingService = RefittingServiceSandbox.Instance;
            }

            // genie context services
            speciesLoader ??= new SpeciesLoader(assetsService);
            materialLoader ??= new MaterialLoader(assetsService);
            tattooLoader ??= new TattooLoader(assetsService);
            makeupLoader ??= new MakeupLoader(assetsService);
            makeupColorLoader ??= new MakeupColorLoader(assetsService);
            skinColorLoader ??= new SkinColorLoader(assetsService);
            flairLoader ??= new FlairLoader(assetsService);
            refittingService ??= new RefittingService(new UtilityVectorService(assetsService));
            blendShapeLoader ??= new BlendShapeLoader(assetsService);
            blendShapePresetLoader ??= new BlendShapePresetLoader(assetsService);
            subSpeciesLoader ??= new SubSpeciesLoader(new SubSpeciesAssetService(assetsService));

            var outfitAssetLoader = new GroupedOutfitAssetLoader(
                gearSkinOutfitAssetLoader,
                staticOutfitAssetLoader,
                ugcDefaultOutfitAssetLoader,
                ugcOutfitAssetLoader
            );

            // species specific outfit metadata services, this is a tmp solution while we explore having the proper species target in assets metadata
            var outfitMetadataServiceBySpecies = new Dictionary<string, IOutfitAssetMetadataService>
            {
                {
                    GenieSpecies.Unified,
                    new OutfitAssetMetadataService(GenieSpecies.Unified, assetsService, ugcTemplateDataService, ugcWearableDefinitionService)
                },
                {
                    GenieSpecies.UnifiedGAP,
                    new OutfitAssetMetadataService(GenieSpecies.UnifiedGAP, assetsService, ugcTemplateDataService, ugcWearableDefinitionService)
                },
                {
                    GenieSpecies.Dolls,
                    new OutfitAssetMetadataService(GenieSpecies.Dolls, assetsService, ugcTemplateDataService, ugcWearableDefinitionService)
                },
            };

            // we don't really need to wait for this process to finish. Any future refitting operation will await for vectors to be loaded and ready
            refittingService.LoadAllVectorsAsync().Forget();

            return UniTask.FromResult(new Avatars.AvatarsContext
            {
                SpeciesLoader = speciesLoader,
                BlendShapeLoader = blendShapeLoader,
                BlendShapePresetLoader = blendShapePresetLoader,
                MaterialLoader = materialLoader,
                SkinColorLoader = skinColorLoader,
                TattooLoader = tattooLoader,
                MakeupLoader = makeupLoader,
                MakeupColorLoader = makeupColorLoader,
                FlairLoader = flairLoader,
                OutfitAssetLoader = outfitAssetLoader,
                SubSpeciesLoader = subSpeciesLoader,
                RefittingService = refittingService,
                OutfitMetadataServicesBySpecies = outfitMetadataServiceBySpecies,
            });
        }
    }
}
