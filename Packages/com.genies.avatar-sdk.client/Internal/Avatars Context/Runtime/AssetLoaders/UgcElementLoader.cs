using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using UnityEngine;
using System;
using Genies.Ugc;
using Genies.Utilities;

namespace Genies.Avatars.Context
{
    public sealed class UgcElementLoader : IAssetLoader<UgcElementAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IUgcTemplateDataService _templateDataService;

        public UgcElementLoader(IAssetsService assetsService, IUgcTemplateDataService templateDataService)
        {
            _assetsService = assetsService;
            _templateDataService = templateDataService;
        }

        public async UniTask<Ref<UgcElementAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            try
            {
                // fetch element data first
                UgcTemplateElementData elementData = await _templateDataService.FetchElementDataAsync(assetId);
                if (elementData is null)
                {
                    return default;
                }

                // fetch the ElementContainer asset from the content build
                Ref<ElementContainer> containerRef = await _assetsService.LoadAssetAsync<ElementContainer>(elementData.ElementId, lod:lod);
                if (!containerRef.IsAlive || !containerRef.Item)
                {
                    return default;
                }

#if UNITY_EDITOR
                Ref<UgcElementAsset> fixedAssetRef = GetFixedUgcElementAsset(elementData, containerRef);
                if (fixedAssetRef.IsAlive)
                    return fixedAssetRef;
#endif

                ElementContainer container = containerRef.Item;

                var asset = new UgcElementAsset(
                    AssetLod.Default,
                    elementData,
                    container.AlbedoTransparency,
                    container.MetallicSmoothness,
                    container.Normal,
                    container.RgbaMask,
                    container.SlotDataAssets.ToArray(),
                    container.extras.GetAssets<IGenieComponentCreator>().ToArray()
                );

                // create a new ref to the element asset that is linked to the ElementContainer ref loaded from the assets service
                Ref<UgcElementAsset> assetRef = CreateRef.FromDependentResource(asset, containerRef);
                return assetRef;
            }
            catch (Exception e)
            {
                Debug.LogError($"UgcElementLoader's LoadAsync couldn't load asset {assetId} with exception: {e}.");
                return default;
            }
        }

#if UNITY_EDITOR
        // this is only required in the editor when targeting Android platform
        private static Ref<UgcElementAsset> GetFixedUgcElementAsset(UgcTemplateElementData elementData, Ref<ElementContainer> containerRef)
        {
            ElementContainer container = containerRef.Item;
            Ref<Texture> fixedNormalRef = ContentNormalMapFix.GetFixedNormalMap(container.Normal);
            if (!fixedNormalRef.IsAlive)
                return default;

            var asset = new UgcElementAsset(
                AssetLod.Default,
                elementData,
                container.AlbedoTransparency,
                container.MetallicSmoothness,
                fixedNormalRef.Item,
                container.RgbaMask,
                container.SlotDataAssets.ToArray(),
                container.extras.GetAssets<IGenieComponentCreator>().ToArray()
            );

            // create a new ref to the element asset that is linked to the ElementContainer ref loaded from the assets service
            Ref<UgcElementAsset> assetRef = CreateRef.FromDependentResource(asset, containerRef, fixedNormalRef);
            return assetRef;
        }
#endif
    }
}
