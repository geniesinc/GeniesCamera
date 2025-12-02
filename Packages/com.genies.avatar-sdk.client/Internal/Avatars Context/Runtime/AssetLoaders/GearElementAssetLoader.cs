using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using UMA;

namespace Genies.Avatars.Context
{
    public sealed class GearElementAssetLoader : IAssetLoader<GearElementAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;


        public GearElementAssetLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<GearElementAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
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

            Ref<GearElementAsset> assetRef = await LoadGearElementAssetAsync(assetId, lod, containerRef.Item);
            return CreateRef.FromDependentResource(assetRef, containerRef);
        }

        public async UniTask<Ref<GearElementAsset>> LoadGearElementAssetAsync(string assetId, string lod, GearElementContainer container)
        {
            Ref<List<GearSubElement>> subElementsRef =  await LoadGearSubElementsAsync(container.subElements);
            if (!subElementsRef.IsAlive)
            {
                return default;
            }

            var asset = new GearElementAsset(
                assetId,
                lod,
                subElementsRef.Item.ToArray(),
                Array.Empty<IGenieComponentCreator>() // right now element assets are not coming with any extras from content data models
            );

            return CreateRef.FromDependentResource(asset, subElementsRef);
        }

        public async UniTask<Ref<List<GearSubElement>>> LoadGearSubElementsAsync(IEnumerable<GearSubElementContainer> containers)
        {
            if (containers is null)
            {
                return default;
            }

            Ref<GearSubElement>[] subElementRefs = await UniTask.WhenAll(containers.Select(LoadGearSubElementAsync));

            var subElements = new List<GearSubElement>(subElementRefs.Length);
            foreach (Ref<GearSubElement> subElementRef in subElementRefs)
            {
                if (subElementRef.IsAlive)
                {
                    subElements.Add(subElementRef.Item);
                }
            }

            return CreateRef.FromDependentResource(subElements, subElementRefs);
        }

        public async UniTask<Ref<GearSubElement>> LoadGearSubElementAsync(GearSubElementContainer container)
        {
            Ref<UMAMaterial> materialRef = await _assetsService.LoadAssetAsync<UMAMaterial>(container.UmaMaterialAddress);

            if (!materialRef.IsAlive)
            {
                // fallback to the material referenced by the slot data asset (create a dummy ref that does nothing)
                materialRef = CreateRef.FromAny(container.slotDataAsset.material);
            }
#if UNITY_EDITOR
            else
            {
                FixChannels(materialRef.Item);
            }
#endif

            var subElement = new GearSubElement(
                container.slotDataAsset,
                container.overlayDataAsset,
                materialRef.Item,
                container.editableRegionsMap,
                container.editableRegionCount
            );

            return CreateRef.FromDependentResource(subElement, materialRef);
        }

#if UNITY_EDITOR
        private static void FixChannels(UMAMaterial material)
        {
            if (!ContentNormalMapFix.IsFixNeeded())
                return;

            UMAMaterial.MaterialChannel[] channels = material.channels;
            for (int i = 0; i < channels.Length; i++)
            {
                UMAMaterial.MaterialChannel channel = channels[i];

                /**
                 * On the Unity Editor we will always have either the PC or MacOS content builds, but if the current target platform doesn't
                 * support DXT5 normal maps then UMA will mess up with channels marked as normal maps, since for static wearables the shader
                 * also comes from the content build and it has DXT5 support enabled. I know its confusing but at least it is a simple fix
                 * to have wearables looking good on the editor when targeting Android.
                 */
                if (channel.channelType is UMAMaterial.ChannelType.NormalMap or UMAMaterial.ChannelType.DetailNormalMap)
                    channel.channelType = UMAMaterial.ChannelType.Texture;

                channels[i] = channel;
            }
        }
#endif
    }
}
