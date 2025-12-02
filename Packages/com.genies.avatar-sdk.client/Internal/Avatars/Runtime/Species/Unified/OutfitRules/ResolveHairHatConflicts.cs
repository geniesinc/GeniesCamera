using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UMA;

namespace Genies.Avatars
{
    /// <summary>
    /// Resolves some conflicts with certain hair-hat asset combinations. Only for the Unified species.
    /// </summary>
    public sealed class ResolveHairHatConflicts : IAssetsValidationRule<OutfitAsset>, IDisposable
    {
        // blend shape name used in our hair assets for the blend shapes that hides the hair under hats with blendshape mode.
        private const string _hideHairBlendShapeKey = "hair_geo_blendShape";
        private const string _fallbackHairId = "recAYdVfFO04gdDyR"; // Gear, hair-0026-buzz
        private const bool _bakeBlendShape = true;

        // dependencies
        private readonly IEditableGenie _genie;
        private readonly IOutfitAssetLoader _outfitAssetLoader;
        private readonly IOutfitAssetMetadataService _outfitAssetMetadataService;

        // state
        private readonly List<string> _hideHairBlendShapeNames;
        private Ref<OutfitAsset> _fallbackHairAssetRef;
        private bool _isDisposed;

        public ResolveHairHatConflicts(IEditableGenie genie, IOutfitAssetLoader outfitAssetLoader, IOutfitAssetMetadataService outfitAssetMetadataService)
        {
            _genie = genie;
            _outfitAssetLoader = outfitAssetLoader;
            _outfitAssetMetadataService = outfitAssetMetadataService;

            _hideHairBlendShapeNames = new List<string>();
        }

        public async UniTask InitializeAsync()
        {
            _fallbackHairAssetRef = await LoadOutfitAssetAsync(_fallbackHairId);
        }

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            if (_isDisposed)
            {
                return;
            }

            // make sure any hide blendshapes from previous hairs are cleared from the genie
            ClearHideHairBlendShapes();

            // no conflicts to solve if there is not a hat and hair assets at the same time
            if (!HasHairAndHat(outfit, out OutfitAsset hair, out OutfitAsset hat))
            {
                // if there is a hair equipped make sure its hide blendshapes are disabled
                SetHideHairBlendShapesEnabled(hair, enabled: false);
                return;
            }

            // checkout the hat hair mode and apply the corresponding solution
            switch (hat.Metadata.CollisionData.HatHairMode)
            {
                case OutfitHatHairMode.Fallback:
                    // replace equipped hair with the fallback hair
                    outfit.Remove(hair);
                    if (!_fallbackHairAssetRef.IsAlive)
                    {
                        break;
                    }

                    outfit.Add(_fallbackHairAssetRef.Item);
                    SetHideHairBlendShapesEnabled(_fallbackHairAssetRef.Item, enabled: false);

                    break;

                case OutfitHatHairMode.Blendshape:
                    SetHideHairBlendShapesEnabled(hair, enabled: true);
                    break;

                default:
                case OutfitHatHairMode.None:
                    SetHideHairBlendShapesEnabled(hair, enabled: false);
                    break;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            ClearHideHairBlendShapes();
            _fallbackHairAssetRef.Dispose();
        }

        private void SetHideHairBlendShapesEnabled(OutfitAsset hairAsset, bool enabled)
        {
            if (hairAsset is null)
            {
                return;
            }

            float blendShapeValue = enabled ? 1.0f : 0.0f;

            // (UMA version) iterate over all the mesh data from the slots
            if (hairAsset.Slots is not null)
            {
                foreach (SlotDataAsset slotDataAsset in hairAsset.Slots)
                {
                    if (!slotDataAsset.meshData || slotDataAsset.meshData.blendShapes is null)
                    {
                        continue;
                    }

                    // iterate over all blendshapes from the mesh data
                    foreach (UMABlendShape blendShape in slotDataAsset.meshData.blendShapes)
                    {
                        string blendShapeName = blendShape?.shapeName;
                        if (blendShapeName is null || !blendShapeName.Contains(_hideHairBlendShapeKey))
                        {
                            continue;
                        }

                        // if this is a hide hair blendshape, then register it and reset its value on the genie
                        _hideHairBlendShapeNames.Add(blendShapeName);

                    }
                }
            }


            // (Non UMA version) iterate over all the mesh assets
            if (hairAsset.MeshAssets is not null)
            {
                foreach (MeshAsset meshAsset in hairAsset.MeshAssets)
                {
                    if (meshAsset.BlendShapes is null)
                    {
                        continue;
                    }

                    // iterate over all blendshapes from the mesh data
                    foreach (UMABlendShape blendShape in meshAsset.BlendShapes)
                    {
                        string blendShapeName = blendShape?.shapeName;
                        if (blendShapeName is null || !blendShapeName.Contains(_hideHairBlendShapeKey))
                        {
                            continue;
                        }

                        // if this is a hide hair blendshape, then register it and reset its value on the genie
                        _hideHairBlendShapeNames.Add(blendShapeName);

                    }
                }
            }

            // Set blendshapes
            foreach(string shape in _hideHairBlendShapeNames)
            {
                _genie.SetBlendShape(shape, blendShapeValue, _bakeBlendShape);
            }
        }

        private void ClearHideHairBlendShapes()
        {
            foreach (string hideHairBlendShapeName in _hideHairBlendShapeNames)
            {
                _genie.RemoveBlendShape(hideHairBlendShapeName);
            }

            _hideHairBlendShapeNames.Clear();
        }

        private async UniTask<Ref<OutfitAsset>> LoadOutfitAssetAsync(string assetId)
        {
            OutfitAssetMetadata metadata = await _outfitAssetMetadataService.FetchAsync(assetId);
            Ref<OutfitAsset> assetRef = await _outfitAssetLoader.LoadAsync(metadata, _genie.Lod);
            return assetRef;
        }

        /// <summary>
        /// Returns true if the given outfit has both hair and hat assets equipped and outputs the found assets.
        /// </summary>
        private static bool HasHairAndHat(HashSet<OutfitAsset> outfit, out OutfitAsset hair, out OutfitAsset hat)
        {
            hair = default;
            hat = default;
            bool hasHair = false;
            bool hasHat = false;

            foreach (OutfitAsset asset in outfit)
            {
                if (!hasHair && asset.Metadata.Slot == UnifiedOutfitSlot.Hair)
                {
                    hair = asset;
                    hasHair = true;
                }

                if (!hasHat && asset.Metadata.Slot == UnifiedOutfitSlot.Hat)
                {
                    hat = asset;
                    hasHat = true;
                }

                // return if we have found both already
                if (hasHair && hasHat)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
