using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// Equips underwear assets when certain parts of the body are naked. Only for the Unified species.
    /// </summary>
    public sealed class EquipUnderwearIfNaked : IAssetsValidationRule<OutfitAsset>, IDisposable
    {
        private const string BottomUnderwearId = "recEjFaecbT1GNNv2";
        private const string TopUnderwearId = "recHDAw1RgZaMG8lt";

        private static readonly HashSet<string> BottomCoveringSlots = new HashSet<string>
        {
            UnifiedOutfitSlot.UnderwearBottom,
            UnifiedOutfitSlot.Legs,
            UnifiedOutfitSlot.Dress,
        };

        private static readonly HashSet<string> TopCoveringSlots = new HashSet<string>
        {
            UnifiedOutfitSlot.UnderwearTop,
            UnifiedOutfitSlot.Shirt,
            UnifiedOutfitSlot.Hoodie,
            UnifiedOutfitSlot.Jacket,
            UnifiedOutfitSlot.Dress,
        };

        // dependencies
        private readonly IOutfitAssetLoader _outfitAssetLoader;
        private readonly IOutfitAssetMetadataService _outfitAssetMetadataService;
        private readonly string _lod;

        // state
        private Ref<OutfitAsset> _bottomUnderwearAssetRef;
        private Ref<OutfitAsset> _topUnderwearAssetRef;
        private bool _isDisposed;

        public EquipUnderwearIfNaked(IOutfitAssetLoader outfitAssetLoader, IOutfitAssetMetadataService outfitAssetMetadataService, string lod)
        {
            _outfitAssetLoader = outfitAssetLoader;
            _outfitAssetMetadataService = outfitAssetMetadataService;
            _lod = lod;
        }

        public async UniTask InitializeAsync()
        {
            // load the underwear assets
            (_bottomUnderwearAssetRef, _topUnderwearAssetRef) = await UniTask.WhenAll
            (
                LoadOutfitAssetAsync(BottomUnderwearId),
                LoadOutfitAssetAsync(TopUnderwearId)
            );
        }

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            if (_isDisposed)
            {
                return;
            }

            // suppresses are properly configured for bottom underwear but to further optimize lets avoid loading the underwear bottom asset if it is not necessary
            if (_bottomUnderwearAssetRef.IsAlive)
            {
                if (!outfit.Any(IsAssetCoveringBottom))
                {
                    outfit.Add(_bottomUnderwearAssetRef.Item);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EquipUnderwearIfNaked] Underwear bottom asset could not be loaded.");
            };

            // suppresses are currently not properly configured for top underwear so we must not add the asset if it is not necessary or it will leak
            if (_bottomUnderwearAssetRef.IsAlive)
            {
                if (!outfit.Any(IsAssetCoveringTop))
                {
                    outfit.Add(_topUnderwearAssetRef.Item);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EquipUnderwearIfNaked] Underwear top asset could not be loaded.");
            };
        }

        public void Dispose()
        {
            _bottomUnderwearAssetRef.Dispose();
            _topUnderwearAssetRef.Dispose();
            _isDisposed = true;
        }

        private async UniTask<Ref<OutfitAsset>> LoadOutfitAssetAsync(string assetId)
        {
            OutfitAssetMetadata metadata = await _outfitAssetMetadataService.FetchAsync(assetId);
            Ref<OutfitAsset> assetRef = await _outfitAssetLoader.LoadAsync(metadata, _lod);
            return assetRef;
        }

        private static bool IsAssetCoveringBottom(OutfitAsset asset)
        {
            if (asset.Id == "underwearBottom-0001-boxers_skin0000")
            {
                return false; // In case non-Gear underwear was serialized in an old defintition
            }

            return BottomCoveringSlots.Contains(asset.Metadata.Slot);
        }

        private static bool IsAssetCoveringTop(OutfitAsset asset)
        {
            // for tops it is not enough to have any equipped asset, but we also need that there is at least one closed one
            // or with layer lower than 2, since non-layered assets and layer 1 assets are open by default but they are not,
            // a good example of this are dresses

            if (asset.Id == "underwearTop-0002-tankTop_skin0000")
            {
                return false; // In case non-Gear underwear was serialized in an old defintition
            }

            return TopCoveringSlots.Contains(asset.Metadata.Slot)
                   && (asset.Metadata.CollisionData.Layer <= 1 || asset.Metadata.CollisionData.Type is OutfitCollisionType.Closed);
        }
    }
}
