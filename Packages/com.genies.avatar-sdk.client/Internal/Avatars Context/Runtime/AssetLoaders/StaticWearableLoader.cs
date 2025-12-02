using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using UMA;
using System;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class StaticWearableLoader : IAssetLoader<StaticWearableAsset>
    {
        // dependencies
        private readonly IAssetsService _assetsService;

        public StaticWearableLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<StaticWearableAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            Ref<AssetContainer> containerRef = await _assetsService.LoadAssetAsync<AssetContainer>(assetId, lod:lod);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            containerRef = await UpdateUmaMaterialsAsync(containerRef);
            AssetContainer container = containerRef.Item;

            var asset = new StaticWearableAsset(
                assetId,
                lod,
                container.Subcategory,
                container.CollisionData.ToOutfitCollisionData(),
                container.Recipe,
                container.Slots.ToArray(),
                container.Overlays.ToArray(),
                container.Extras.GetAssets<IGenieComponentCreator>().ToArray()
            );

            return CreateRef.FromDependentResource(asset, containerRef);
        }

        private async UniTask<Ref<AssetContainer>> UpdateUmaMaterialsAsync(Ref<AssetContainer> containerRef, string lod = AssetLod.Default)
        {
            if (!containerRef.IsAlive || !containerRef.Item)
            {
                return containerRef;
            }

            try
            {
                AssetContainer container = containerRef.Item;
                var dependencies = new List<Ref>(container.Slots.Count + container.Overlays.Count + 1);
                dependencies.Add(containerRef);

                foreach (SlotDataAsset slot in container.Slots)
                {
                    if (!slot.material || string.IsNullOrEmpty(slot.material.name))
                    {
                        continue;
                    }

                    Ref<UMAMaterial> materialRef = await _assetsService.LoadAssetAsync<UMAMaterial>(slot.material.name, lod:lod);
                    if (!materialRef.IsAlive)
                    {
                        continue;
                    }

#if UNITY_EDITOR
                    FixChannels(materialRef.Item);
#endif

                    // create a ref that will restore the previous material instance when releasing the asset
                    UMAMaterial previousMaterial = slot.material;
                    var restoreMaterialRef = CreateRef.FromAny(materialRef.Item, _ =>
                    {
                        slot.material = previousMaterial;
                        materialRef.Dispose();
                    });

                    slot.material = restoreMaterialRef.Item;
                    dependencies.Add(restoreMaterialRef);
                }

                foreach (OverlayDataAsset overlay in container.Overlays)
                {
                    if (!overlay.material || string.IsNullOrEmpty(overlay.material.name))
                    {
                        continue;
                    }

                    Ref<UMAMaterial> materialRef = await _assetsService.LoadAssetAsync<UMAMaterial>(overlay.material.name, lod:lod);
                    if (!materialRef.IsAlive)
                    {
                        continue;
                    }

#if UNITY_EDITOR
                    FixChannels(materialRef.Item);
#endif

                    // create a ref that will restore the previous material instance when releasing the asset
                    UMAMaterial previousMaterial = overlay.material;
                    var restoreMaterialRef = CreateRef.FromAny(materialRef.Item, _ =>
                    {
                        overlay.material = previousMaterial;
                        materialRef.Dispose();
                    });

                    overlay.material = restoreMaterialRef.Item;
                    dependencies.Add(restoreMaterialRef);
                }

                return CreateRef.FromDependentResource(container, dependencies);
            }
            catch (Exception e)
            {
                Debug.LogError($"StaticWearableLoader's UpdateUmaMaterialsAsync couldn't update uma materials with exception: {e}.");
                return containerRef;
            }
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
