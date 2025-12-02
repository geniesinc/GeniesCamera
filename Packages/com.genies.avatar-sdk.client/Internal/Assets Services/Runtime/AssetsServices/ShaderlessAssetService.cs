using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Addressables;
using Genies.Addressables.CustomResourceLocation;
using Genies.Components.ShaderlessTools;
using Genies.FeatureFlags;
using Genies.Models;
using Genies.Refs;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Assets.Services
{
    public class ShaderlessAssetService : IShaderlessAssetService
    {
        // dependencies
        private readonly IAssetsService _assetsService;
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        public ShaderlessAssetService(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public async UniTask<Ref<T>> LoadShadersAsync<T>(Ref<T> assetRef)
        {
            if (!assetRef.IsAlive)
            {
                return assetRef;
            }

            if (assetRef.Item is not IShaderlessAsset shaderlessAsset)
            {
                return assetRef;
            }

            ShaderlessMaterials shaderlessMats = shaderlessAsset.ShaderlessMaterials;
            Ref[] shaderContainers  = await UniTask.WhenAll(shaderlessMats.materials.Select(ProcessMaterialProperties));

            return CreateRef.FromDependentResource(assetRef, shaderContainers);
        }

        private async UniTask<Ref> ProcessMaterialProperties(MaterialProps materialProp)
        {
            if (_FeatureFlagsManager is not null && _FeatureFlagsManager.IsFeatureEnabled(SharedFeatureFlags.AddressablesInventoryLocations))
            {
                AddShaderMaterialResourceLocator(materialProp.hash);
            }

            Ref<ShadersContainer> shaderContainerRef = await _assetsService.LoadAssetAsync<ShadersContainer>(materialProp.hash);
            if(!shaderContainerRef.IsAlive)
            {
                return default;
            }

            Material templateMaterial = shaderContainerRef.Item.Material;

            // re-apply all properties to the material.
            ShaderlessMaterialUtility.SetShaderProps(materialProp, templateMaterial);
            return shaderContainerRef;
        }

        private void AddShaderMaterialResourceLocator(string hash, int version = 0)
        {
            CustomResourceLocationService.InitializeResourceProviders();

            var locationMetadata = new ResourceLocationMetadata
            {
                Type = typeof(ShadersContainer),
                Address = ShaderlessResourceLocationProvider.PrimaryKey(hash),
                InternalId = ShaderlessResourceLocationProvider.InternalId(hash),
                BundleKey = ShaderlessResourceLocationProvider.BundleKey(hash, version),
                RemoteUrl = ShaderlessResourceLocationProvider.RemoteUrl(hash, BaseAddressablesService.GetPlatformString(), BaseAddressableProvider.DynBaseUrl, version),
            };

            CustomResourceLocationUtils.AddCustomLocator(locationMetadata);
        }
    }
}
