using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Removes from the outfit any assets that are incompatible with the equipping asset. Assets are considered incompatible
    /// when they occupy the same slot. Optionally, you can also provide an <see cref="OutfitSlotsData"/> instance to also check
    /// for extra incompatible slots and colliding assets based on collision groups.
    /// </summary>
    public sealed class RemoveIncompatibleAssets : IAssetsAdjustmentRule<OutfitAsset>
    {
        private readonly OutfitSlotsData _slotsData;

        public RemoveIncompatibleAssets(OutfitSlotsData slotsData = null)
        {
            _slotsData = slotsData;
        }

        public void Apply(HashSet<OutfitAsset> outfit, OutfitAsset equippedAsset)
        {
            // remove any asset occupying the equipping asset's slot
            outfit.RemoveWhere(asset => asset.Metadata.Slot == equippedAsset.Metadata.Slot);

            if (_slotsData is null)
            {
                return;
            }

            // remove any incompatible slots
            if (_slotsData.TryGetSlot(equippedAsset.Metadata.Slot, out OutfitSlotsData.Slot slotData))
            {
                RemoveIncompatibleSlots(slotData, outfit);
            }

            // remove any colliding assets
            if (_slotsData.TryGetCollisionGroupBySlotName(equippedAsset.Metadata.Slot, out OutfitSlotsData.CollisionGroup collisionGroup))
            {
                RemoveCollidingAssets(collisionGroup, outfit, equippedAsset);
            }
        }

        private void RemoveIncompatibleSlots(OutfitSlotsData.Slot slotData, HashSet<OutfitAsset> outfit)
        {
            foreach (string slot in slotData.IncompatibleSlots)
            {
                outfit.RemoveWhere(asset => asset.Metadata.Slot == slot);
            }
        }

        private void RemoveCollidingAssets(OutfitSlotsData.CollisionGroup collisionGroup, HashSet<OutfitAsset> outfit, OutfitAsset equippingAsset)
        {
            OutfitCollisionData equippingCollisionData = equippingAsset.Metadata.CollisionData;
            int equippingLayer = equippingCollisionData.Layer;
            bool isEquippingAssetClosed = equippingCollisionData.Type is OutfitCollisionType.Closed;

            outfit.RemoveWhere(asset =>
            {
                // only process collisions on assets belonging to the same collision group
                if (!collisionGroup.Slots.Contains(asset.Metadata.Slot))
                {
                    return false;
                }

                OutfitCollisionData collisionData = asset.Metadata.CollisionData;

                // remove any assets on the same layer
                if (collisionData.Layer == equippingLayer)
                {
                    return true;
                }

                // remove any assets on lower layers if the equipping asset is closed
                if (isEquippingAssetClosed && collisionData.Layer < equippingLayer)
                {
                    return true;
                }

                // remove any assets on higher layers if they are closed since the equipping asset has priority
                return collisionData.Layer > equippingLayer && collisionData.Type is OutfitCollisionType.Closed;
            });
        }
    }
}
