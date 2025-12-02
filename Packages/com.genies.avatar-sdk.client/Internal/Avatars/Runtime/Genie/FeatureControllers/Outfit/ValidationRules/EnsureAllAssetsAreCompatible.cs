using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Ensures that all assets in the outfit are compatible with each other. Assets are considered incompatible
    /// when they occupy the same slot. Optionally, you can also provide an <see cref="OutfitSlotsData"/> instance
    /// to also check for extra incompatible slots and colliding assets.
    /// </summary>
    public sealed class EnsureAllAssetsAreCompatible : IAssetsValidationRule<OutfitAsset>
    {
        private readonly OutfitSlotsData _slotsData;
        private readonly Dictionary<string, OutfitAsset> _mappedOutfit;
        private readonly List<OutfitAsset> _assets;
        private readonly HashSet<int> _layers;

        public EnsureAllAssetsAreCompatible(OutfitSlotsData slotsData = null)
        {
            _slotsData = slotsData;
            _mappedOutfit = new Dictionary<string, OutfitAsset>();
            _assets = new List<OutfitAsset>();
            _layers = new HashSet<int>();
        }

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            // map all assets to their slots, as a result we will also discard any exceeding assets for each slot
            _mappedOutfit.Clear();
            foreach (OutfitAsset asset in outfit)
            {
                _mappedOutfit[asset.Metadata.Slot] = asset; // discard any previous asset
            }

            EnsureCompatibilityBasedOnTheSlotsData();

            // replace the outfit with the result mapped outfit
            outfit.Clear();
            outfit.UnionWith(_mappedOutfit.Values);
        }

        private void EnsureCompatibilityBasedOnTheSlotsData()
        {
            if (_slotsData is null)
            {
                return;
            }

            // ensure no incompatible assets based on the incompatible slots defined in the data
            foreach (OutfitSlotsData.Slot slot in _slotsData.Slots)
            {
                if (!_mappedOutfit.ContainsKey(slot.Name))
                {
                    continue;
                }

                // mapped outfit contains an asset on this slot so remove all incompatible slots
                foreach (string incompatibleSlot in slot.IncompatibleSlots)
                {
                    _mappedOutfit.Remove(incompatibleSlot);
                }
            }
            
            // ensure no colliding assets based on all collision groups defined in the data
            foreach (OutfitSlotsData.CollisionGroup collisionGroup in _slotsData.CollisionGroups)
            {
                // perform the following actions on the mapped outfit for each collision group
                EnsureOnlyOneAssetPerLayer(collisionGroup);
                RemoveAssetsHiddenByHigherClosedLayers(collisionGroup);
            }
        }

        private void EnsureOnlyOneAssetPerLayer(OutfitSlotsData.CollisionGroup collisionGroup)
        {
            _layers.Clear();

            // iterate over all slots from the collision group
            foreach (string slot in collisionGroup.Slots)
            {
                // skip if there is no asset on this slot
                if (!_mappedOutfit.TryGetValue(slot, out OutfitAsset asset))
                {
                    continue;
                }

                // check if layer is occupied by another asset, in that case remove the asset from the outfit
                int layer = asset.Metadata.CollisionData.Layer;
                
                if (_layers.Contains(layer))
                {
                    _mappedOutfit.Remove(slot);
                }
                else
                {
                    _layers.Add(layer);
                }
            }
        }

        private void RemoveAssetsHiddenByHigherClosedLayers(OutfitSlotsData.CollisionGroup collisionGroup)
        {
            // gather all assets on this collision group
            _assets.Clear();
            foreach (string slot in collisionGroup.Slots)
            {
                if (_mappedOutfit.TryGetValue(slot, out OutfitAsset asset))
                {
                    _assets.Add(asset);
                }
            }

            // sort the collision group assets by their layer in descending order
            _assets.Sort((assetX, assetY) => assetY.Metadata.CollisionData.Layer.CompareTo(assetX.Metadata.CollisionData.Layer));
            
            // iterate from the top to the bottom layer
            bool isHidden = false;
            for (int i = 0; i < _assets.Count; ++i)
            {
                if (isHidden)
                {
                    _mappedOutfit.Remove(_assets[i].Metadata.Slot);
                    continue;
                }
                
                // as soon as we find a closed asset we remove all the lower layers
                isHidden = _assets[i].Metadata.CollisionData.Type is OutfitCollisionType.Closed;
            }
        }
    }
}