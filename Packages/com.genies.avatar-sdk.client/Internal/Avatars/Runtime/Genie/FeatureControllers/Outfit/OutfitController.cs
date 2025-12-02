using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// Controls the outfit of a <see cref="IEditableGenie"/> instance.
    /// </summary>
    public sealed class OutfitController : AssetsController<OutfitAsset>
    {
        // expose the rules publicly as we want the outfit rules to be customizable by the user
        public new List<IAssetsAdjustmentRule<OutfitAsset>> EquippingAdjustmentRules => base.EquippingAdjustmentRules;
        public new List<IAssetsAdjustmentRule<OutfitAsset>> UnequippedAdjustmentRules => base.UnequippedAdjustmentRules;
        public new List<IAssetsValidationRule<OutfitAsset>> ValidationRules => base.ValidationRules;
        public new List<IAssetsValidationRule<OutfitAsset>> ResolutionRules => base.ResolutionRules;
        
        // dependencies
        private readonly IEditableGenie _genie;
        private readonly IOutfitAssetLoader _outfitAssetLoader;
        private readonly IOutfitAssetMetadataService _outfitAssetMetadataService;

        public OutfitController(IEditableGenie genie, IOutfitAssetLoader outfitAssetLoader, IOutfitAssetMetadataService outfitAssetMetadataService)
        {
            _genie = genie;
            _outfitAssetLoader = outfitAssetLoader;
            _outfitAssetMetadataService = outfitAssetMetadataService;
            
            // avoid releasing assets when unequipped to not break the genie and release them after the genie is rebuilt instead
            ReleaseUnequippedAssetsAutomatically = false;
            _genie.Rebuilt += OnGenieRebuilt;
        }
        
        /// <summary>
        /// Call this after modifying rules to re-apply them and readjust the equipped assets.
        /// </summary>
        public new UniTask ValidateAndResolveAssetsAsync()
        {
            return base.ValidateAndResolveAssetsAsync();
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _genie.Rebuilt -= OnGenieRebuilt;
        }

        protected override async UniTask<Ref<OutfitAsset>> LoadAssetAsync(string assetId)
        {
            if (assetId is null)
            {
                return default;
            }

            OutfitAssetMetadata metadata = await _outfitAssetMetadataService.FetchAsync(assetId);
            if (!metadata.IsValid)
            {
                return default;
            }

            Ref<OutfitAsset> assetRef = await _outfitAssetLoader.LoadAsync(metadata, _genie.Lod);
            return assetRef;
        }

        protected override UniTask OnAssetEquippedAsync(OutfitAsset asset)
        {
            return _genie.AddOutfitAssetAsync(asset);
        }

        protected override UniTask OnAssetUnequippedAsync(OutfitAsset asset)
        {
            return _genie.RemoveOutfitAssetAsync(asset);
        }

        private void OnGenieRebuilt()
        {
            ReleaseUnequippedAssets();
        }
    }
}