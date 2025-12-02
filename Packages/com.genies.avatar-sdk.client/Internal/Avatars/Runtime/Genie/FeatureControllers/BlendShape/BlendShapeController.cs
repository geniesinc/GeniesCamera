using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Avatars.ValidationRules;
using Genies.Refs;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    public sealed class BlendShapeController : AssetsController<BlendShapeAsset>, IBlendShapeController
    {
        // dependencies
        private readonly IEditableGenie _genie;
        private readonly IAssetLoader<BlendShapeAsset> _blendShapeLoader;
        private readonly IAssetLoader<BlendShapePresetAsset> _blendShapePresetLoader;
        
        // state
        private readonly ForceDefaultBlendShapes _defaultBlendShapesRule;

        private readonly Dictionary<string, BlendShapeAsset> _assetsOnSlots;

        public BlendShapeController(IEditableGenie genie, IAssetLoader<BlendShapeAsset> blendShapeLoader,
                                    IAssetLoader<BlendShapePresetAsset> blendShapePresetLoader, IEnumerable<BlendShapeAsset> defaultBlendShapes = null)
        {
            _genie = genie;
            _blendShapeLoader = blendShapeLoader;
            _blendShapePresetLoader = blendShapePresetLoader;

            _assetsOnSlots = new Dictionary<string, BlendShapeAsset>();
            _defaultBlendShapesRule = new ForceDefaultBlendShapes(defaultBlendShapes);
            
            // this controller uses a fixed set of rules, so we are initializing them here and we are not exposing the rules to be modified externally
            EquippingAdjustmentRules.Add(new RemovePreviousBlendShapeOnSlot());
            ValidationRules.Add(new EnsureOnlyOneBlendShapePerSlot());
            ResolutionRules.Add(_defaultBlendShapesRule);
        }

        public UniTask SetDefaultBlendShapesAsync(IEnumerable<BlendShapeAsset> blendShapes)
        {
            _defaultBlendShapesRule.SetDefaultBlendShapes(blendShapes);
            return ValidateAndResolveAssetsAsync();
        }
        
        public async UniTask LoadAndEquipPresetAsync(string assetId)
        {
            if (IsDisposedWithLog())
            {
                return;
            }

            // we don't need to keep this ref alive as the BlendShapePresetAsset doesn't really allocate releasable assets
            using Ref<BlendShapePresetAsset> assetRef = await _blendShapePresetLoader.LoadAsync(assetId);
            await EquipPresetAsync(assetRef.Item);
        }

        public UniTask EquipPresetAsync(BlendShapePresetAsset preset)
        {
            if (IsDisposedWithLog() || preset?.BlendShapeAssets is null)
            {
                return UniTask.CompletedTask;
            }

            IEnumerable<Ref<BlendShapeAsset>> blendShapeAssetRefs = preset.BlendShapeAssets.Select(CreateRef.FromAny);
            return UniTask.WhenAll(blendShapeAssetRefs.Select(EquipAssetAsync));
        }

        public string GetEquippedBlendShapeForSlot(string slot)
        {
            if (IsDisposedWithLog())
            {
                return null;
            }

            return _assetsOnSlots.TryGetValue(slot, out var asset) ? asset.Id : null;
        }

        public async UniTask<bool> IsPresetEquippedAsync(string presetId)
        {
            if (IsDisposedWithLog())
            {
                return false;
            }

            using Ref<BlendShapePresetAsset> presetRef = await _blendShapePresetLoader.LoadAsync(presetId);
            return presetRef.IsAlive && IsPresetEquipped(presetRef.Item);
        }

        public bool IsPresetEquipped(BlendShapePresetAsset preset)
        {
            if (IsDisposedWithLog() || preset?.BlendShapeAssets is null)
            {
                return false;
            }

            foreach (BlendShapeAsset asset in preset.BlendShapeAssets)
            {
                if (!IsAssetEquipped(asset.Id))
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool IsPresetEquipped(IEnumerable<string> assetIds)
        {
            if (IsDisposedWithLog())
            {
                return false;
            }

            foreach (string assetId in assetIds)
            {
                if (!IsAssetEquipped(assetId))
                {
                    return false;
                }
            }
            
            return true;
        }

        public override void Dispose()
        {
            base.Dispose();
            _defaultBlendShapesRule.SetDefaultBlendShapes(null);
        }
        
        protected override UniTask<Ref<BlendShapeAsset>> LoadAssetAsync(string assetId)
        {
            return _blendShapeLoader.LoadAsync(assetId, _genie.Lod);
        }

        protected override UniTask OnAssetEquippedAsync(BlendShapeAsset asset)
        {
            if (asset.Dna is null)
            {
                return UniTask.CompletedTask;
            }

            //Track the asset got added
            _assetsOnSlots[asset.Slot] = asset;

            foreach (DnaEntry dnaEntry in asset.Dna)
            {
                _genie.SetDna(dnaEntry.Name, dnaEntry.Value);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask OnAssetUnequippedAsync(BlendShapeAsset asset)
        {
            if (asset.Dna is null)
            {
                return UniTask.CompletedTask;
            }

            //Untrack asset
            if (_assetsOnSlots.ContainsKey(asset.Slot))
            {
                _assetsOnSlots.Remove(asset.Slot);
            }            
            
            foreach (DnaEntry dnaEntry in asset.Dna)
            {
                _genie.SetDna(dnaEntry.Name, 0.5f);
            }

            return UniTask.CompletedTask;
        }
    }
}