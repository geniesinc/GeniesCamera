using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// Given a default set of assets mapped by the slot, it will ensure that the given outfit includes the default assets
    /// for the empty slots that have one.
    /// </summary>
    public sealed class AddDefaultAssetsOnEmptySlots : IAssetsValidationRule<OutfitAsset>, IDisposable
    {
        // dependencies
        private readonly IOutfitAssetLoader _outfitAssetLoader;
        private readonly IOutfitAssetMetadataService _outfitAssetMetadataService;
        
        // state
        private readonly Dictionary<string, Ref<OutfitAsset>> _defaultAssetsBySlot;
        private bool _isDisposed;

        public AddDefaultAssetsOnEmptySlots(IOutfitAssetLoader outfitAssetLoader, IOutfitAssetMetadataService outfitAssetMetadataService)
        {
            _outfitAssetLoader = outfitAssetLoader;
            _outfitAssetMetadataService = outfitAssetMetadataService;
            
            _defaultAssetsBySlot = new Dictionary<string, Ref<OutfitAsset>>();
        }

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (KeyValuePair<string, Ref<OutfitAsset>> keyValuePair in _defaultAssetsBySlot)
            {
                string slotId = keyValuePair.Key;
                Ref<OutfitAsset> assetRef = keyValuePair.Value;
                
                // if the slot is empty, add the default asset
                if(assetRef.IsAlive && !outfit.Any(asset => asset.Metadata.Slot == slotId))
                {
                    outfit.Add(assetRef.Item);
                }
            }
        }

        public UniTask SetDefaultAssetsAsync(IEnumerable<(string id, string slotId)> assets)
        {
            foreach (Ref<OutfitAsset> assetRef in _defaultAssetsBySlot.Values)
            {
                assetRef.Dispose();
            }

            _defaultAssetsBySlot.Clear();
            return UniTask.WhenAll(assets.Select(SetDefaultAssetAsync));
        }
        
        public void Dispose()
        {
            foreach (Ref<OutfitAsset> assetRef in _defaultAssetsBySlot.Values)
            {
                assetRef.Dispose();
            }

            _defaultAssetsBySlot.Clear();
            _isDisposed = true;
        }

        private async UniTask SetDefaultAssetAsync((string assetId, string slotId) asset)
        {
            if (asset.assetId is null || asset.slotId is null)
            {
                return;
            }

            // load the asset
            OutfitAssetMetadata metadata = await _outfitAssetMetadataService.FetchAsync(asset.assetId);
            Ref<OutfitAsset> assetRef = await _outfitAssetLoader.LoadAsync(metadata);

            // dispose previous reference
            if (_defaultAssetsBySlot.TryGetValue(asset.slotId, out Ref<OutfitAsset> previousAssetRef) &&
                previousAssetRef != assetRef)
            {
                previousAssetRef.Dispose();
            }

            if (!assetRef.IsAlive)
            {
                return;
            }

            _defaultAssetsBySlot[asset.slotId] = assetRef;
        }
    }
}