using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using Genies.Utilities;

namespace Genies.Avatars.Context
{
    public sealed class GearAssetLoader : IAssetLoader<GearAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly GearElementAssetLoader _elementLoader;

        public GearAssetLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
            _elementLoader = new GearElementAssetLoader(assetsService);
        }

        public async UniTask<Ref<GearAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            Ref<GearElementContainer> containerRef = await _assetsService.LoadAssetAsync<GearElementContainer>(assetId, lod:lod);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            // for now, we only have one element asset per gear asset. This is a tmp solution until we bring back UGC assets
            Ref<GearElementAsset> assetRef = await _elementLoader.LoadGearElementAssetAsync(assetId, lod, containerRef.Item);
            if (!assetRef.IsAlive)
            {
                containerRef.Dispose();
                return default;
            }

            GearAsset asset = CreateGearAsset(assetId, lod, containerRef.Item, new [] { assetRef.Item });
            return CreateRef.FromDependentResource(asset, containerRef, assetRef);
        }

        /// <summary>
        /// Converts the <see cref="GearElementContainer"/> data model into the <see cref="GearAsset"/>.
        /// </summary>
        public static GearAsset CreateGearAsset(string assetId, string lod, GearElementContainer container, GearElementAsset[] elements)
        {
            var data = new GearAssetData(
                assetId,
                container.slot,
                container.subcategory,
                container.collisionData.ToOutfitCollisionData()
            );

            var asset = new GearAsset(
                lod,
                data,
                container.meshHideAssets.ToArray(),
                elements,
                container.extras.GetAssets<IGenieComponentCreator>().ToArray()
            );

            return asset;
        }
    }
}
