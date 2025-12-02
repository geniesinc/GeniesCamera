using System;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Shaders;
using Genies.Models;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class MaterialLoader : ISlottedAssetLoader<MaterialAsset>
    {
        private static readonly int ColorBaseId = Shader.PropertyToID("_ColorBase");
        private static readonly int ColorRId = Shader.PropertyToID("_ColorR");
        private static readonly int ColorGId = Shader.PropertyToID("_ColorG");
        private static readonly int ColorBId = Shader.PropertyToID("_ColorB");

        // dependencies
        private readonly IAssetsService _assetsService;

        public MaterialLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<MaterialAsset>> LoadAsync(string assetId, string slotId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            if (slotId == UnifiedMaterialSlot.Hair)
            {
                // try to load as a custom hair color from embedded data
                Ref<MaterialAsset> assetRef = LoadEmbeddedCustomHair(assetId);
                if (assetRef.IsAlive)
                {
                    return assetRef;
                }
            }

            Ref<MaterialDataContainer> containerRef = await _assetsService.LoadAssetAsync<MaterialDataContainer>(assetId, lod:lod);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            var materialAsset = new MaterialAsset(assetId, AssetLod.Default, containerRef.Item.targetMaterial);

            return CreateRef.FromDependentResource(materialAsset, containerRef);
        }

        private Ref<MaterialAsset> LoadEmbeddedCustomHair(string assetId)
        {
            try
            {
                // try to get the data from AvatarEmbeddedData
                if (!AvatarEmbeddedData.TryGetData(assetId, out CustomHairColorData data))
                {
                    return default;
                }

                var material = new Material(GeniesShaders.MegaHair.Shader);
                material.SetColor(ColorBaseId, data.ColorBase);
                material.SetColor(ColorRId,    data.ColorR);
                material.SetColor(ColorGId,    data.ColorG);
                material.SetColor(ColorBId,    data.ColorB);

                var materialAsset = new MaterialAsset(assetId, AssetLod.Default, material);
                Ref<Material> materialRef = CreateRef.FromUnityObject(material);
                return CreateRef.FromDependentResource(materialAsset, materialRef);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(MaterialLoader)}] something went wrong while loading embedded custom hair color: {assetId}\n{exception}");
                return default;
            }
        }
    }
}
